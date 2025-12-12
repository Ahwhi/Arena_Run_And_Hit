using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RowSettingItem : MonoBehaviour {
    [Header("Refs")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI currentText;
    public Button btnLeft;
    public Button btnRight;

    string[] _options;
    int _index;
    Action<int> _onChanged;

    public void Setup(string displayName, string[] options, Func<int> getIndex, Action<int> onChanged) {
        nameText.text = displayName;
        _options = options;
        _onChanged = onChanged;
        _index = Mathf.Clamp(getIndex?.Invoke() ?? 0, 0, Mathf.Max(0, _options.Length - 1));
        RefreshLabel();

        btnLeft.onClick.RemoveAllListeners();
        btnRight.onClick.RemoveAllListeners();
        btnLeft.onClick.AddListener(() => { Shift(-1); });
        btnRight.onClick.AddListener(() => { Shift(+1); });
    }

    void Shift(int delta) {
        if (_options == null || _options.Length == 0) return;
        _index = (_index + delta) % _options.Length;
        if (_index < 0) _index += _options.Length;
        RefreshLabel();
        _onChanged?.Invoke(_index);
        SoundManager.I?.Play2D(SfxId.MouseClick);
    }

    void RefreshLabel() {
        if (currentText && _options != null && _index >= 0 && _index < _options.Length)
            currentText.text = _options[_index];
    }

    // 외부에서 값만 바뀌었을 때 갱신용
    public void SetIndex(int idx) {
        _index = Mathf.Clamp(idx, 0, Mathf.Max(0, (_options?.Length ?? 1) - 1));
        RefreshLabel();
    }
}
