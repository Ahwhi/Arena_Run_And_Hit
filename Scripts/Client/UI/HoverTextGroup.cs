using System.Collections.Generic;
using UnityEngine;

public class HoverTextGroup : MonoBehaviour {
    private readonly List<HoverGrowText> _items = new();
    private HoverGrowText _current;

    // 자식들이 자기 자신을 등록/해제
    public void Register(HoverGrowText item) {
        if (!_items.Contains(item)) _items.Add(item);
    }
    public void Unregister(HoverGrowText item) {
        if (_items.Contains(item)) _items.Remove(item);
        if (_current == item) _current = null;
    }

    // 어떤 항목이 호버되었다
    public void SetHovered(HoverGrowText hovered) {
        _current = hovered;
        foreach (var it in _items) {
            it.SetTargetHover(it == hovered);
        }
    }

    // 호버 빠졌을 때(그룹 전체 원복)
    public void ClearHovered(HoverGrowText who) {
        if (_current == who) _current = null;
        foreach (var it in _items) {
            it.SetTargetHover(false);
        }
    }
}
