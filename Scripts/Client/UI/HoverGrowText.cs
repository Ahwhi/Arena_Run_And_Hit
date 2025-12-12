using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public class HoverGrowText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Refs")]
    public TMP_Text targetText;             // 확대할 글자(TMP)
    public HoverTextGroup group;            // 형제들을 묶는 그룹

    [Header("Sizes")]
    public float normalSize = 28f;
    public float hoverSize = 34f;

    [Header("Smoothing")]
    public float lerpSpeed = 12f;           // 값이 클수록 더 빠르게

    float _current;
    float _target;

    void Reset() {
        targetText = GetComponentInChildren<TMP_Text>(true);
        group = GetComponentInParent<HoverTextGroup>(true);
    }

    void Awake() {
        if (targetText == null) targetText = GetComponentInChildren<TMP_Text>(true);
        if (group == null) group = GetComponentInParent<HoverTextGroup>(true);
        _current = targetText ? targetText.fontSize : normalSize;
        _target = normalSize;

        // 그룹에 자기 등록
        if (group) group.Register(this);
    }

    void OnDestroy() {
        if (group) group.Unregister(this);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (group) group.SetHovered(this);
        else SetTargetHover(true); // 그룹이 없어도 단독 동작 가능
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (group) group.ClearHovered(this);
        else SetTargetHover(false);
    }

    public void SetTargetHover(bool on) {
        _target = on ? hoverSize : normalSize;
    }

    void Update() {
        // 스무딩
        _current = Mathf.Lerp(_current, _target, Time.unscaledDeltaTime * lerpSpeed);
        if (targetText) targetText.fontSize = _current;
    }
}
