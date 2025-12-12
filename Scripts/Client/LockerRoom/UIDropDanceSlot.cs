using UnityEngine;
using UnityEngine.EventSystems;

public class UIDropDanceSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler {
    LockerRoomUI _owner;
    int _slotIndex;

    public void Bind(LockerRoomUI owner, int slotIndex) {
        _owner = owner;
        _slotIndex = slotIndex;
    }

    public void OnDrop(PointerEventData eventData) {
        if (_owner == null) return;

        var drag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<UIDragDanceItem>() : null;
        if (drag == null) return;

        // 수락 표시(중요: EndDrag에서 해제 처리되지 않도록)
        drag.MarkAccepted();

        _owner.OnDropDanceToSlot(_slotIndex, drag.Sku, drag.FromSlotIndex);

        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    public void OnPointerEnter(PointerEventData eventData) { /* highlight optional */ }
    public void OnPointerExit(PointerEventData eventData) { /* highlight off */ }
}
