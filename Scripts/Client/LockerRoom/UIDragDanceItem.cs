using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIDragDanceItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    LockerRoomUI _owner;
    string _sku;
    int _fromSlotIndex = -1;   // -1: 목록에서, 0~3: 슬롯에서

    RectTransform _rootCanvasRT;
    Canvas _rootCanvas;

    GameObject _dragClone;     // 프리팹 전체 복제
    RectTransform _dragRT;
    bool _accepted;            // 어떤 슬롯이 수락했는지

    public void Init(LockerRoomUI owner, string sku, int fromSlotIndex = -1) {
        _owner = owner;
        _sku = sku;
        _fromSlotIndex = fromSlotIndex;
    }

    void FindRootCanvas() {
        if (_rootCanvas) return;
        _rootCanvas = GetComponentInParent<Canvas>();
        // 최상위 Canvas 찾기
        var cur = _rootCanvas;
        while (cur && cur.transform.parent && cur.transform.parent.GetComponentInParent<Canvas>())
            cur = cur.transform.parent.GetComponentInParent<Canvas>();
        _rootCanvas = cur ? cur : _rootCanvas;
        if (_rootCanvas) _rootCanvasRT = _rootCanvas.transform as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (string.IsNullOrEmpty(_sku)) return;
        FindRootCanvas();

        // ★ 프리팹(카드) 통째로 복제해서 최상위 캔버스에 붙임
        _dragClone = Instantiate(gameObject, _rootCanvasRT);
        _dragClone.name = "DragClone_" + _sku;

        // 여기 추가
        var cg = _dragClone.AddComponent<CanvasGroup>();
        cg.alpha = 0.1f;           // 드래그 중 반투명
        cg.blocksRaycasts = false;  // 드래그 타겟 블로킹 방지
        cg.interactable = false;    // 상호작용 비활성

        // 드래그 클론은 UI 블로킹 안 하도록 처리 (CanvasGroup 없이)
        foreach (var g in _dragClone.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;
        foreach (var t in _dragClone.GetComponentsInChildren<TMP_Text>(true))
            t.raycastTarget = false;
        foreach (var b in _dragClone.GetComponentsInChildren<Button>(true))
            b.interactable = false;
        foreach (var d in _dragClone.GetComponentsInChildren<UIDragDanceItem>(true))
            d.enabled = false;

        _dragRT = _dragClone.transform as RectTransform;
        FitToPreferredSize(_dragRT, gameObject.transform as RectTransform);

        _accepted = false;
        UpdateDragPosition(eventData);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnDrag(PointerEventData eventData) {
        UpdateDragPosition(eventData);
    }

    void UpdateDragPosition(PointerEventData e) {
        if (!_dragRT || !_rootCanvasRT) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootCanvasRT, e.position, e.pressEventCamera, out var lp);
        _dragRT.anchoredPosition = lp;
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (_dragClone) Destroy(_dragClone);

        // 어떤 슬롯도 수락하지 않았고, 슬롯에서 끌어온 거면 → 해제
        if (!_accepted && _fromSlotIndex >= 0)
            _owner?.OnUnequipDanceSlot(_fromSlotIndex);
    }

    // 슬롯이 수락했을 때 Drop 쪽에서 호출
    public void MarkAccepted() { _accepted = true; }

    public string Sku => _sku;
    public int FromSlotIndex => _fromSlotIndex;

    void FitToPreferredSize(RectTransform clone, RectTransform source) {
        if (!clone) return;
        clone.anchorMin = new Vector2(0.5f, 0.5f);
        clone.anchorMax = new Vector2(0.5f, 0.5f);
        clone.pivot = new Vector2(0.5f, 0.5f);
        clone.sizeDelta = source ? source.rect.size : new Vector2(120, 120);
        clone.localScale = Vector3.one;
    }
}
