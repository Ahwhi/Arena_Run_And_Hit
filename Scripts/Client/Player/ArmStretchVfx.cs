using System.Collections;
using UnityEngine;

/// <summary>
/// Mixamo(휴머노이드) 기준: 팔 뼈의 로컬 X축이 뼈 진행 방향.
/// RangeUp 동안 오른팔(상완/하완)을 x축으로 늘린다.
/// </summary>
public class ArmStretchVfx : MonoBehaviour {
    [Range(1f, 2.5f)] public float stretchFactor = LevelData.ARM_STRETCH_LENGTH; // 얼마나 늘릴지
    public bool alsoStretchHand = false;                   // 손까지 늘릴지 옵션

    Transform upper;   // UpperArm
    Transform lower;   // LowerArm
    Transform hand;    // Hand (옵션)

    Vector3 upperBase;
    Vector3 lowerBase;
    Vector3 handBase;

    Coroutine co;
    float remain;      // 남은 시간(갱신 갱신 시 늘려주기)

    void Awake() {
        // 1) 휴머노이드 우선
        var anim = GetComponentInChildren<Animator>();
        if (anim && anim.isHuman) {
            upper = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            lower = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        }

        // 2) 휴머노이드가 아니거나 못 찾은 경우: 경로 → 경로에서 mixamorig: 제거 순으로 시도
        if (!upper) upper = TryFindMixamoPath(
            "mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm"
        );
        if (!lower) lower = TryFindMixamoPath(
            "mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm"
        );
        if (!hand) hand = TryFindMixamoPath(
            "mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand"
        );

        if (upper) upperBase = upper.localScale;
        if (lower) lowerBase = lower.localScale;
        if (hand) handBase = hand.localScale;
    }

    Transform TryFindMixamoPath(string path) {
        var t = transform.Find(path);
        if (t) return t;

        // 접두사 제거 버전으로 재시도
        var alt = path.Replace("mixamorig:", string.Empty);
        return transform.Find(alt);
    }

    public void Play(float seconds, float factorOverride = -1f) {
        if (factorOverride > 0f) stretchFactor = factorOverride;
        remain = Mathf.Max(remain, seconds); // 이미 재생 중이면 남은 시간을 연장
        if (co != null) return;
        co = StartCoroutine(CoStretch());
    }

    IEnumerator CoStretch() {
        // 켜기
        ApplyScale(stretchFactor);

        // 유지
        while (remain > 0f) {
            remain -= Time.deltaTime;
            yield return null;
        }

        // 원복
        Restore();
        co = null;
    }

    void ApplyScale(float f) {
        // Mixamo는 로컬 X축이 뼈 진행 방향 → x만 키워주면 길어짐
        if (upper) upper.localScale = new Vector3(upperBase.x * f, upperBase.y, upperBase.z);
        if (lower) lower.localScale = new Vector3(lowerBase.x * f, lowerBase.y, lowerBase.z);
        if (alsoStretchHand && hand)
            hand.localScale = new Vector3(handBase.x * f, handBase.y, handBase.z);
    }

    void Restore() {
        if (upper) upper.localScale = upperBase;
        if (lower) lower.localScale = lowerBase;
        if (hand) hand.localScale = handBase;
    }

    void OnDisable() { if (co != null) { StopCoroutine(co); co = null; Restore(); } }
}
