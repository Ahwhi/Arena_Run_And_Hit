// KeyAction.cs
using UnityEngine;

public enum KeyAction {
    Attack = 0,
    Dance1 = 1,
    Dance2 = 2,
    Dance3 = 3,
    Dance4 = 4,
}

[System.Serializable]
public class KeyBindings {
    public KeyCode attack = KeyCode.Space;
    public KeyCode dance1 = KeyCode.Alpha1;
    public KeyCode dance2 = KeyCode.Alpha2;
    public KeyCode dance3 = KeyCode.Alpha3;
    public KeyCode dance4 = KeyCode.Alpha4;

    public KeyCode Get(KeyAction a) {
        switch (a) {
            case KeyAction.Attack: return attack;
            case KeyAction.Dance1: return dance1;
            case KeyAction.Dance2: return dance2;
            case KeyAction.Dance3: return dance3;
            case KeyAction.Dance4: return dance4;
            default: return KeyCode.None;
        }
    }

    public void Set(KeyAction a, KeyCode code) {
        switch (a) {
            case KeyAction.Attack: attack = code; break;
            case KeyAction.Dance1: dance1 = code; break;
            case KeyAction.Dance2: dance2 = code; break;
            case KeyAction.Dance3: dance3 = code; break;
            case KeyAction.Dance4: dance4 = code; break;
        }
    }

    public void ResetDefaults() {
        attack = KeyCode.Space;
        dance1 = KeyCode.Alpha1;
        dance2 = KeyCode.Alpha2;
        dance3 = KeyCode.Alpha3;
        dance4 = KeyCode.Alpha4;
    }

    public bool TryFindActionByKey(KeyCode code, out KeyAction action) {
        if (attack == code) { action = KeyAction.Attack; return true; }
        if (dance1 == code) { action = KeyAction.Dance1; return true; }
        if (dance2 == code) { action = KeyAction.Dance2; return true; }
        if (dance3 == code) { action = KeyAction.Dance3; return true; }
        if (dance4 == code) { action = KeyAction.Dance4; return true; }
        action = default;
        return false;
    }
}
