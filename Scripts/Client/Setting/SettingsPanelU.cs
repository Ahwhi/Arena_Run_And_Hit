using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SettingsPanelUI : MonoBehaviour {
    [Header("Refs")]
    public SettingsApplier applier;       // 비워도 자동 검색됨
    public Slider sliderBgm;
    public Slider sliderSfx;

    SettingsData _working;

    // 새로 추가: 패널 오픈 시점의 저장값 스냅샷과 저장 여부
    SettingsData _opened;      // 패널 열 당시의 '저장된' 값 스냅샷
    bool _savedThisSession;    // 이번 오픈-클로즈 사이에 저장 눌렀는지

    [Header("Key UI (optional)")]
    public KeySettingsUI keyUI;

    [Header("Video UI (optional)")]
    public VideoSettingsUI videoUI;

    [Header("Facility UI (optional)")]
    public FacilitySettingsUI facilityUI;

    public TextMeshProUGUI bgmtxt;
    public TextMeshProUGUI sfxtxt;

    void Awake() {
        // 에디터에서 열어볼 때 자동 와이어링
        TryResolveApplier(immediateOnly: true);
    }

    void OnEnable() {
        _savedThisSession = false;  // 새로 열 때 초기화
        StartCoroutine(CoEnsureApplierAndInit());
    }

    IEnumerator CoEnsureApplierAndInit() {
        // 한 프레임 대기: 다른 오브젝트(Applier)가 Awake/Start로 올라올 시간 확보
        if (!TryResolveApplier(immediateOnly: true))
            yield return null;

        // 그래도 못 찾았으면 적극적으로 재시도(최대 30프레임 정도)
        int guard = 30;
        while (applier == null && guard-- > 0) {
            if (TryResolveApplier(immediateOnly: false)) break;
            yield return null;
        }

        if (applier == null) {
            Debug.LogWarning("[SettingsPanelUI] SettingsApplier를 찾지 못했습니다. 씬에 하나 배치되어야 합니다.");
            yield break;
        }

        // 현재 저장된 값 불러와 스냅샷 저장
        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
        _opened = new SettingsData {
            bgm = cur.bgm,
            sfx = cur.sfx,
            keys = cur.keys,
            resolutionIndex = cur.resolutionIndex,
            windowMode = cur.windowMode,
            maxFps = cur.maxFps,
            languageIndex = cur.languageIndex,
            facilityHelpMessage = cur.facilityHelpMessage,
            showPlayerNameplate = cur.showPlayerNameplate,
            cameraViewMode = cur.cameraViewMode
        };

        bgmtxt.text = ((int)(_opened.bgm * 100)).ToString() + "%";
        sfxtxt.text = ((int)(_opened.sfx * 100)).ToString() + "%";

        // 작업용 버퍼는 스냅샷으로 초기화
        _working = new SettingsData { bgm = _opened.bgm, sfx = _opened.sfx };

        if (sliderBgm) {
            sliderBgm.SetValueWithoutNotify(_working.bgm);
            sliderBgm.onValueChanged.AddListener(OnBgmChanged);
        }
        if (sliderSfx) {
            sliderSfx.SetValueWithoutNotify(_working.sfx);
            sliderSfx.onValueChanged.AddListener(OnSfxChanged);
        }

        if (keyUI != null) keyUI.ApplyFrom(_opened);
        if (videoUI != null) videoUI.ApplyFrom(_opened); // 라벨만 반영(실시스템 변경 없음)
        if (facilityUI != null) facilityUI.ApplyFrom(_opened);
    }

    void OnDisable() {
        if (sliderBgm) sliderBgm.onValueChanged.RemoveListener(OnBgmChanged);
        if (sliderSfx) sliderSfx.onValueChanged.RemoveListener(OnSfxChanged);

        // 저장 안 하고 닫혔다면, 미리보기로 적용된 볼륨을 스냅샷으로 되돌림
        if (applier != null && !_savedThisSession && _opened != null) {
            applier.ApplyVolumes(_opened);
            applier.ApplyLanguage(_opened, previewOnly: true);
        }
    }

    bool TryResolveApplier(bool immediateOnly) {
        if (applier) return true;

        // 1) 싱글톤 먼저
        if (SettingsApplier.I) {
            applier = SettingsApplier.I;
            return true;
        }

        // 2) 즉시 탐색 (씬+DDOL 포함)
#if UNITY_2023_1_OR_NEWER
        applier = FindAnyObjectByType<SettingsApplier>(FindObjectsInactive.Include);
#else
        applier = FindAnyObjectByType<SettingsApplier>(true);
#endif
        if (applier) return true;

        // 3) 바로 찾는 것만 허용할지 여부
        if (immediateOnly) return false;

        return false;
    }

    void OnBgmChanged(float v) {
        _working.bgm = v;
        bgmtxt.text = ((int)(v * 100)).ToString() + "%";
        applier?.ApplyVolumes(_working); // 실시간 미리듣기
    }

    void OnSfxChanged(float v) {
        _working.sfx = v;
        sfxtxt.text = ((int)(v * 100)).ToString() + "%";
        applier?.ApplyVolumes(_working); // 실시간 미리듣기
    }

    public void OnButtonLeftBGM() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        _working.bgm -= 0.01f;
        if (_working.bgm < 0) {
            _working.bgm = 0;
        }
        bgmtxt.text = ((int)(_working.bgm * 100)).ToString() + "%";
        applier?.ApplyVolumes(_working); // 실시간 미리듣기
    }

    public void OnButtonRightBGM() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        _working.bgm += 0.01f;
        if (_working.bgm > 1) {
            _working.bgm = 1;
        }
        bgmtxt.text = ((int)(_working.bgm * 100)).ToString() + "%";
        applier?.ApplyVolumes(_working); // 실시간 미리듣기
    }

    public void OnButtonLeftSFX() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        _working.sfx -= 0.01f;
        if (_working.sfx < 0) {
            _working.sfx = 0;
        }
        sfxtxt.text = ((int)(_working.sfx * 100)).ToString() + "%";
        applier?.ApplyVolumes(_working); // 실시간 미리듣기
    }

    public void OnButtonRightSFX() {
        SoundManager.I?.Play2D(SfxId.MouseClick);
        _working.sfx += 0.01f;
        if (_working.sfx > 1) {
            _working.sfx = 1;
        }
        sfxtxt.text = ((int)(_working.sfx * 100)).ToString() + "%";
        applier?.ApplyVolumes(_working); // 실시간 미리듣기
    }

    // ==== 버튼 ====
    public void ClickSave() {
        if (!applier) { Debug.LogWarning("[SettingsPanelUI] applier 없음. 저장 실패"); return; }

        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
        cur.bgm = _working?.bgm ?? cur.bgm;
        cur.sfx = _working?.sfx ?? cur.sfx;

        if (keyUI != null) {
            if (cur.keys == null) cur.keys = new KeyBindings();
            var k = keyUI.GetWorking();
            cur.keys.attack = k.attack; cur.keys.dance1 = k.dance1; cur.keys.dance2 = k.dance2; cur.keys.dance3 = k.dance3; cur.keys.dance4 = k.dance4;
        }

        if (videoUI != null) {
            var (resIndex, mode, maxFps) = videoUI.GetWorking();
            cur.resolutionIndex = resIndex;
            cur.windowMode = mode;
            cur.maxFps = maxFps; // ▼ FPS 저장
        }

        if (facilityUI != null) {
            cur.languageIndex = facilityUI.GetWorkingLanguage();
            cur.facilityHelpMessage = facilityUI.GetWorking();
            cur.showPlayerNameplate = facilityUI.GetWorkingNameplate();
            cur.cameraViewMode = facilityUI.GetWorkingCameraView();
        }

        SettingsApplier.Current = cur;
        SettingsStorage.Save(cur);

        applier.ApplyVolumes(cur);
        applier.ApplyVideo(cur);     // 해상도/창모드 적용
        applier.ApplyFramerate(cur); // ▼ FPS 적용
        applier.ApplyLanguage(cur);

        // 저장한 순간의 값으로 스냅샷 갱신 + 저장플래그 ON (OnDisable에서 롤백 방지)
        _opened = new SettingsData {
            bgm = cur.bgm,
            sfx = cur.sfx,
            keys = cur.keys,
            resolutionIndex = cur.resolutionIndex,
            windowMode = cur.windowMode,
            maxFps = cur.maxFps,
            languageIndex = cur.languageIndex,
            facilityHelpMessage = cur.facilityHelpMessage,
            showPlayerNameplate = cur.showPlayerNameplate,
            cameraViewMode = cur.cameraViewMode
        };
        _savedThisSession = true;

        UIManager.ShowSuccess("설정을 저장했습니다.");
    }

    public void ClickCancel() {
        if (!applier) { Debug.LogWarning("[SettingsPanelUI] applier 없음. 취소 실패"); return; }
        if (_opened == null) return;

        // UI 롤백
        _working = new SettingsData { bgm = _opened.bgm, sfx = _opened.sfx };
        bgmtxt.text = ((int)(_opened.bgm * 100)).ToString() + "%";
        sfxtxt.text = ((int)(_opened.sfx * 100)).ToString() + "%";
        if (sliderBgm) sliderBgm.SetValueWithoutNotify(_working.bgm);
        if (sliderSfx) sliderSfx.SetValueWithoutNotify(_working.sfx);
        if (keyUI != null) keyUI.ApplyFrom(_opened);
        if (videoUI != null) videoUI.ApplyFrom(_opened);
        if (facilityUI != null) facilityUI.ApplyFrom(_opened);

        // **실제 오디오도 즉시 스냅샷으로 복구**
        applier.ApplyVolumes(_opened);
    }

    public void ClickReset() {
        if (!applier) { Debug.LogWarning("[SettingsPanelUI] applier 없음. 리셋 실패"); return; }

        // 파일/메모리 기본값으로 초기화하되, ‘저장’ 눌러야 실제 적용되도록 UI만 채움
        var defaults = SettingsData.CreateDefaults();
        _working = new SettingsData { bgm = defaults.bgm, sfx = defaults.sfx };
        if (sliderBgm) sliderBgm.SetValueWithoutNotify(_working.bgm);
        if (sliderSfx) sliderSfx.SetValueWithoutNotify(_working.sfx);
        if (keyUI) keyUI.ApplyFrom(defaults);
        if (videoUI) videoUI.ApplyFrom(defaults);
        if (facilityUI) facilityUI.ApplyFrom(defaults);

        UIManager.ShowSuccess("기본값으로 초기화했습니다. 저장 버튼을 눌러주세요.");
    }
}
