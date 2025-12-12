using System.IO;
using UnityEngine;

public static class SettingsStorage {
    private const string FileName = "settings.json";
    public static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

    // 이번 앱 실행이 "첫 실행(설정파일 없음)" 이었는지
    public static bool WasFirstRunThisSession { get; private set; }

    public static void Save(SettingsData data) {
        if (data == null) data = SettingsData.CreateDefaults(); // 가드
        var json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(Path, json);
#if UNITY_EDITOR
        // Debug.Log($"[Settings] Saved: {Path}\n{json}");
#endif
    }

    public static SettingsData LoadOrDefault() {
        WasFirstRunThisSession = false; // 기본값
        try {
            if (File.Exists(Path)) {
                var json = File.ReadAllText(Path);
                var data = JsonUtility.FromJson<SettingsData>(json);
                if (data != null) {
                    MigrateIfNeeded(data);    // ★ 오래된 저장 파일 보정
                    return data;
                }
            }
        } catch {
            // 손상/예외 시 기본값으로 복구
        }

        // ★ 여기까지 왔다는건 파일이 없거나/깨졌거나 → "첫 실행 취급"
        WasFirstRunThisSession = !File.Exists(Path);

        // ★ 파일이 없거나 파싱 실패 → 설치 기본값으로
        var defaults = SettingsData.CreateDefaults();
        Save(defaults); // 원하면 첫 로드시 바로 파일 생성
        return defaults;
    }

    // === 저장 구조가 바뀔 때 안전하게 값 채워넣는 곳 ===
    private static void MigrateIfNeeded(SettingsData data) {
        // v1 → v2: keys가 없던 시절 저장파일 보정
        if (data.keys == null) data.keys = SettingsData.CreateDefaults().keys;
        if (data.maxFps == 0) data.maxFps = 144; // 과거 저장본 보정

        // 앞으로 항목 늘어나면 여기에 조건 추가
        // ex) if (data.someNewField == 0) data.someNewField = 123;
        if (data.resolutionIndex == 0 && Screen.resolutions.Length == 0) { /* no-op 안전가드 */ }
        if (data.resolutionIndex == 0 && data.windowMode == 0) { /* 컴파일러용 더미 */ }

        if (data.resolutionIndex == 0 && data.windowMode == 0) { /* 유지 */ }
        if (data.resolutionIndex == -1) data.resolutionIndex = SettingsData.CreateDefaults().resolutionIndex;

        if (data.languageIndex < 0 || data.languageIndex > 1)
            data.languageIndex = 0;

        if ((int)data.cameraViewMode < 0 || (int)data.cameraViewMode > 1)
            data.cameraViewMode = CameraViewMode.Default;
    }
}
