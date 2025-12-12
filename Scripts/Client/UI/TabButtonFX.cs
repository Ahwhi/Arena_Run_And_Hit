using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // ★ 추가

[RequireComponent(typeof(Image))]
public class TabButtonFX : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {
    [Header("State")]
    public bool startActive = false;
    public bool useToggle = false;
    public Toggle toggle;
    public float lerpSpeed = 12f;

    [Header("Hover tune")]
    public float hoverMax = 1f;
    public float pressBoost = 0.2f;

    [Header("Text dim when inactive")]
    [Range(0f, 1f)] public float inactiveTextAlpha = 0.35f; // ★ 비활성일 때 목표 알파
    [Range(0f, 1f)] public float hoverTextBoost = 0.15f;    // ★ 호버 시 알파 보정

    Image _img;
    Material _mat;
    float _hover, _hoverTarget;
    float _active, _activeTarget;

    static readonly int PID_Active = Shader.PropertyToID("_ActiveAmt");
    static readonly int PID_Hover = Shader.PropertyToID("_HoverAmt");

    // ★ 버튼 하위 TMP 텍스트들
    readonly List<TextMeshProUGUI> _tmps = new();
    readonly List<Color> _tmpBaseColors = new();

    void Awake() {
        _img = GetComponent<Image>();
        _mat = _img.material ? Instantiate(_img.material) : new Material(Shader.Find("UI/TabButton"));
        _img.material = _mat;

        _active = _activeTarget = startActive ? 1f : 0f;

        if (useToggle && toggle) {
            toggle.isOn = startActive;
            toggle.onValueChanged.AddListener(OnToggle);
        }

        // ★ 모든 하위 TMP 텍스트 수집 + 원본 색 저장
        _tmps.Clear(); _tmpBaseColors.Clear();
        GetComponentsInChildren(true, _tmps);
        foreach (var t in _tmps)
            _tmpBaseColors.Add(t.color);
    }

    void OnEnable() {
        if (_mat) {
            _mat.SetFloat(PID_Active, _active);
            _mat.SetFloat(PID_Hover, _hover);
        }
        ApplyTextAlphaImmediate(); // ★ 초기 반영
    }

    void Update() {
        _hover = Mathf.Lerp(_hover, _hoverTarget, Time.unscaledDeltaTime * lerpSpeed);
        _active = Mathf.Lerp(_active, _activeTarget, Time.unscaledDeltaTime * lerpSpeed);

        if (_mat) {
            _mat.SetFloat(PID_Hover, _hover);
            _mat.SetFloat(PID_Active, _active);
        }

        // ★ 텍스트 알파 보간 적용
        ApplyTextAlphaLerped();
    }

    void ApplyTextAlphaImmediate() => ApplyTextAlpha(1f);
    void ApplyTextAlphaLerped() => ApplyTextAlpha(Time.unscaledDeltaTime * lerpSpeed);

    void ApplyTextAlpha(float lerpFactor) {
        // 활성도에 따라 알파: inactive→active 선형 보간, 호버로 살짝 증폭
        float baseScale = Mathf.Lerp(inactiveTextAlpha, 1f, _active);
        float boosted = Mathf.Clamp01(baseScale + _hover * hoverTextBoost);

        for (int i = 0; i < _tmps.Count; i++) {
            var t = _tmps[i];
            if (!t) continue;

            var baseCol = _tmpBaseColors[i];        // 원래 색/알파
            float targetA = baseCol.a * boosted;    // 원본 알파에 스케일 적용

            // 부드럽게 알파 보간
            var cur = t.color;
            cur.a = Mathf.Lerp(cur.a, targetA, lerpFactor);
            t.color = cur;
        }
    }

    public void SetActive(bool on) {
        _activeTarget = on ? 1f : 0f;
        if (useToggle && toggle && toggle.isOn != on)
            toggle.isOn = on;
    }

    void OnToggle(bool on) => SetActive(on);

    public void OnPointerEnter(PointerEventData eventData) => _hoverTarget = hoverMax;
    public void OnPointerExit(PointerEventData eventData) => _hoverTarget = 0f;
    public void OnPointerDown(PointerEventData eventData) => _hoverTarget = Mathf.Min(hoverMax + pressBoost, 1f);
    public void OnPointerUp(PointerEventData eventData) => _hoverTarget = hoverMax;
}
