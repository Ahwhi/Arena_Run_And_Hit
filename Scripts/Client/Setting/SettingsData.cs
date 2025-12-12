using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WindowMode { Fullscreen, Borderless, Windowed }
public enum CameraViewMode {
    Default = 0,
    TopView = 1
}

[Serializable]
public class SettingsData {
    public float bgm = 0.77f;
    public float sfx = 0.95f;
    public KeyBindings keys = new KeyBindings();

    public int resolutionIndex = -1;                 // 사용 가능한 해상도 목록 인덱스
    public WindowMode windowMode = WindowMode.Borderless; // 기본값: 테두리없는 전체화면
    public int maxFps = 144; // 60/120/144/180/240 중 하나
    public int languageIndex = 0; //0=한국 1=영어
    public bool facilityHelpMessage = true;
    public bool showPlayerNameplate = true;
    public CameraViewMode cameraViewMode = CameraViewMode.Default;

    public static SettingsData CreateDefaults() {

        // 기본 해상도 인덱스를 1920x1080으로 찾기
        int defaultResIndex = -1;
        try {
            var list = SettingsApplier.DistinctResolutions;
            for (int i = 0; i < list.Length; i++) {
                if (list[i].width == 1920 && list[i].height == 1080) {
                    defaultResIndex = i;
                    break;
                }
            }
        } catch {
            // 에디터/특수 환경에서 Screen.resolutions 못 쓰는 경우 대비
            defaultResIndex = -1;
        }

        return new SettingsData {
            bgm = 0.77f,
            sfx = 0.95f,
            keys = new KeyBindings {
                attack = KeyCode.Space,
                dance1 = KeyCode.Alpha1,
                dance2 = KeyCode.Alpha2,
                dance3 = KeyCode.Alpha3,
                dance4 = KeyCode.Alpha4,
            },

            // ★ 여기서 1920x1080 인덱스를 기본값으로 사용
            resolutionIndex = defaultResIndex,  // 없으면 -1 → 현재 해상도 유지

            windowMode = WindowMode.Borderless, // 전체화면(테두리 없음)
            maxFps = 144,
            languageIndex = 0,
            facilityHelpMessage = true,
            showPlayerNameplate = true,
            cameraViewMode = CameraViewMode.Default
        };
    }
}

