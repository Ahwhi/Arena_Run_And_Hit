using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InputFieldGlow : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler {
    public TMP_InputField input; // 자동 할당됨
    public Image glow;           // 하위 Glow Image 하나만

    [Header("Alphas")]
    [Range(0, 1)] public float alphaIdle = 0f;
    [Range(0, 1)] public float alphaHover = 0.15f;
    [Range(0, 1)] public float alphaFocus = 0.55f;
    [Range(0, 1)] public float alphaError = 0.8f;

    [Header("Colors")]
    public Color glowColor = new Color(0.2f, 0.9f, 1f, 1f); // 기본 네온
    public Color errorColor = new Color(1f, 0.3f, 0.3f, 1f); // 에러 시

    [Header("Anim")]
    [Range(1f, 30f)] public float fadeSpeed = 12f;
    public bool pulseOnFocus = true;
    [Range(0f, 0.4f)] public float pulseAmp = 0.08f;
    [Range(0.2f, 6f)] public float pulseHz = 1.6f;

    bool _hover, _focus, _error;
    Coroutine _fadeCo, _pulseCo;

    void Reset() {
        input = GetComponent<TMP_InputField>();
        if (transform.Find("Glow")) glow = transform.Find("Glow").GetComponent<Image>();
    }

    void Awake() {
        if (!input) input = GetComponent<TMP_InputField>();
        if (!glow && transform.Find("Glow")) glow = transform.Find("Glow").GetComponent<Image>();
        if (glow) {
            var c = glowColor; c.a = alphaIdle; glow.color = c;
            glow.raycastTarget = false;
        }
        // TMP 내부 이벤트에도 묶어줌 (선택/해제)
        input.onSelect.AddListener(_ => OnSelectInternal());
        input.onDeselect.AddListener(_ => OnDeselectInternal());
    }

    public void OnPointerEnter(PointerEventData e) { _hover = true; Refresh(); }
    public void OnPointerExit(PointerEventData e) { _hover = false; Refresh(); }
    public void OnSelect(BaseEventData e) { OnSelectInternal(); }
    public void OnDeselect(BaseEventData e) { OnDeselectInternal(); }

    void OnSelectInternal() {
        _focus = true;
        Refresh();
        if (pulseOnFocus && !_error) StartPulse();
    }
    void OnDeselectInternal() {
        _focus = false;
        Refresh();
        StopPulse();
    }

    public void SetError(bool on) {
        _error = on;
        Refresh();
        if (_error) StopPulse();
        else if (_focus && pulseOnFocus) StartPulse();
    }

    void Refresh() {
        if (!glow) return;

        // 목표 색/알파 결정
        Color baseCol = _error ? errorColor : glowColor;
        float targetA = alphaIdle;
        if (_error) targetA = alphaError;
        else if (_focus) targetA = alphaFocus;
        else if (_hover) targetA = alphaHover;

        // 페이드 코루틴
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeTo(baseCol, targetA));
    }

    IEnumerator FadeTo(Color targetRGB, float targetA) {
        Color start = glow.color;
        Color end = targetRGB; end.a = targetA;
        float t = 0f;
        while (t < 1f) {
            t += Time.unscaledDeltaTime * fadeSpeed;
            glow.color = Color.Lerp(start, end, t);
            yield return null;
        }
        glow.color = end;
        _fadeCo = null;
    }

    void StartPulse() {
        if (_pulseCo != null) StopCoroutine(_pulseCo);
        _pulseCo = StartCoroutine(CoPulse());
    }
    void StopPulse() {
        if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }
    }
    IEnumerator CoPulse() {
        // 포커스 기준 알파를 중심으로 살짝 들썩
        float baseA = alphaFocus;
        float t = 0f;
        while (_focus && !_error) {
            t += Time.unscaledDeltaTime * pulseHz;
            float a = baseA + Mathf.Sin(t * Mathf.PI * 2f) * pulseAmp;
            var c = glow.color; c.a = Mathf.Clamp01(a);
            glow.color = c;
            yield return null;
        }
    }
}
