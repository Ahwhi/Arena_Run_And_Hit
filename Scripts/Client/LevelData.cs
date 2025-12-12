using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData : MonoBehaviour {
    // 사정거리 증가 보급품
    public static readonly float ARM_STRETCH_LENGTH = 4.00f;
    public static readonly float ATTACK_COOLDOWN = 0.4f;

    public static readonly float MAP_MIN_X = -25f;
    public static readonly float MAP_MAX_X = 25f;
    public static readonly float MAP_MIN_Z = -25f;
    public static readonly float MAP_MAX_Z = 25f;

    public static readonly float KNOCKBACK_DISTANCE = 8.0f;
    public static readonly int KNOCKBACK_LOCK_MS = 1500;
}
