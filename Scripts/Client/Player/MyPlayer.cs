using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class MyPlayer : Player {
    public static event Action<MyPlayer> OnMyPlayerSpawned;
    public bool isAlive = true;

    // 네트워킹/예측용
    private Rigidbody rb;
    private readonly List<InputCmd> pending = new();   // 아직 서버가 처리 안한 입력들
    private int seq = 0;                                // 내 입력 시퀀스 번호
    private bool hasSnapshot = false;

    // 서버 권위 스냅샷 캐시 (내 캐릭 전용)
    private Vector3 authPos;
    private Quaternion authRot;
    private int lastProcessedSeq = 0;                   // 서버가 처리 완료한 내 입력 seq

    int lastServerTick = -1;

    [SerializeField] float turnSpeedDeg = 1200f; // 초당 회전 각도

    float localSpeedMul = 1f;
    float localSpeedUntil = 0f;
    Coroutine speedMulCo;

    // 액션 비트마스크
    const byte ACTION_ATTACK = 1 << 0;  // 0000_0001
    const byte ACTION_DANCE = 1 << 1;  // 0000_0010

    bool attackLatch;
    bool danceLatch;
    int pendingEmoteSlot = 0; // 0=없음
    private static readonly float INV_SQRT2 = 0.70710678f;

    private bool isCanAttack = true;

    bool isKnockbackLocked;
    float knockbackUnlockTime;


    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        anim = GetComponentInChildren<Animator>();
        StartCoroutine(Heartbeat());
        OnMyPlayerSpawned?.Invoke(this);
        PlayerUIManager.Instance?.Attach(this);
    }

    void Update() {
        if (!isAlive || GameManager.Instance.isGameOver) return;

        var kb = SettingsApplier.Current?.keys ?? new KeyBindings();

        // 공격
        if (isCanAttack && Input.GetKeyDown(kb.Get(KeyAction.Attack))) {
            attackLatch = true;
            isCanAttack = false;
            StartCoroutine(SetAttackCoolDown());
            //SoundManager.I?.Play3D(SfxId.Whoosh, this.transform.position);
        }

        // 춤들
        if (Input.GetKeyDown(kb.Get(KeyAction.Dance1))) {
            pendingEmoteSlot = 1;
            danceLatch = true;
        }
        if (Input.GetKeyDown(kb.Get(KeyAction.Dance2))) {
            pendingEmoteSlot = 2;
            danceLatch = true;
        }
        if (Input.GetKeyDown(kb.Get(KeyAction.Dance3))) {
            pendingEmoteSlot = 3;
            danceLatch = true;
        }
        if (Input.GetKeyDown(kb.Get(KeyAction.Dance4))) {
            pendingEmoteSlot = 4;
            danceLatch = true;
        }
    }

    void FixedUpdate() {
        // 죽었으면 입력/전송/로컬예측 모두 중단
        if (!isAlive || GameManager.Instance.isGameOver) {
            pending.Clear();
            anim?.SetFloat(HashSpeed, 0f);
            if (rb) {
                if (!rb.isKinematic) {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            return;
        }

        // 1) 틱 시작 근데 !!! 혹시 온 스냅샷 있으면 먼저 리컨
        if (hasSnapshot) {
            Reconcile();
            hasSnapshot = false;
        }

        // 넉백 락: 일정 시간 동안 입력/이동 막기
        if (isKnockbackLocked) {
            if (Time.time >= knockbackUnlockTime) {
                isKnockbackLocked = false;
                anim?.SetBool("Dead", false);
            } else {
                anim?.SetFloat(HashSpeed, 0f);
                if (rb && !rb.isKinematic) {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                return; // 이 틱은 여기서 끝, 입력/예측 안 함
            }
        }


        if (!GameManager.isGameStart) {
            return;
        }

        // 2) 입력 수집(틱 단위) → 커맨드 생성/버퍼/전송
        float ix = Input.GetAxisRaw("Horizontal"); // -1/0/1
        float iz = Input.GetAxisRaw("Vertical");   // -1/0/1

        var cmd = new InputCmd {
            seq = ++seq,
            moveX = (sbyte)Mathf.Clamp(Mathf.RoundToInt(ix), -1, 1),
            moveZ = (sbyte)Mathf.Clamp(Mathf.RoundToInt(iz), -1, 1),
            actions = 0
        };

        if (attackLatch) { 
            cmd.actions |= ACTION_ATTACK; 
            attackLatch = false;
            anim?.SetTrigger("Attack");
        }
        if (danceLatch) { cmd.actions |= ACTION_DANCE; danceLatch = false; }

        pending.Add(cmd);
        SendInput(cmd);       // 서버로 이 틱의 입력 전송(TCP)

        // 3) 로컬 예측 적용 (서버와 동일 수식)
        ApplyLocally(cmd, Time.fixedDeltaTime);
    }

    // 로컬 예측 이동: 서버와 동일한 결정적 수식 사용
    private void ApplyLocally(InputCmd cmd, float dt) {
        // 1) 방향 정규화 (대각선 속도 보정)
        int ix = cmd.moveX;
        int iz = cmd.moveZ;

        // (-1|0|1, -1|0|1) 조합에 대해 대각선만 보정
        float dx = ix;
        float dz = iz;
        if (ix != 0 && iz != 0) { // 대각선
            dx *= INV_SQRT2;
            dz *= INV_SQRT2;
        }

        // 2) 최종 속도 벡터
        float vx = dx * moveSpeed * localSpeedMul;
        float vz = dz * moveSpeed * localSpeedMul;

        var p = rb.position;

        // 바깥으로 나가려는 축의 속도만 0으로
        if (p.x <= LevelData.MAP_MIN_X && vx < 0f) vx = 0f;
        if (p.x >= LevelData.MAP_MAX_X && vx > 0f) vx = 0f;
        if (p.z <= LevelData.MAP_MIN_Z && vz < 0f) vz = 0f;
        if (p.z >= LevelData.MAP_MAX_Z && vz > 0f) vz = 0f;

        // 혹시 이미 살짝 넘어가 있으면 현재 좌표만 즉시 클램프
        if (p.x < LevelData.MAP_MIN_X) rb.position = new Vector3(LevelData.MAP_MIN_X, p.y, p.z);
        else if (p.x > LevelData.MAP_MAX_X) rb.position = new Vector3(LevelData.MAP_MAX_X, p.y, p.z);
        p = rb.position; // x 고정 후 최신화
        if (p.z < LevelData.MAP_MIN_Z) rb.position = new Vector3(p.x, p.y, LevelData.MAP_MIN_Z);
        else if (p.z > LevelData.MAP_MAX_Z) rb.position = new Vector3(p.x, p.y, LevelData.MAP_MAX_Z);

        // 3) 이동 & 회전
        Vector3 move = new Vector3(vx, 0f, vz);
        Vector3 next = rb.position + move * dt;
        //rb.MovePosition(next); // 이거 쓰면 내 캐릭 떨림
        rb.linearVelocity = new Vector3(vx, 0, vz); // 이걸로 하고 리컨 새걸로

        if (move.sqrMagnitude > 0.0001f) {
            Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeedDeg * dt);
            rb.MoveRotation(newRot);
        }

        // 4) 애니 속도는 실제 이동속도 크기로
        float speed = new Vector3(vx, 0f, vz).magnitude;
        anim?.SetFloat(HashSpeed, speed, 0.1f, dt);
    }

    public void ApplyLocalBuff(SupplyEffect eff, float seconds) {
        if (eff == SupplyEffect.SpeedUp) {
            localSpeedMul = 2f;
            localSpeedUntil = Time.time + seconds;
            if (speedMulCo != null) StopCoroutine(speedMulCo);
            speedMulCo = StartCoroutine(CoSpeedMulWatch());
        }
    }

    IEnumerator CoSpeedMulWatch() {
        while (Time.time < localSpeedUntil) yield return null;
        localSpeedMul = 1f;
        speedMulCo = null;
    }

    // 서버 스냅샷 핸들러(패킷 수신부에서 호출)
    public void OnSnapshotFromServer(S_Snapshot snap) {
        if (snap.serverTick == lastServerTick) {
            //Debug.Log("d");
            return;
        }
        
        lastServerTick = snap.serverTick;

        // 내 엔티티만 골라서 읽음!! 내것만 ㅋ
        authPos = new Vector3(snap.posX, snap.posY, snap.posZ);
        authRot = Quaternion.Euler(0, snap.yaw, 0); // 예시
        lastProcessedSeq = snap.lastProcessedInputSeq;
        hasSnapshot = true;
    }

    // 리컨: 서버 권위 상태로 스냅 후, 아직 미확정 입력들만 재적용
    Vector3 _reconVel;                // 보정용 내부 속도 (SmoothDamp)
    float _lastReconAt;             // 리컨 레이트 제한
    [SerializeField] float reconHz = 10f;         // 리컨 최대 빈도(초당)
    [SerializeField] float posDeadzone = 0.08f;   // 이 거리 이하면 무시
    [SerializeField] float posSoftZone = 0.60f;  // 이 거리 이하면 소프트 보정
    [SerializeField] float posHardSnap = 2.50f;  // 이 이상 벌어지면 스냅
    [SerializeField] float posSmoothTime = 0.12f; // 소프트 보정 시간(초)

    [SerializeField] float rotDeadDeg = 2f;
    [SerializeField] float rotSoftDeg = 15f;
    [SerializeField] float rotHardDeg = 60f;
    [SerializeField] float rotMaxDegPerSec = 360f;
    private void Reconcile() {
        // 레이트 제한(예: 최대 10Hz로만 리컨; 스냅샷은 더 자주 와도 OK)
        if (Time.time - _lastReconAt < 1f / Mathf.Max(1f, reconHz))
            return;
        _lastReconAt = Time.time;

        // 1) 위치 오차 계산
        Vector3 curPos = rb.position;
        Vector3 err = authPos - curPos;
        float d = err.magnitude;

        if (d >= posHardSnap) {
            // 하드 스냅
            rb.position = authPos;
            _reconVel = Vector3.zero;
        } else if (d >= posSoftZone) {
            Vector3 next = Vector3.SmoothDamp(curPos, authPos, ref _reconVel, posSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
            rb.MovePosition(next);
        } else if (d > posDeadzone) {
            Vector3 biasVel = err * (1f / Mathf.Max(0.08f, posSmoothTime));
            rb.linearVelocity += Vector3.ClampMagnitude(biasVel, 1.0f); // 최대 1 m/s 정도만 보정
        }
        // deadzone 이하면 아무 것도 안 함

        // 2) 회전 보정
        float ang = Quaternion.Angle(transform.rotation, authRot);
        if (ang >= rotHardDeg) {
            transform.rotation = authRot;
        } else if (ang >= rotSoftDeg) {
            float step = rotMaxDegPerSec * Time.fixedDeltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, authRot, step);
        } else if (ang > rotDeadDeg) {
            float step = (rotMaxDegPerSec * 0.5f) * Time.fixedDeltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, authRot, step);
        }

        pending.RemoveAll(cmd => cmd.seq <= lastProcessedSeq);
    }

    // 입력 전송 (TCP)
    private void SendInput(InputCmd cmd) {
        C_Input pkt = new C_Input();
        pkt.seq = cmd.seq;
        pkt.moveX = cmd.moveX;
        pkt.moveZ = cmd.moveZ;
        pkt.action = cmd.actions;
        pkt.yaw = transform.eulerAngles.y; // 현재 회전 각도 그대로
        pkt.emoteSlot = (byte)pendingEmoteSlot;
        NetworkManager.Send(pkt.Write());
        pendingEmoteSlot = 0;
    }


    IEnumerator Heartbeat() {
        var wait = new WaitForSeconds(0.5f);
        while (true) {
            yield return wait;
        }
    }

    IEnumerator SetAttackCoolDown() {
        float time = 0f;
        while (time < LevelData.ATTACK_COOLDOWN) {
            time += Time.deltaTime;    
            yield return null;
        }
        isCanAttack = true;
    }

    // 사망/리스폰 시 플래그 동기화
    protected override void OnKilled() {
        base.OnKilled();
        isAlive = false;
        pending.Clear();
    }

    public override void OnRespawned(Vector3 worldPos, float yawDeg) {
        base.OnRespawned(worldPos, yawDeg);
        isAlive = true;
    }

    public override void OnEmoteBySku(string sku) {
        base.OnEmoteBySku(sku);
    }

    public void OnRespawnedLocal(Vector3 worldPos, float yawDeg) {
        // 즉시 텔레포트(리콘실 기준 좌표도 강제 세팅)
        if (rb) {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = worldPos;
            rb.rotation = Quaternion.Euler(0f, yawDeg, 0f);
        } else {
            transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yawDeg, 0f));
        }
    }

    // 네트워크 입력 구조
    private struct InputCmd {
        public int seq;
        public sbyte moveX;
        public sbyte moveZ;
        public byte actions; // 비트마스크
    }

}