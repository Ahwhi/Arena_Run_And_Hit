// VideoSettingsUI.cs
using System.Linq;
using Steamworks;
using UnityEngine;

public class VideoSettingsUI : MonoBehaviour {
    [Header("Build Targets")]
    public Transform content;
    public GameObject rowSettingPrefab;

    RowSettingItem _rowResolution;
    RowSettingItem _rowWindowMode;
    RowSettingItem _rowFps;            // ▼ 추가

    int _workResolutionIndex = -1;
    WindowMode _workWindowMode = WindowMode.Borderless;
    int _workMaxFps = 144;             // ▼ 추가

    public (int resIndex, WindowMode mode, int maxFps) GetWorking()
        => (_workResolutionIndex, _workWindowMode, _workMaxFps);

    public void ApplyFrom(SettingsData data) {
        _workResolutionIndex = Mathf.Clamp(data.resolutionIndex, -1, SettingsApplier.DistinctResolutions.Length - 1);
        _workWindowMode = data.windowMode;
        _workMaxFps = data.maxFps;

        if (_rowResolution) _rowResolution.SetIndex(Mathf.Max(0, _workResolutionIndex));
        if (_rowWindowMode) _rowWindowMode.SetIndex(_workWindowMode switch {
            WindowMode.Fullscreen => 0,
            WindowMode.Borderless => 1,
            WindowMode.Windowed => 2,
            _ => 1
        });
        if (_rowFps) _rowFps.SetIndex(FpsIndexOf(_workMaxFps));
    }

    void OnEnable() {
        if (content.childCount == 0) BuildRowsOnce();
        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
        ApplyFrom(cur);
    }

    void BuildRowsOnce() {
        var res = SettingsApplier.DistinctResolutions;
        var labels = res.Length > 0
            ? res.Select(SettingsApplier.Pretty).ToArray()
            : new[] { $"{Screen.currentResolution.width}×{Screen.currentResolution.height}" };

        // 해상도
        _rowResolution = Instantiate(rowSettingPrefab, content).GetComponent<RowSettingItem>();
        _rowResolution.Setup(
            LanguageSwitcher.L("UI/Set6"),
            labels,
            getIndex: () => {
                if (_workResolutionIndex < 0 && res.Length > 0)
                    _workResolutionIndex = FindClosestResolutionIndex(res, Screen.currentResolution);
                return Mathf.Clamp(_workResolutionIndex < 0 ? 0 : _workResolutionIndex, 0, labels.Length - 1);
            },
            onChanged: idx => _workResolutionIndex = idx
        );

        // 창모드
        _rowWindowMode = Instantiate(rowSettingPrefab, content).GetComponent<RowSettingItem>();
        _rowWindowMode.Setup(
            LanguageSwitcher.L("UI/Set7"),
            new[] { LanguageSwitcher.L("UI/Set9"), LanguageSwitcher.L("UI/Set10"), LanguageSwitcher.L("UI/Set11") },
            getIndex: () => _workWindowMode switch { WindowMode.Fullscreen => 0, WindowMode.Borderless => 1, WindowMode.Windowed => 2, _ => 1 },
            onChanged: idx => _workWindowMode = (idx == 0) ? WindowMode.Fullscreen : (idx == 1) ? WindowMode.Borderless : WindowMode.Windowed
        );

        // ▼ 최대 FPS
        _rowFps = Instantiate(rowSettingPrefab, content).GetComponent<RowSettingItem>();
        string[] fpsLabels = { "60", "120", "144", "180", "240" };
        _rowFps.Setup(
            LanguageSwitcher.L("UI/Set8"),
            fpsLabels,
            getIndex: () => FpsIndexOf(_workMaxFps),
            onChanged: idx => _workMaxFps = FpsValueAt(idx)
        );
    }

    int[] _fpsValues = { 60, 120, 144, 180, 240 };
    int FpsIndexOf(int v) {
        for (int i = 0; i < _fpsValues.Length; i++) if (_fpsValues[i] == v) return i;
        return 2; // 기본 144
    }
    int FpsValueAt(int idx) {
        idx = Mathf.Clamp(idx, 0, _fpsValues.Length - 1);
        return _fpsValues[idx];
    }

    int FindClosestResolutionIndex(Resolution[] list, Resolution cur) {
        int best = 0; long bestDiff = long.MaxValue;
        for (int i = 0; i < list.Length; i++) {
            long dw = Mathf.Abs(list[i].width - cur.width);
            long dh = Mathf.Abs(list[i].height - cur.height);
            long diff = (dw * 10000L) + dh;
            if (diff < bestDiff) { bestDiff = diff; best = i; }
        }
        return best;
    }
}
