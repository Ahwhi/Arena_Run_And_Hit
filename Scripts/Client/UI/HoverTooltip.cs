using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Tooltip UI")]
    public RectTransform tooltip;          // Panel_Tooltip
    public TextMeshProUGUI tooltipText;    // Text_Tooltip

    [Header("Localization Key")]
    // 예) "Tooltip/01", "UI/Match_FindTip" 등
    public string messageKey = "Tooltip/01";

    [Header("Time & Pos")]
    public float showDuration = 1.5f;            // 몇 초 동안 보일지
    public Vector2 offset = new Vector2(0f, 40f); // 버튼 위로 살짝

    Coroutine _hideCo;

    Canvas _canvas;
    RectTransform _canvasRect;

    void Awake() {
        if (tooltip != null) {
            _canvas = tooltip.GetComponentInParent<Canvas>();
            if (_canvas != null)
                _canvasRect = _canvas.GetComponent<RectTransform>();

            tooltip.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (tooltip == null || tooltipText == null || _canvasRect == null)
            return;

        // 여기서 LanguageSwitcher.L 사용
        string localized = LanguageSwitcher.L(messageKey);
        if (string.IsNullOrEmpty(localized))
            localized = messageKey; // 키 못 찾으면 키 그대로 보여줌(디버그용)

        tooltipText.text = localized;

        // 마우스 위치 → 캔버스 로컬 좌표
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            eventData.position,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out localPoint
        );

        tooltip.anchoredPosition = localPoint + offset;
        tooltip.gameObject.SetActive(true);

        if (_hideCo != null)
            StopCoroutine(_hideCo);
        _hideCo = StartCoroutine(HideAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (tooltip == null)
            return;

        if (_hideCo != null)
            StopCoroutine(_hideCo);

        tooltip.gameObject.SetActive(false);
    }

    IEnumerator HideAfterDelay() {
        yield return new WaitForSeconds(showDuration);
        if (tooltip != null)
            tooltip.gameObject.SetActive(false);
    }
}
