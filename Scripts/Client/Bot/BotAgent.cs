using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BotAgent : Player {
    [Header("Bot")]
    public int BotId;
    public bool isOwnedByMe;
    public NavMeshAgent agent;

    Coroutine _aiLoop;

    Vector3 _spawnFallback = Vector3.zero;

    [Header("Combat Sense")]
    public float senseRadius = 15f;       // 탐지 반경
    public float chaseStopRange = 2.0f;   // 도착 판정 여유
    public float preferReacquireSec = 10.6f;
    // 너무 붙었을 때 뒤로 빠질 거리/쿨다운
    public float minPersonalSpace = 2.0f;      // 이 거리 이하면 백오프 시도
    public Vector2 backoffRange = new Vector2(6.0f, 15.0f); // 랜덤 후퇴 거리
    public float fleeCooldown = 1.2f;          // 너무 자주 튀지 않게
    float _fleeCooldownUntil = 0f;


    Transform _chasingTarget;
    float _nextSenseAt = 0f;

    [Header("Flee Lock")]
    public float fleeLockSeconds = 1.5f;   // 도망 고정 시간
    float _fleeLockUntil = 0f;           // 이 시간 전까지는 재교전 금지

    [Header("Think Loop")]
    public float thinkBaseInterval = 0.2f;
    public float thinkJitter = 0.07f;          // 지터

    float _initDesync;


    bool TryPickBackoff(Vector3 threatPos, out Vector3 dest) {
        dest = transform.position;

        // (나부터 위협할때 ㅅㅂ) 반대 방향 + 약간의 좌/우 랜덤 오프셋
        Vector3 away = (transform.position - threatPos);
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
        away.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, away).normalized;
        float sideMul = Random.Range(-0.6f, 0.6f); // 살짝 비켜서 빠지기 안빠지면 ㅈ댐
        float dist = Random.Range(backoffRange.x, backoffRange.y);

        Vector3 want = transform.position + (away + side * sideMul).normalized * dist;

        if (NavMesh.SamplePosition(want, out var hit, 3f, NavMesh.AllAreas)) {
            dest = hit.position;
            return true;
        }
        return false;
    }


    Transform FindNearestTarget() {
        Transform best = null;
        float bestD2 = float.MaxValue;
        float r2 = senseRadius * senseRadius;

        // 1) 봇 먼저 탐색
        if (BotRuntime.Instance != null) {
            foreach (var kv in BotRuntime.Instance.agents) {
                var bot = kv.Value;
                if (!bot || bot == this) continue;
                if (bot.HP <= 0) continue;

                float d2 = (bot.transform.position - transform.position).sqrMagnitude;
                if (d2 < bestD2 && d2 <= r2) { bestD2 = d2; best = bot.transform; }
            }
        }

        // 2) 그다음 사람(플레이어)
        foreach (var kv in PlayerManager.FindAllPlayer) {
            var p = kv.Value;
            if (p == this) continue;
            if (p.HP <= 0) continue;

            float d2 = (p.transform.position - transform.position).sqrMagnitude;
            if (d2 < bestD2 && d2 <= r2) { bestD2 = d2; best = p.transform; }
        }

        return best;
    }


    void Awake() {
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // 물리 간섭 제거
        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.useGravity = false; }

        if (agent) {
            agent.enabled = false;
            agent.baseOffset = 0f;
            agent.updateRotation = true;
        }

        _initDesync = Random.Range(0.0f, 0.5f);
        _nextSenseAt = Time.time + Random.Range(0f, Mathf.Max(0.25f, preferReacquireSec));

        NickName = $"BOT_{BotId}";
    }

    protected override void OnKilled() {
        base.OnKilled();

        if (_aiLoop != null) { StopCoroutine(_aiLoop); _aiLoop = null; }
        if (agent) {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
        }
    }

    public void SetOwned(bool owned) {
        if (isOwnedByMe == owned) return;
        isOwnedByMe = owned;

        if (!agent) return;

        if (owned) {
            if (!agent.enabled) agent.enabled = true;

            Vector3 want = transform.position;
            if (!TryWarpSafe(want)) {
                if (!TryWarpSafe(_spawnFallback)) {
                    Debug.LogError($"[BotAgent] {BotId} Warp 실패: NavMesh가 씬에 없거나 BaseOffset/레이어 설정 확인 필요");
                }
            }

            if (_aiLoop != null) StopCoroutine(_aiLoop);
            _aiLoop = StartCoroutine(AIBehavior());
        } else {
            // 비오너: 에이전트 완전 off (원격 보간만)
            if (_aiLoop != null) { StopCoroutine(_aiLoop); _aiLoop = null; }
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
        }
    }

    bool TryWarpSafe(Vector3 around) {
        if (NavMesh.SamplePosition(around, out var hit, 6f, NavMesh.AllAreas)) {
            Vector3 snapped = hit.position;
            bool ok = agent.Warp(snapped);
            //Debug.Log($"[Warp] ok={ok} onMesh={agent.isOnNavMesh} to={snapped}");
            return ok && agent.isOnNavMesh;
        }
        return false;
    }


    void Update() {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) {
            if (agent && agent.enabled) {
                if (agent.isOnNavMesh) agent.ResetPath();
                agent.enabled = false;
            }
            anim?.SetFloat(HashSpeed, 0f);
            return;
        }

        // 비오너(원격)만 스냅샷 보간 적용
        if (!isOwnedByMe) {
            TickRemoteLerp();
            return;
        }

        // 오너는 에이전트 속도로 애니 갱신
        if (agent && agent.enabled) {
            anim?.SetFloat(HashSpeed, agent.velocity.magnitude, 0.5f, Time.deltaTime);
        }
    }

    IEnumerator AIBehavior() {
        while (!GameManager.isGameStart) {
            // 중간에 오너권 잃거나 비활성화되면 종료 안전장치
            if (!isOwnedByMe || agent == null) yield break;
            yield return null;
        }

        // 시작 프레임 분산
        if (_initDesync > 0f)
            yield return new WaitForSeconds(_initDesync);

        // 움직임이 부드럽고 길게 이어지도록
        agent.stoppingDistance = 0.5f;   // 살짝 늘려서 들이박지 않게
        agent.autoBraking = false;        // 접근 시 감속해서 오버슈트 방지
        agent.autoRepath = true;
        agent.acceleration = 240f; //24        // 너무 낮으면 제동이 안 걸린 듯 보임
        agent.angularSpeed = 7200f;


        // 파라미터
        float minRunDist = 4f;               // 최소 달릴 경로 길이(경로 길이 기준)
        float maxRunDist = 30f;              // 최대 시도 반경
        float minCommit = 0.5f, maxCommit = 5f;// 최소 유지 시간(목적지 고정)
        float retargetTimeout = 8f;         // 이 시간이 지나도 못 가면 목적지 재설정
        float stuckSpeed = 1.05f;            // 이 속도보다 오래 느리면 '막힘'
        float stuckSeconds = 1.5f;

        //var shortTick = new WaitForSeconds(0.2f);
        Vector3 currentTarget = transform.position;
        float commitUntil = 0f;
        float giveupAt = 0f;
        float stuckTimer = 0f;


        // 최초 목적지 선정
        PickNewTarget(out currentTarget, minRunDist, maxRunDist);
        agent.SetDestination(currentTarget);
        commitUntil = Time.time + Random.Range(minCommit, maxCommit);
        giveupAt = Time.time + retargetTimeout;

        while (isOwnedByMe && agent && agent.enabled) {
            if (HP <= 0) break;

            if (!agent.isOnNavMesh) {
                TryWarpSafe(transform.position);
                yield return new WaitForSeconds(thinkBaseInterval * 0.5f + Random.Range(-thinkJitter, thinkJitter) * 0.5f);
                continue;
            }

            bool fleeLocked = Time.time < _fleeLockUntil;
            if (fleeLocked) {
                _chasingTarget = null;   // 혹시 남아있어도 강제로 끊기
            }

            if (!fleeLocked && Time.time >= _nextSenseAt) {
                _nextSenseAt = Time.time + preferReacquireSec;

                var t = FindNearestTarget();
                if (t != null) {
                    _chasingTarget = t;

                    // 목적지를 타깃으로 갱신 (도망/정찰 중이더라도 선회)
                    Vector3 tgt = _chasingTarget.position;
                    if (NavMesh.SamplePosition(tgt, out var hit, 3f, NavMesh.AllAreas)) {
                        currentTarget = hit.position;
                        agent.SetDestination(currentTarget);
                        commitUntil = Time.time + Random.Range(minCommit * 0.5f, maxCommit * 0.7f);
                        giveupAt = Time.time + retargetTimeout;
                    }
                } else {
                    _chasingTarget = null;
                }
            }

            if (!fleeLocked && _chasingTarget != null) {
                float d = Vector3.Distance(transform.position, _chasingTarget.position);
                if (d > chaseStopRange) {
                    // 주기적으로 목적지를 갱신(타깃이 움직이니)
                    if (!agent.pathPending && ((Time.frameCount + BotId) % 3 == 0)) {
                        Vector3 tgt = _chasingTarget.position;
                        if (NavMesh.SamplePosition(tgt, out var hit, 2.5f, NavMesh.AllAreas))
                            agent.SetDestination(hit.position);
                    }
                } else {
                    // 너무 붙었으면 랜덤하게 뒤로 빠졌다가 다시 보정
                    if (Time.time >= _fleeCooldownUntil && d <= minPersonalSpace) {
                        if (TryPickBackoff(_chasingTarget.position, out var back)) {
                            agent.SetDestination(back);
                            _fleeCooldownUntil = Time.time + fleeCooldown;
                            _fleeLockUntil = Time.time + fleeLockSeconds;
                            _chasingTarget = null;                 // 현재 타깃도 즉시 끊음
                            _nextSenseAt = _fleeLockUntil;         // 그 전까지는 감지 자체를 안 함
                        }
                    } else {
                        // 아직 쿨다운이면 그냥 살짝 멈칫
                        if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                            agent.ResetPath();
                    }
                }
            }

            // 막힘 감지(속도 느림 + 경로 보유 + 아직 멀다)
            bool pathReady = !agent.pathPending && agent.hasPath;
            bool farFromTarget = agent.remainingDistance > agent.stoppingDistance + 0.4f;
            if (pathReady && farFromTarget) {
                if (agent.velocity.magnitude < stuckSpeed) stuckTimer += 0.2f;
                else stuckTimer = 0f;

                if (stuckTimer >= stuckSeconds) {
                    // 같은 목적지로 재경로 시도 → 실패면 목적지 교체
                    var p = new NavMeshPath();
                    if (!agent.CalculatePath(currentTarget, p) || p.status != NavMeshPathStatus.PathComplete) {
                        // 교체
                        PickNewTarget(out currentTarget, minRunDist, maxRunDist);
                    }
                    agent.SetDestination(currentTarget);
                    stuckTimer = 0f;
                    // 커밋과 타임아웃은 유지(지나있으면 아래 로직에서 교체됨)
                }
            }

            // 도착 판정
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f) {
                // 잠깐 머뭇임 후 다음 목적지
                float linger = 0f;//Random.Range(0.1f, 0.5f);
                yield return new WaitForSeconds(linger);

                PickNewTarget(out currentTarget, minRunDist, maxRunDist);
                agent.SetDestination(currentTarget);
                commitUntil = Time.time + Random.Range(minCommit, maxCommit);
                giveupAt = Time.time + retargetTimeout;
            }
            // 커밋 시간 지났거나 타임아웃이면 강제 재선택
            else if (Time.time >= giveupAt || (Time.time >= commitUntil && !farFromTarget)) {
                PickNewTarget(out currentTarget, minRunDist, maxRunDist);
                agent.SetDestination(currentTarget);
                commitUntil = Time.time + Random.Range(minCommit, maxCommit);
                giveupAt = Time.time + retargetTimeout;
            }

            // 애니 갱신
            //anim?.SetFloat(HashSpeed, agent.velocity.magnitude, 0.1f, Time.deltaTime);

            // 매 틱 대기
            float wait = Mathf.Max(0.02f, thinkBaseInterval + Random.Range(-thinkJitter, thinkJitter));
            yield return new WaitForSeconds(wait);
        }
    }

    // 경로 길이를 계산해서 충분히 먼 목적지만 채택
    bool PickNewTarget(out Vector3 target, float minPathLen, float maxRadius) {
        // 중심을 NavMesh에 스냅
        if (!NavMesh.SamplePosition(transform.position, out var cHit, 6f, NavMesh.AllAreas)) {
            target = transform.position;
            return false;
        }

        const int MAX_TRY = 16;
        for (int i = 0; i < MAX_TRY; i++) {
            // 균등 원반 샘플
            float u = Random.value, v = Random.value;
            float r = Mathf.Lerp(minPathLen * 0.7f, maxRadius, Mathf.Sqrt(u));
            float a = 2f * Mathf.PI * v;
            Vector3 raw = cHit.position + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);

            // NavMesh에 스냅
            if (!NavMesh.SamplePosition(raw, out var hit, 4f, NavMesh.AllAreas))
                continue;

            // 경로 계산 및 길이 합산
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(cHit.position, hit.position, NavMesh.AllAreas, path))
                continue;
            if (path.status != NavMeshPathStatus.PathComplete)
                continue;

            float len = PathLength(path);
            if (len >= minPathLen) {
                target = hit.position;
                return true;
            }
        }

        // 실패 시 근처라도
        target = cHit.position;
        return false;
    }

    float PathLength(NavMeshPath path) {
        if (path.corners == null || path.corners.Length < 2) return 0f;
        float sum = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            sum += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return sum;
    }

    // 서버 전송용 현재 상태 수집
    public void Collect(out int id, out Vector3 pos, out float yaw) {
        id = BotId;
        pos = transform.position;
        yaw = transform.eulerAngles.y;
    }

    // 서버가 내려준 월드 스냅샷(원격일 때만 사용)
    public void SetWorldPose(Vector3 pos, float yawDeg) {
        if (isOwnedByMe) return; // 오너는 NavMeshAgent가 이동
        base.SetSnapshotTarget(pos, yawDeg);
    }

    public override void OnRespawned(Vector3 worldPos, float yawDeg) {
        base.OnRespawned(worldPos, yawDeg);

        // 오너가 아니면 원격 보간만(여기서 끝)
        if (!isOwnedByMe) return;

        // 리스폰 시 에이전트 복구
        if (agent) {
            if (!agent.enabled) agent.enabled = true;
            if (!NavMesh.SamplePosition(worldPos, out var hit, 6f, NavMesh.AllAreas)) {
                hit.position = worldPos;
            }
            agent.Warp(hit.position);
            agent.ResetPath();
        }

        if (_aiLoop != null) { StopCoroutine(_aiLoop); _aiLoop = null; }
        _aiLoop = StartCoroutine(AIBehavior());
    }
}
