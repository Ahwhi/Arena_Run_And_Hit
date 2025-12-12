using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraManager : MonoBehaviour {
    public static CameraManager Instance { get; private set; }

    [Header("Follow")]
    public Vector3 offset = new Vector3(0, 12, -16);
    public float followSmoothTime = 0.15f;
    public float maxFollowSpeed = Mathf.Infinity;

    [Header("View Presets")]
    public Vector3 defaultFollowOffset = new Vector3(0, 12, -16);
    public Vector3 topViewFollowOffset = new Vector3(0, 18, -10);
    [Tooltip("탑뷰 시 선택 시 기본 피치에서 추가되는 각도")]
    public float topViewPitchDelta = 12f;

    [Header("Spectate")]
    public float refreshSpectateListInterval = 0.5f;
    public bool includeSelfWhenAlive = false;

    [Header("Intro Orbit")]
    [Tooltip("게임 시작 카운트다운 동안 상공에서 도는 연출을 쓸지 여부")]
    public bool useIntroOrbit = true;
    [Tooltip("플레이어 분포 반경 + 여유거리(기본 반경 계산용)")]
    public float introRadiusMargin = 6f;
    [Tooltip("카운트다운 정보가 애매할 때 기본 연출 길이(초)")]
    public float defaultIntroDuration = 4f;

    [Tooltip("인트로 카메라 피치 각도 (지평선 기준 위쪽 각도)")]
    public float introPitchDeg = 35f;
    [Tooltip("플레이어 분포 반경 대비 카메라 거리 배수")]
    public float introDistanceMul = 1.6f;
    [Tooltip("인트로 최소 거리")]
    public float introMinDistance = 18f;
    [Tooltip("인트로 최대 거리")]
    public float introMaxDistance = 45f;
    [Tooltip("인트로가 끝난 뒤 내 플레이어 시점으로 넘어가는 블렌딩 시간")]
    public float introBlendTime = 0.8f;



    enum FollowMode { Self, Spectate }
    FollowMode _mode = FollowMode.Self;

    Transform _target;
    Vector3 _vel;

    readonly List<Player> _spectateList = new();
    int _spectateIndex = -1;
    float _nextRefreshAt = 0f;

    bool _isFocus = false;
    Transform _focusTarget;
    Vector3 _focusOffset;
    float _focusLerp = 0.2f;
    Coroutine _focusCo;

    bool _isIntro = false;
    float _introTimer = 0f;
    float _introTotalTime = 0f;
    Vector3 _introCenter;
    float _introRadius;
    Quaternion _defaultRotation;

    bool _isIntroBlend = false;
    float _introBlendTimer = 0f;
    Vector3 _introBlendStartPos;
    Quaternion _introBlendStartRot;

    void Awake() {
        Instance = this;
        _defaultRotation = transform.rotation;
        offset = defaultFollowOffset;
    }

    void OnEnable() {
        MyPlayer.OnMyPlayerSpawned += RegisterTargetPlayer;
    }

    void OnDisable() {
        MyPlayer.OnMyPlayerSpawned -= RegisterTargetPlayer;
    }

    void Start() {
        if (PlayerManager.MyPlayer)
            RegisterTargetPlayer(PlayerManager.MyPlayer);

        var mode = SettingsApplier.Current != null
            ? SettingsApplier.Current.cameraViewMode
            : CameraViewMode.Default;
        ApplyViewMode(mode);

        if (useIntroOrbit && GameManager.countDown > 0 && !GameManager.isGameStart) {
            PlayIntroOrbit(GameManager.countDown);
        }
    }

    void RegisterTargetPlayer(MyPlayer me) {
        _mode = FollowMode.Self;
        _target = me ? me.transform : null;
        UpdateWatchHud();
    }

    void LateUpdate() {
        if (_isIntro) {
            UpdateIntroCamera();
            return;
        }

        if (_isIntroBlend) {
            UpdateIntroBlend();
            return;
        }

        if (_isFocus && _focusTarget) {
            Vector3 wantPos = _focusTarget.position + _focusOffset;
            transform.position = Vector3.SmoothDamp(
                transform.position, wantPos, ref _vel,
                _focusLerp, maxFollowSpeed, Time.deltaTime
            );
            return;
        }

        if (_mode == FollowMode.Spectate && Time.time >= _nextRefreshAt) {
            _nextRefreshAt = Time.time + refreshSpectateListInterval;
            RebuildSpectateList(keepCurrentTarget: true);
        }

        if (_mode == FollowMode.Spectate) {
            if (!IsValidSpectateTarget(_target)) {
                SpectateNext(+1);
            }
        } else { // Self
            if (!_target && PlayerManager.MyPlayer) {
                _target = PlayerManager.MyPlayer.transform;
            }
        }

        if (!_target) return;

        Vector3 want = _target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position, want, ref _vel,
            followSmoothTime, maxFollowSpeed, Time.deltaTime
        );

    }

    public void EnterSpectate(int? preferredId = null) {
        _mode = FollowMode.Spectate;
        _target = null;
        RebuildSpectateList();

        if (preferredId.HasValue) {
            int idx = _spectateList.FindIndex(p => p && p.PlayerId == preferredId.Value);
            if (idx >= 0) {
                _spectateIndex = idx;
                _target = _spectateList[idx].transform;
                return;
            }
        }

        if (_spectateList.Count > 0) {
            _spectateIndex = 0;
            _target = _spectateList[0].transform;
            UpdateWatchHud();
        } else {
            ExitSpectateToSelf();
        }
    }

    public void ExitSpectateToSelf() {
        _mode = FollowMode.Self;
        _spectateList.Clear();
        _spectateIndex = -1;
        _target = PlayerManager.MyPlayer ? PlayerManager.MyPlayer.transform : null;
        UpdateWatchHud();
    }

    public void SpectateNext(int dir = +1) {
        if (_mode != FollowMode.Spectate) return;
        if (_spectateList.Count == 0) { ExitSpectateToSelf(); return; }

        int safety = 0;
        do {
            _spectateIndex = ((_spectateIndex + dir) % _spectateList.Count + _spectateList.Count) % _spectateList.Count;
            var cand = _spectateList[_spectateIndex];
            if (IsValidSpectateTarget(cand ? cand.transform : null)) {
                _target = cand.transform;
                UpdateWatchHud();
                return;
            }
            safety++;
        } while (safety <= _spectateList.Count);


        ExitSpectateToSelf();
    }

    public bool IsSpectating => _mode == FollowMode.Spectate;



    void RebuildSpectateList(bool keepCurrentTarget = false) {
        _spectateList.Clear();

        var all = PlayerManager.FindAllPlayer?.Values;
        if (all == null) return;

        foreach (var p in all) {
            if (!p) continue;
            if (!includeSelfWhenAlive && PlayerManager.MyPlayer && p.PlayerId == PlayerManager.MyPlayer.PlayerId) {
                continue;
            }
            if (!IsValidSpectateTarget(p.transform)) continue;
            _spectateList.Add(p);
        }

        if (keepCurrentTarget && _target) {
            int idx = _spectateList.FindIndex(pl => pl && pl.transform == _target);
            if (idx >= 0) {
                _spectateIndex = idx;
                UpdateWatchHud();
                return;
            }
        }

        if (_spectateList.Count == 0) {
            _spectateIndex = -1;
            _target = null;
        } else {
            _spectateIndex = Mathf.Clamp(_spectateIndex, 0, _spectateList.Count - 1);
            _target = _spectateList[_spectateIndex].transform;
            UpdateWatchHud();
        }
    }

    bool IsValidSpectateTarget(Transform t) {
        if (!t) return false;
        if (!t.gameObject.activeInHierarchy) return false;

        var p = t.GetComponent<Player>();
        if (!p) return false;

        var type = p.GetType();
        var prop = type.GetProperty("IsAlive");
        if (prop != null && prop.PropertyType == typeof(bool))
            return (bool)prop.GetValue(p);

        var field = type.GetField("isAlive");
        if (field != null && field.FieldType == typeof(bool))
            return (bool)field.GetValue(p);

        var hpProp = type.GetProperty("HP");
        if (hpProp != null && hpProp.PropertyType == typeof(int))
            return ((int)hpProp.GetValue(p)) > 0;

        return true;
    }

    void UpdateWatchHud() {
        var ui = GameManager.Instance;
        if (ui == null || ui.watchPlayerNameText == null) return;

        if (_mode == FollowMode.Spectate && _target) {
            var p = _target.GetComponent<Player>();
            ui.watchPlayerNameText.text = p ? p.NickName : "";
            ui.watchPlayerNameText.gameObject.SetActive(true);
        } else {
            ui.watchPlayerNameText.text = "";
            ui.watchPlayerNameText.gameObject.SetActive(false);
        }
    }

    public void FocusToTarget(Transform target, float focusTime = 2f, float lerp = 0.2f, Vector3? customOffset = null) {
        if (target == null) return;

        // 인트로 중이면 우승 연출이 우선이니 인트로 종료
        if (_isIntro) {
            EndIntroOrbit(false);
        }

        // 기존 포커스 돌고 있으면 끊기
        if (_focusCo != null)
            StopCoroutine(_focusCo);

        _focusTarget = target;
        _focusOffset = customOffset.HasValue ? customOffset.Value : new Vector3(0, 6.5f, -8f); // 우승 연출용 살짝 가까운 값
        _focusLerp = Mathf.Max(0.01f, lerp);
        _isFocus = true;

        _focusCo = StartCoroutine(CoFocusHold(focusTime));
    }

    IEnumerator CoFocusHold(float focusTime) {
        float t = 0f;
        while (t < focusTime) {
            t += Time.deltaTime;
            if (_focusTarget == null) break;
            yield return null;
        }
        _isFocus = false;
        _focusTarget = null;
        _focusCo = null;
    }


    public void PlayIntroOrbit(int countdownSec) {
        if (!useIntroOrbit) return;
        if (GameManager.isGameStart) return;
        if (_isIntro) return;

        ComputeIntroCenterAndRadius(out _introCenter, out _introRadius);

        _introRadius = Mathf.Max(_introRadius * 1.3f, 12f);

        _introTimer = 0f;
        float rawDuration = countdownSec > 0 ? countdownSec - 0.7f : defaultIntroDuration;
        _introTotalTime = Mathf.Max(1f, rawDuration);

        _isIntro = true;
        _isIntroBlend = false;

        _mode = FollowMode.Self;
    }

    void UpdateIntroCamera() {
        if (GameManager.isGameStart || (GameManager.countDown <= 1 && GameManager.countDown >= 0)) {
            EndIntroOrbit(true);
            return;
        }

        if (_introTotalTime <= 0f) _introTotalTime = defaultIntroDuration;

        _introTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_introTimer / _introTotalTime);
        float e = t * t * (3f - 2f * t);

        float angle = Mathf.Lerp(0f, 360f, e);
        float rad = angle * Mathf.Deg2Rad;

        // 반경 기반으로 카메라 거리/높이 계산
        float horizDist = Mathf.Clamp(_introRadius * introDistanceMul, introMinDistance, introMaxDistance);
        float pitchRad = introPitchDeg * Mathf.Deg2Rad;
        float height = Mathf.Tan(pitchRad) * horizDist;

        Vector3 orbitDir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        Vector3 targetPos = _introCenter + orbitDir * horizDist + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref _vel,
            followSmoothTime, maxFollowSpeed, Time.deltaTime
        );

        // 항상 중심(플레이어들 뭉탱이)을 바라보도록 회전
        Vector3 lookDir = _introCenter - transform.position;
        if (lookDir.sqrMagnitude > 0.001f) {
            Quaternion wantRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, wantRot, 4f * Time.deltaTime);
        }
    }

    void EndIntroOrbit(bool smoothToSelf) {
        if (!_isIntro) return;
        _isIntro = false;
        _introTimer = 0f;
        _vel = Vector3.zero;

        _isIntroBlend = false;

        var mode = SettingsApplier.Current != null
            ? SettingsApplier.Current.cameraViewMode
            : CameraViewMode.Default;

        if (smoothToSelf && PlayerManager.MyPlayer) {
            _introBlendStartPos = transform.position;
            _introBlendStartRot = transform.rotation;
            _introBlendTimer = 0f;
            _isIntroBlend = true;

            _target = PlayerManager.MyPlayer.transform;
            offset = GetOffsetForMode(mode);
        } else {
            if (PlayerManager.MyPlayer) {
                _target = PlayerManager.MyPlayer.transform;
                transform.position = _target.position + offset;
                //transform.rotation = _defaultRotation;
                transform.rotation = GetRotationForMode(mode);
            }
        }
    }

    void UpdateIntroBlend() {
        if (!PlayerManager.MyPlayer) {
            _isIntroBlend = false;
            return;
        }

        _introBlendTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_introBlendTimer / introBlendTime);
        float e = t * t * (3f - 2f * t);

        var mode = SettingsApplier.Current != null
            ? SettingsApplier.Current.cameraViewMode
            : CameraViewMode.Default;

        Vector3 targetPos = PlayerManager.MyPlayer.transform.position + offset;
        Quaternion targetRot = GetRotationForMode(mode);
        //Quaternion targetRot = _defaultRotation;

        transform.position = Vector3.Lerp(_introBlendStartPos, targetPos, e);
        transform.rotation = Quaternion.Slerp(_introBlendStartRot, targetRot, e);

        if (t >= 1f) {
            _isIntroBlend = false;
            transform.position = targetPos;
            transform.rotation = targetRot;
            _vel = Vector3.zero;
        }
    }

    void ComputeIntroCenterAndRadius(out Vector3 center, out float radius) {
        center = Vector3.zero;
        radius = 10f;

        var all = PlayerManager.FindAllPlayer?.Values;
        if (all == null) {
            if (PlayerManager.MyPlayer)
                center = PlayerManager.MyPlayer.transform.position;
            return;
        }

        int count = 0;
        foreach (var p in all) {
            if (!p) continue;
            center += p.transform.position;
            count++;
        }
        if (count > 0) center /= count;

        float maxSq = 0f;
        foreach (var p in all) {
            if (!p) continue;
            Vector3 pos = p.transform.position;
            Vector2 flat = new Vector2(pos.x - center.x, pos.z - center.z);
            float sq = flat.sqrMagnitude;
            if (sq > maxSq) maxSq = sq;
        }

        radius = Mathf.Sqrt(maxSq) + introRadiusMargin;
    }

    Vector3 GetOffsetForMode(CameraViewMode mode) {
        return (mode == CameraViewMode.TopView) ? topViewFollowOffset : defaultFollowOffset;
    }

    Quaternion GetRotationForMode(CameraViewMode mode) {
        var e = _defaultRotation.eulerAngles;
        if (mode == CameraViewMode.TopView)
            e.x += topViewPitchDelta;
        return Quaternion.Euler(e);
    }

    public void ApplyViewMode(CameraViewMode mode) {
        offset = GetOffsetForMode(mode);
        if (!_isIntro && !_isIntroBlend && !_isFocus && _target) {
            transform.position = _target.position + offset;
            transform.rotation = GetRotationForMode(mode);
        }
    }

}
