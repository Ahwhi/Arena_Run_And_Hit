using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviour {
    public int PlayerId { get; set; }
    public string NickName { get; set; }
    public int MaxHP { get; private set; } = 100;
    public int HP { get; private set; } = 100;

    public int Kill {  get; set; } = 0;
    public int Death { get; set; } = 0;

    [SerializeField] private GameObject hitVfxPrefab; // 선택
    [SerializeField] private SkinnedMeshRenderer hurtFlashRenderer; // 선택(머티리얼 플래시)

    public TextMeshProUGUI nameText { get; set; }
    public Transform uiAnchor;            // 머리 위에 빈 오브젝트 달아서 할당
    public Vector3 uiOffset = new Vector3(0f, 4.0f, 0f); // 앵커가 없을 때 예비 오프셋

    public Rigidbody playerRigidbody;
    public float moveSpeed = 8f;

    protected Animator anim;
    public static readonly int HashSpeed = Animator.StringToHash("Speed");
    static readonly int HashAngularSpeed = Animator.StringToHash("AngularSpeed"); // 선택

    Vector3 _targetPos;
    Quaternion _targetRot;
    bool _hasTarget;

    [SerializeField] float posLerp = 15f;
    [SerializeField] float rotLerp = 15f;

    Vector3 _lastPos;
    Quaternion _lastRot;
    bool _hasLastPose;

    public int ColorIndex { get; set; } = -1;
    static readonly Color[] Palette = new Color[] {
        new Color(1.00f,0.20f,0.20f), // 0 빨강
        new Color(0.20f,0.90f,0.30f), // 1 초록
        new Color(0.25f,0.55f,1.00f), // 2 파랑
        new Color(0.65f,0.35f,0.95f), // 3 보라
        new Color(0.98f,0.85f,0.18f), // 4 노랑
        new Color(1.00f,0.55f,0.10f), // 5 주황
        new Color(0.10f,0.90f,0.90f), // 6 시안
        new Color(1.00f,0.20f,0.70f), // 7 마젠타
        new Color(0.60f,1.00f,0.20f), // 8 라임
        new Color(0.90f,0.90f,0.90f), // 9 화이트
    };
    SkinnedMeshRenderer bodyRend;
    MaterialPropertyBlock _mpb;

    bool _isFlashing = false;

    public void ApplyServerHealth(int hp, int maxHp = -1) {
        if (maxHp > 0) MaxHP = maxHp;
        HP = Mathf.Clamp(hp, 0, MaxHP);
        if (HP <= 0) OnKilled();
    }

    public void OnHitFromServer(int damage, int hpAfter, bool killed) {
        // 피격 리액션 연출
        anim?.SetTrigger("Hit");
        if (hitVfxPrefab) Instantiate(hitVfxPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
        StartCoroutine(HurtFlash());

        ApplyServerHealth(hpAfter);
        if (killed) OnKilled();
    }

    IEnumerator HurtFlash() {
        if (_isFlashing) yield break;
        _isFlashing = true;

        if (!bodyRend) bodyRend = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (!bodyRend) yield break;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        var shared = bodyRend.sharedMaterial;
        if (shared && !shared.IsKeywordEnabled("_EMISSION"))
            shared.EnableKeyword("_EMISSION");

 
        bodyRend.GetPropertyBlock(_mpb);
        Color baseAlbedo = _mpb.GetColor("_Color");
        if (baseAlbedo.maxColorComponent == 0f && shared) baseAlbedo = shared.GetColor("_Color");
        if (baseAlbedo.maxColorComponent == 0f) baseAlbedo = Color.white;

        Color baseEmit = _mpb.GetColor("_EmissionColor");

        float duration = 0.16f;   // 160ms
        float t = 0f;

        float emitPeak = 1.0f;
        float albedoBoost = 0.5f; // 흰색으로 당기는 비율(0~1)

        while (t < duration) {
            t += Time.deltaTime;
            float n = t / duration;
            float k = 1f - n;
            k = k * k;

            Color flashAlbedo = Color.Lerp(baseAlbedo, Color.white, Mathf.Clamp01(k * albedoBoost));
            _mpb.SetColor("_Color", flashAlbedo);

            _mpb.SetColor("_EmissionColor", Color.white * (emitPeak * k));

            bodyRend.SetPropertyBlock(_mpb);
            yield return null;
        }

        _mpb.SetColor("_Color", baseAlbedo);
        _mpb.SetColor("_EmissionColor", baseEmit);
        bodyRend.SetPropertyBlock(_mpb);
        _isFlashing = false;
    }

    protected virtual void OnKilled() {
        anim?.SetBool("Dead", true);
        var col = GetComponent<Collider>(); if (col) col.enabled = false;
        var rb = GetComponent<Rigidbody>(); if (rb) rb.isKinematic = true;
    }

    public virtual void OnRespawned(Vector3 worldPos, float yawDeg) {
        anim?.SetBool("Dead", false);
        var col = GetComponent<Collider>(); if (col) col.enabled = true;
        var rb = GetComponent<Rigidbody>(); if (rb) rb.isKinematic = this is not MyPlayer;
        transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yawDeg, 0f));
    }

    public void OnTriggerAttack() {
        anim?.SetTrigger("Attack");
    }

    void Start() {
        playerRigidbody = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>(); // 자식에 있을 수도 있어서 InChildren
        _lastPos = transform.position;
        _lastRot = transform.rotation;
        _hasLastPose = true;
        PlayerUIManager.Instance?.Attach(this);

        if (!hurtFlashRenderer)
            hurtFlashRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);

        // M_Body 렌더러 캐시
        if (!bodyRend) bodyRend = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        if (ColorIndex >= 0) ApplyColorIndex(ColorIndex);
    }

    public void ApplyColorIndex(int idx) {
        ColorIndex = idx;
        if (!bodyRend) bodyRend = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (bodyRend == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        var c = Palette[Mathf.Clamp(idx, 0, Palette.Length - 1)];

        bodyRend.GetPropertyBlock(_mpb);
        _mpb.SetColor("_Color", c);
        _mpb.SetColor("_EmissionColor", Color.black);
        bodyRend.SetPropertyBlock(_mpb);
    }

    public void SetSnapshotTarget(Vector3 pos, float yawDeg) {
        _targetPos = pos;
        _targetRot = Quaternion.Euler(0f, yawDeg, 0f);
        _hasTarget = true;
    }

    protected void TickRemoteLerp() {
        // === 원격 보간 ===
        if (_hasTarget && !(this is MyPlayer)) {
            Vector3 before = transform.position;
            Quaternion beforeRot = transform.rotation;

            transform.position = Vector3.Lerp(before, _targetPos, Time.deltaTime * posLerp);
            transform.rotation = Quaternion.Slerp(beforeRot, _targetRot, Time.deltaTime * rotLerp);

            // --- 원격 Speed 계산 (거리/초) ---
            if (_hasLastPose) {
                float dt = Time.deltaTime > 0 ? Time.deltaTime : 0.0167f;
                float speed = Vector3.Distance(transform.position, _lastPos) / dt;

                // 살짝 감쇠를 주어 튀는 값 방지
                anim?.SetFloat(HashSpeed, speed, 0.1f, Time.deltaTime);

                float ang = Quaternion.Angle(transform.rotation, _lastRot);
                float angularSpeed = ang / dt;
                anim?.SetFloat(HashAngularSpeed, angularSpeed, 0.1f, Time.deltaTime);
            }

            _lastPos = transform.position;
            _lastRot = transform.rotation;
            _hasLastPose = true;
        }
    }

    void Update() {
        TickRemoteLerp();
    }

    public virtual void OnEmote(int emoteId) {
        if (HP <= 0) return;
        switch (emoteId) {
            case 1: anim?.SetTrigger("Dance"); break;
        }
    }

    public virtual void OnEmoteBySku(string sku) {
        if (HP <= 0) return;

        anim?.SetTrigger(sku);
    }
}
