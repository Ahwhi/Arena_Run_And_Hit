using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SettingsApplier : MonoBehaviour {
    public static SettingsApplier I { get; private set; }

    [Header("Mixer (Master)")]
    public AudioMixer masterMixer; // Master.mixer drag & drop

    [Header("Exposed Param Names")]
    public string bgmParam = "BGM_Volume";
    public string sfxParam = "SFX_Volume";

    // 캐시: 중복/가짜 리프레시 제거한 해상도 리스트
    static Resolution[] _distinctRes;

    public static SettingsData Current { get; set; }

    /// <summary>
    /// 편의성 - 도움 메세지가 켜져있는지 여부 (없으면 기본 true)
    /// </summary>
    public static bool IsHelpMessageEnabled =>
        Current == null ? true : Current.facilityHelpMessage;

    public static bool IsNameplateEnabled =>
        Current == null ? true : Current.showPlayerNameplate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() {
        Current = SettingsStorage.LoadOrDefault();
    }

    void Awake() {
        // 싱글톤 유지 (중복 생기면 자신 파괴)
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (Current == null) Current = SettingsStorage.LoadOrDefault();

        // 첫 프레임에 다른 컴포넌트들이 Mixer/Snapshot/AudioSource 세팅한 뒤 재적용
        StartCoroutine(CoApplyDelayed());

        StartCoroutine(LanguageSwitcher.SetLocale(Current.languageIndex == 1 ? "en" : "ko"));
        // 씬 바뀔 때마다 재적용(스냅샷 전환/초기화 방지)
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // 로드시 null이면 새 기본값으로 채워지게
        if (Current.keys == null) Current.keys = new KeyBindings();
    }

    void OnDestroy() {
        if (I == this) SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene a, UnityEngine.SceneManagement.Scene b) {
        // 씬 로딩 직후 한 프레임 밀어 적용 (다른 초기화가 끝난 뒤)
        StartCoroutine(CoApplyDelayed());
    }

    System.Collections.IEnumerator CoApplyDelayed() {
        yield return null;
        ApplyVolumes(Current, withDiagnostics: true);
        ApplyVideo(Current);
        ApplyFramerate(Current);
        ApplyLanguage(Current);
        if (SceneManager.GetActiveScene().name == "GameScene") {
            ApplyCamera(Current);
        }
    }

    // ---------------- 언어 적용 ----------------
    public void ApplyLanguage(SettingsData data, bool previewOnly = false) {
        if (data == null) return;

        int idx = Mathf.Clamp(data.languageIndex, 0, 1);
        string code = (idx == 1) ? "en" : "ko";

        StartCoroutine(LanguageSwitcher.SetLocale(code));

        if (!previewOnly) {
            Current.languageIndex = idx;
        }
    }

    public void ApplyVolumes(SettingsData data, bool withDiagnostics = false) {
        float ToDb(float v) => Mathf.Lerp(-80f, 0f, Mathf.Clamp01(v));

        if (!masterMixer) {
            if (withDiagnostics) Debug.LogWarning("[SettingsApplier] masterMixer is NULL. Mixer 참조를 할당하세요.");
            return;
        }

        // 파라미터 존재 확인 (이름 오타/노출 여부 체크)
        if (withDiagnostics) {
            bool hasBgm = masterMixer.GetFloat(bgmParam, out var _);
            bool hasSfx = masterMixer.GetFloat(sfxParam, out var _);
            if (!hasBgm) Debug.LogWarning($"[SettingsApplier] Mixer에 '{bgmParam}' 파라미터가 없습니다. (Expose ‘Volume’ 했는지 확인)");
            if (!hasSfx) Debug.LogWarning($"[SettingsApplier] Mixer에 '{sfxParam}' 파라미터가 없습니다.");
        }

        masterMixer.SetFloat(bgmParam, ToDb(data.bgm));
        masterMixer.SetFloat(sfxParam, ToDb(data.sfx));

        if (withDiagnostics) {
            masterMixer.GetFloat(bgmParam, out var dBbgm);
            masterMixer.GetFloat(sfxParam, out var dBsfx);
            //Debug.Log($"[SettingsApplier] Applied BGM={data.bgm:0.00}({dBbgm:0.0} dB), SFX={data.sfx:0.00}({dBsfx:0.0} dB)");
        }
    }

    public static Resolution[] DistinctResolutions {
        get {
            if (_distinctRes == null || _distinctRes.Length == 0) {
                const float EPS = 0.01f;
                bool IsAllowedAR(Resolution r) {
                    float ar = (float)r.width / r.height;
                    return Mathf.Abs(ar - 16f / 9f) < EPS ||
                           Mathf.Abs(ar - 16f / 10f) < EPS;
                }
                bool IsMinSize(Resolution r) => r.width >= 1366 && r.height >= 768;

                _distinctRes = Screen.resolutions
                    .Where(IsAllowedAR)
                    .Where(IsMinSize) // ★ 1366×768 미만 제거
                    .GroupBy(r => (r.width, r.height)) // 주사율 중복 제거(Hz는 UI에서 분리했으므로 크기만)
                    .Select(g => g.Last())
                    .OrderBy(r => r.width).ThenBy(r => r.height)
                    .ToArray();

                // 만약 필터 후 하나도 안 남으면 안전 폴백(현재 해상도가 기준 이상이면 포함)
                if (_distinctRes.Length == 0) {
                    var cur = Screen.currentResolution;
                    if (IsAllowedAR(cur) && IsMinSize(cur)) {
                        _distinctRes = new[] { cur };
                    }
                }
            }
            return _distinctRes;
        }
    }

    public static string Pretty(Resolution r) => $"{r.width}×{r.height}";

    public void ApplyVideo(SettingsData data, bool previewOnly = false) {
        var mode = data.windowMode switch {
            WindowMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
            WindowMode.Borderless => FullScreenMode.FullScreenWindow,
            WindowMode.Windowed => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow
        };

        if (data.resolutionIndex >= 0 && data.resolutionIndex < DistinctResolutions.Length) {
            var r = DistinctResolutions[data.resolutionIndex];
#if UNITY_2022_2_OR_NEWER
            Screen.SetResolution(r.width, r.height, mode, r.refreshRateRatio);
#else
            Screen.SetResolution(r.width, r.height, mode, r.refreshRate);
#endif
        } else {
            Screen.fullScreenMode = mode;
        }

        if (!previewOnly) {
            Current.windowMode = data.windowMode;
            Current.resolutionIndex = data.resolutionIndex;
        }
    }

    public void ApplyFramerate(SettingsData data) {
        QualitySettings.vSyncCount = 0;                 // 타겟 FPS를 정확히 따르게 (원하면 VSync 옵션 나중에 분리)
        Application.targetFrameRate = data.maxFps;      // 60/120/144/180/240
        Current.maxFps = data.maxFps;
    }

    public static CameraViewMode CameraMode =>
        Current == null ? CameraViewMode.Default : Current.cameraViewMode;

    public void ApplyCamera(SettingsData data, bool previewOnly = false) {
        if (data == null) return;
        var mode = data.cameraViewMode;

        CameraManager.Instance?.ApplyViewMode(mode);

        if (!previewOnly && Current != null)
            Current.cameraViewMode = mode;
    }




    public void Save(SettingsData data) {
        Current = data;
        SettingsStorage.Save(Current);
        ApplyVolumes(Current);
        ApplyLanguage(Current);
        ApplyCamera(Current);
    }

    public void ResetToDefaultsAndSave() {
        Current = SettingsData.CreateDefaults();   // 기본값으로
        SettingsStorage.Save(Current);             // 파일 저장
        ApplyVolumes(Current);                     // 즉시 적용(오디오)
        ApplyLanguage(Current);
        ApplyCamera(Current);
    }
}
