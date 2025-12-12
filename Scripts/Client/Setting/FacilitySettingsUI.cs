using UnityEngine;

public class FacilitySettingsUI : MonoBehaviour {
    [Header("Build Targets")]
    public Transform content;
    public GameObject rowSettingPrefab;

    RowSettingItem _rowLanguage;
    RowSettingItem _rowHelpMessage;
    RowSettingItem _rowNameplate;
    RowSettingItem _rowCameraView;

    int _workLanguageIndex = 0; // 0=한국어, 1=English
    bool _workHelpOn = true;
    bool _workNameplateOn = true;
    int _workCameraView = 0;   // 0=기본, 1=탑뷰


    public bool GetWorking() => _workHelpOn;
    public int GetWorkingLanguage() => _workLanguageIndex;
    public bool GetWorkingNameplate() => _workNameplateOn;
    public CameraViewMode GetWorkingCameraView() => (CameraViewMode)_workCameraView;

    /// <summary>
    /// SettingsData에서 값 받아서 UI 갱신
    /// </summary>
    public void ApplyFrom(SettingsData data) {
        _workLanguageIndex = (data != null) ? Mathf.Clamp(data.languageIndex, 0, 1) : 0;
        if (_rowLanguage)
            _rowLanguage.SetIndex(_workLanguageIndex);

        _workHelpOn = (data != null) ? data.facilityHelpMessage : true;
        if (_rowHelpMessage)
            _rowHelpMessage.SetIndex(_workHelpOn ? 0 : 1);

        _workNameplateOn = (data != null) ? data.showPlayerNameplate : true;
        if (_rowNameplate)
            _rowNameplate.SetIndex(_workNameplateOn ? 0 : 1);

        // ★ 카메라 시점
        _workCameraView = (data != null)
            ? Mathf.Clamp((int)data.cameraViewMode, 0, 1)
            : 0;
        if (_rowCameraView)
            _rowCameraView.SetIndex(_workCameraView);
    }

    void OnEnable() {
        if (content && content.childCount == 0)
            BuildRowsOnce();

        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
        ApplyFrom(cur);
    }

    void BuildRowsOnce() {
        if (!content || !rowSettingPrefab) {
            Debug.LogWarning("[FacilitySettingsUI] content 또는 rowSettingPrefab 미할당");
            return;
        }

        // 1) ★ 언어 설정 Row (도움메세지 위)
        _rowLanguage = Instantiate(rowSettingPrefab, content)
            .GetComponent<RowSettingItem>();

        _rowLanguage.Setup(
            LanguageSwitcher.L("UI/Set12"),
            new[] { "한국어", "English" },
            getIndex: () => _workLanguageIndex,
            onChanged: idx => {
                _workLanguageIndex = Mathf.Clamp(idx, 0, 1);

                // 미리보기 적용(저장 전)
                SettingsApplier.I?.ApplyLanguage(
                    new SettingsData { languageIndex = _workLanguageIndex },
                    previewOnly: true
                );
            }
        );

        _rowHelpMessage = Instantiate(rowSettingPrefab, content)
            .GetComponent<RowSettingItem>();

        _rowHelpMessage.Setup(
            LanguageSwitcher.L("UI/Set13"),
            new[] { LanguageSwitcher.L("UI/Set14"), LanguageSwitcher.L("UI/Set15") },
            getIndex: () => _workHelpOn ? 0 : 1,
            onChanged: idx => _workHelpOn = (idx == 0)
        );

        // 2) ★ 플레이어 이름표 ON/OFF Row
        _rowNameplate = Instantiate(rowSettingPrefab, content)
            .GetComponent<RowSettingItem>();

        _rowNameplate.Setup(
            LanguageSwitcher.L("UI/nameplay"),
            new[] { LanguageSwitcher.L("UI/Set14"), LanguageSwitcher.L("UI/Set15") }, // 같은 “켜기/끄기” 텍스트 재사용
            getIndex: () => _workNameplateOn ? 0 : 1,
            onChanged: idx => _workNameplateOn = (idx == 0)
        );


        // 4) ★ 게임 카메라 시점
        _rowCameraView = Instantiate(rowSettingPrefab, content)
            .GetComponent<RowSettingItem>();

        _rowCameraView.Setup(
            LanguageSwitcher.L("UI/CameraView"), // 예: "게임 카메라 시점"
            new[] {
                LanguageSwitcher.L("UI/CameraDefault"), // 예: "기본"
                LanguageSwitcher.L("UI/CameraTop")      // 예: "탑뷰"
            },
            getIndex: () => _workCameraView,
            onChanged: idx => {
                _workCameraView = Mathf.Clamp(idx, 0, 1);
                CameraManager.Instance?.ApplyViewMode((CameraViewMode)_workCameraView);
            }
        );



    }
}
