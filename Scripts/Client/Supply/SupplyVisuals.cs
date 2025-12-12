using System.Collections;
using UnityEngine;

public class SupplyVisuals : MonoBehaviour {
    // 선택: 여기 슬롯에 프리팹 연결하면 자동으로 쓰고, 없으면 GetComponent로 대체 시도
    [Header("Optional Prefabs")]
    public GameObject shieldBubblePrefab;   // 무적 시 둘러싸는 버블
    public GameObject speedTrailPrefab;     // 속도 잔상
    public GameObject vomitAuraPrefab;      // 구토 오라
    public GameObject healBurstPrefab;      // 힐 번쩍

    // 내부 참조
    Transform _t;
    Animator _anim;
    SkinnedMeshRenderer _body;
    MaterialPropertyBlock _mpb;

    GameObject _shield;
    GameObject _speedFx;
    GameObject _vomitFx;
    Coroutine _giantCo, _colorCo;

    void Awake() {
        _t = transform;
        _anim = GetComponentInChildren<Animator>();
        _body = GetComponentInChildren<SkinnedMeshRenderer>(true);
        _mpb = new MaterialPropertyBlock();
    }

    // ===== Public API =====
    public void PlayHealFlash() {
        if (healBurstPrefab) Instantiate(healBurstPrefab, _t.position + Vector3.up * 1.2f, Quaternion.identity);
        StartCoroutine(FlashEmissionOnce(Color.white, 4f, 0.16f));
    }

    public void PlayRangeUp(float duration) {
        // 가벼운 시각: 손/무기 Emission을 노란빛으로
        //StartReplaceEmission(Color.yellow, 2.2f, duration);
        // 공격 트레일 있으면 굵기↑
        var trail = GetTrail();
        if (trail) StartCoroutine(TrailWidthFor(trail, width: 0.6f, duration));
    }

    public void PlaySpeedUp(float duration) {
        if (!_speedFx && speedTrailPrefab) _speedFx = Instantiate(speedTrailPrefab, _t);
        EnableObjFor(_speedFx, duration);
        // 애니 조금 빠르게
        StartCoroutine(SetAnimParamFor("SpeedMult", 2f, duration));
    }

    IEnumerator SetAnimParamFor(string param, float value, float dur) {
        if (!_anim) yield break;
        float baseVal = _anim.GetFloat(param);
        _anim.SetFloat(param, value);
        yield return new WaitForSeconds(dur);
        _anim.SetFloat(param, baseVal);
    }

    public void PlayDamageUp(float duration) {
        StartReplaceEmission(new Color(1f, 0.25f, 0.25f), 2.8f, duration);
        var trail = GetTrail();
        if (trail) StartCoroutine(TrailColorFor(trail, new Color(1f, 0.2f, 0.1f), duration));
    }

    public void PlayVomitCountdown(float duration) {
        if (!_vomitFx && vomitAuraPrefab) _vomitFx = Instantiate(vomitAuraPrefab, _t);
        EnableObjFor(_vomitFx, duration);
    }

    public void PlayInvincible(float duration) {
        if (!_shield && shieldBubblePrefab) _shield = Instantiate(shieldBubblePrefab, _t);
        EnableObjFor(_shield, duration);
        // 피격 플래시 약화는 서버/피격 로직과 충돌 없도록 시각만
        StartReplaceEmission(new Color(0.9f, 0.9f, 0.4f), 1.8f, duration);
    }

    public void PlayGiant(float duration) {
        if (_giantCo != null) StopCoroutine(_giantCo);
        _giantCo = StartCoroutine(ScaleForDuration(_t, 1.5f, duration));
    }

    public void PlayKnockbackReady(float duration) {
        // 대충 몸에 파란 기운 도는 느낌으로 DamageUp이랑 비슷하게
        StartReplaceEmission(new Color(0.4f, 0.8f, 1f), 2.5f, duration);
    }

    // ===== Helpers =====
    IEnumerator FlashEmissionOnce(Color color, float peak, float dur) {
        if (!_body) yield break;
        _body.GetPropertyBlock(_mpb);
        Color baseEmit = _mpb.GetColor("_EmissionColor");
        float t = 0f;
        while (t < dur) {
            t += Time.deltaTime;
            float k = 1f - (t / dur);
            k *= k;
            _mpb.SetColor("_EmissionColor", color * (peak * k));
            _body.SetPropertyBlock(_mpb);
            yield return null;
        }
        _mpb.SetColor("_EmissionColor", baseEmit);
        _body.SetPropertyBlock(_mpb);
    }

    void StartReplaceEmission(Color c, float peak, float duration) {
        if (_colorCo != null) StopCoroutine(_colorCo);
        _colorCo = StartCoroutine(ReplaceEmissionFor(c, peak, duration));
    }

    IEnumerator ReplaceEmissionFor(Color c, float peak, float duration) {
        if (!_body) yield break;
        _body.GetPropertyBlock(_mpb);
        Color baseEmit = _mpb.GetColor("_EmissionColor");
        float t = 0f;
        while (t < duration) {
            t += Time.deltaTime;
            // 약한 펄스
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            _mpb.SetColor("_EmissionColor", Color.Lerp(baseEmit, c * peak, 0.6f * pulse));
            _body.SetPropertyBlock(_mpb);
            yield return null;
        }
        _mpb.SetColor("_EmissionColor", baseEmit);
        _body.SetPropertyBlock(_mpb);
    }

    static IEnumerator ScaleForDuration(Transform t, float mul, float dur) {
        Vector3 from = t.localScale;
        Vector3 to = from * mul;
        float ramp = 0.2f;

        float a = 0f; while (a < ramp) { a += Time.deltaTime; t.localScale = Vector3.Lerp(from, to, a / ramp); yield return null; }
        yield return new WaitForSeconds(Mathf.Max(0, dur - 2 * ramp));
        float b = 0f; while (b < ramp) { b += Time.deltaTime; t.localScale = Vector3.Lerp(to, from, b / ramp); yield return null; }
        t.localScale = from;
    }

    void EnableObjFor(GameObject go, float dur) {
        if (!go) return;
        go.SetActive(true);
        StartCoroutine(DisableLater(go, dur));
    }
    IEnumerator DisableLater(GameObject go, float t) {
        yield return new WaitForSeconds(t);
        if (go) go.SetActive(false);
    }

    TrailRenderer GetTrail() {
        // 흔히 무기/손에 붙인 트레일 오브젝트 이름 예시
        var tr = GetComponentInChildren<TrailRenderer>(true);
        return tr;
    }
    IEnumerator TrailWidthFor(TrailRenderer tr, float width, float dur) {
        if (!tr) yield break;
        float baseW = tr.widthMultiplier;
        tr.widthMultiplier = width;
        tr.gameObject.SetActive(true);
        yield return new WaitForSeconds(dur);
        tr.widthMultiplier = baseW;
    }
    IEnumerator TrailColorFor(TrailRenderer tr, Color c, float dur) {
        if (!tr) yield break;
        Gradient baseG = tr.colorGradient;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        tr.colorGradient = g;
        tr.gameObject.SetActive(true);
        yield return new WaitForSeconds(dur);
        tr.colorGradient = baseG;
    }
}
