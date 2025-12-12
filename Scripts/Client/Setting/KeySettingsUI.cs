// KeySettingsUI.cs (Panel_Setting_Key 오브젝트에 붙이기)
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeySettingsUI : MonoBehaviour {
    [Header("Buttons & Labels")]
    public Button btnAttack;
    public TextMeshProUGUI lblAttack;

    public Button btnDance1;
    public TextMeshProUGUI lblDance1;

    public Button btnDance2;
    public TextMeshProUGUI lblDance2;

    public Button btnDance3;
    public TextMeshProUGUI lblDance3;

    public Button btnDance4;
    public TextMeshProUGUI lblDance4;

    [Header("Colors")]
    public Color idleColor = Color.white;   // 리스닝 아님
    public Color listenColor = Color.red;   // 리스닝 중(키 대기)

    KeyBindings _working;            // SettingsPanel의 working과 동기
    bool _listening = false;
    KeyAction _listeningAction;
    static readonly KeyCode[] _blockKeys = { KeyCode.None }; // 필요하면 금지키 추가

    void OnEnable() {
        // 현재 설정을 Working으로 복사
        var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
        if (cur.keys == null) cur.keys = new KeyBindings();
        _working = Clone(cur.keys);

        WireButtons();
        RefreshAllLabels();
        UpdateButtonColors();
    }

    void OnDisable() {
        _listening = false;
        SetBtnColor(btnAttack, idleColor);
        SetBtnColor(btnDance1, idleColor);
        SetBtnColor(btnDance2, idleColor);
        SetBtnColor(btnDance3, idleColor);
        SetBtnColor(btnDance4, idleColor);
    }

    KeyBindings Clone(KeyBindings src) {
        return new KeyBindings {
            attack = src.attack,
            dance1 = src.dance1,
            dance2 = src.dance2,
            dance3 = src.dance3,
            dance4 = src.dance4,
        };
    }

    void WireButtons() {
        btnAttack.onClick.RemoveAllListeners();
        btnDance1.onClick.RemoveAllListeners();
        btnDance2.onClick.RemoveAllListeners();
        btnDance3.onClick.RemoveAllListeners();
        btnDance4.onClick.RemoveAllListeners();

        btnAttack.onClick.AddListener(() => BeginListen(KeyAction.Attack));
        btnDance1.onClick.AddListener(() => BeginListen(KeyAction.Dance1));
        btnDance2.onClick.AddListener(() => BeginListen(KeyAction.Dance2));
        btnDance3.onClick.AddListener(() => BeginListen(KeyAction.Dance3));
        btnDance4.onClick.AddListener(() => BeginListen(KeyAction.Dance4));
    }

    void BeginListen(KeyAction action) {
        if (_listening) return;
        _listening = true;
        _listeningAction = action;

        //UIManager.ShowInfo($"{ActionName(action)}: 원하는 키를 눌러주세요.");
        UIManager.ShowInfoKey("TOAST9", ActionName(action));
        UpdateButtonColors();
    }

    void EndListen(bool apply) {
        _listening = false;
        UpdateButtonColors();
    }

    string ActionName(KeyAction a) {
        switch (a) {
            case KeyAction.Attack: return "공격";
            case KeyAction.Dance1: return "춤 1";
            case KeyAction.Dance2: return "춤 2";
            case KeyAction.Dance3: return "춤 3";
            case KeyAction.Dance4: return "춤 4";
        }
        return a.ToString();
    }

    void Update() {
        if (!_listening) return;

        // 0) 리스닝 중 마우스는 무시 (클릭/휠/사이드버튼 포함)
        for (int i = 0; i <= 6; i++) {
            if (Input.GetMouseButtonDown(i)) {
                // 아무 동작도 하지 않고 무시
                return;
            }
        }

        // Esc로 취소
        if (Input.GetKeyDown(KeyCode.Escape)) {
            EndListen(false);
            return;
        }

        // 아무 키 감지
        if (Input.anyKeyDown) {
            var code = DetectPressedKeyCode();
            if (code == KeyCode.None) return;
            if (Array.Exists(_blockKeys, k => k == code)) return;

            // 중복 방지: 다른 액션이 이미 쓰고 있으면 풀어주기
            if (_working.TryFindActionByKey(code, out var other) && other != _listeningAction) {
                _working.Set(other, KeyCode.None);
            }

            _working.Set(_listeningAction, code);
            RefreshAllLabels();
            EndListen(true);
        }
    }

    KeyCode DetectPressedKeyCode() {
        foreach (KeyCode k in Enum.GetValues(typeof(KeyCode))) {
            if (k == KeyCode.None) continue;
            if (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6) continue; // 마우스 차단
            if (Input.GetKeyDown(k)) return k;
        }
        return KeyCode.None;
    }

    string KeyToLabel(KeyCode code) {
        if (code == KeyCode.None) return "미지정";
        return code.ToString();
    }

    void RefreshAllLabels() {
        if (lblAttack) lblAttack.text = KeyToLabel(_working.attack);
        if (lblDance1) lblDance1.text = KeyToLabel(_working.dance1);
        if (lblDance2) lblDance2.text = KeyToLabel(_working.dance2);
        if (lblDance3) lblDance3.text = KeyToLabel(_working.dance3);
        if (lblDance4) lblDance4.text = KeyToLabel(_working.dance4);
    }

    void UpdateButtonColors() {
        // 전부 기본색
        SetBtnColor(btnAttack, idleColor);
        SetBtnColor(btnDance1, idleColor);
        SetBtnColor(btnDance2, idleColor);
        SetBtnColor(btnDance3, idleColor);
        SetBtnColor(btnDance4, idleColor);

        // 리스닝 중인 액션만 빨강
        if (_listening) {
            var b = GetButton(_listeningAction);
            SetBtnColor(b, listenColor);
        }
    }

    Button GetButton(KeyAction a) {
        switch (a) {
            case KeyAction.Attack: return btnAttack;
            case KeyAction.Dance1: return btnDance1;
            case KeyAction.Dance2: return btnDance2;
            case KeyAction.Dance3: return btnDance3;
            case KeyAction.Dance4: return btnDance4;
        }
        return null;
    }

    void SetBtnColor(Button b, Color c) {
        if (!b) return;
        // Button의 targetGraphic(Image) 또는 image 어느 쪽이든 색 변경
        if (b.targetGraphic) b.targetGraphic.color = c;
        else if (b.image) b.image.color = c;
    }

    // ==== SettingsPanelUI와 연동 (Save/Cancel에서 호출) ====
    public KeyBindings GetWorking() {
        if (_working == null) {
            var cur = SettingsApplier.Current ?? SettingsStorage.LoadOrDefault();
            _working = cur.keys ?? new KeyBindings();   // ★ 항상 유효 반환
            RefreshAllLabels();
            UpdateButtonColors();
        }
        return _working;
    }

    public void ApplyFrom(SettingsData data) {
        // 취소 시 롤백용
        if (data.keys == null) data.keys = new KeyBindings();
        _working = Clone(data.keys);
        RefreshAllLabels();
        UpdateButtonColors();
    }
}
