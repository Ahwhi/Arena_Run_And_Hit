// 새 파일: KoreanImeWatcher.cs
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class KoreanImeWatcher : MonoBehaviour {
    [Header("Targets")]
    public TMP_InputField idField;
    public TMP_InputField pwField;

    [Header("UI")]
    public GameObject KoreanWarningPanel;

    // 방어: 과도한 토글 깜빡임 방지용
    bool lastOn;
    float nextCheckTime;

    void OnEnable() { ForceOff(); }
    void OnDisable() { ForceOff(); }
    void OnApplicationFocus(bool focus) { if (!focus) ForceOff(); }
    void OnApplicationPause(bool pause) { if (pause) ForceOff(); }

    void Update() {
        // 60Hz 이하에서 과도한 Win32 호출 방지
        if (Time.unscaledTime < nextCheckTime) return;
        nextCheckTime = Time.unscaledTime + 0.05f; // 20Hz 폴링

        var es = EventSystem.current;
        var selected = es ? es.currentSelectedGameObject : null;

        bool focused =
            IsFocused(idField, selected) ||
            IsFocused(pwField, selected);

        if (!focused) { SetPanel(false); return; }

        bool isKorean =
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            IsKoreanIme_Windows();
#else
            // 비윈도우: 조합 중 한글이 실제로 들어오고 있을 때만 경고
            HasHangul(Input.compositionString);
#endif
        SetPanel(isKorean);
    }

    bool IsFocused(TMP_InputField f, GameObject selected) {
        if (!f) return false;
        if (!f.interactable || !f.enabled || !f.gameObject.activeInHierarchy) return false;
        // 진짜로 이 필드가 선택된 경우에만 인정
        if (selected != f.gameObject) return false;
        return f.isFocused;
    }

    void SetPanel(bool on) {
        if (KoreanWarningPanel && on != lastOn) {
            KoreanWarningPanel.SetActive(on);
            lastOn = on;
        }
    }
    void ForceOff() { lastOn = false; if (KoreanWarningPanel) KoreanWarningPanel.SetActive(false); }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    // ===== Windows: 진짜 한글 모드인지 정확 검출 =====
    // 참고: IME는 Open(열림) 상태여도 영어일 수 있음 -> ConversionStatus에 NATIVE 플래그가 켜져야 '가/한 중 가(한글)' 상태
    const int LANG_KOREAN = 0x0412;
    const int IME_CMODE_NATIVE = 0x0001;     // 한글/Native 모드
    const int IME_CMODE_FULLSHAPE = 0x0008;  // 전각(참고용, 필수는 아님)

    [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern IntPtr GetKeyboardLayout(uint idThread);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();

    [DllImport("imm32.dll")] static extern IntPtr ImmGetContext(IntPtr hWnd);
    [DllImport("imm32.dll")] static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
    [DllImport("imm32.dll")] static extern bool ImmGetConversionStatus(IntPtr hIMC, out int conversion, out int sentence);

    static ushort LOWORD(IntPtr v) => (ushort)((ulong)v & 0xFFFF);

    bool IsKoreanIme_Windows() {
        try {
            // 에디터/GameView/플레이 모드에서도 잘 잡히도록 ActiveWindow 우선
            IntPtr hwnd = GetActiveWindow();
            if (hwnd == IntPtr.Zero) hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;

            // 현재 스레드 레이아웃이 한국어인지
            IntPtr hkl = GetKeyboardLayout(GetCurrentThreadId());
            if (LOWORD(hkl) != LANG_KOREAN) return false;

            IntPtr hImc = ImmGetContext(hwnd);
            if (hImc == IntPtr.Zero) return false;

            bool ok = ImmGetConversionStatus(hImc, out int conv, out _);
            ImmReleaseContext(hwnd, hImc);
            if (!ok) return false;

            // 진짜 '한글' 모드: NATIVE 플래그가 켜져 있어야 함
            bool isNative = (conv & IME_CMODE_NATIVE) != 0;
            return isNative;
        } catch {
            return false;
        }
    }
#endif

    static bool HasHangul(string s) {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (char c in s) {
            if ((c >= '\uAC00' && c <= '\uD7A3') || // 음절
                (c >= '\u1100' && c <= '\u11FF') || // 자모
                (c >= '\u3130' && c <= '\u318F'))   // 호환 자모
                return true;
        }
        return false;
    }
}
