using UnityEngine;

public class TrailDuringWindow : StateMachineBehaviour {
    [Header("Path under Animator")]
    public string trailObjectPath =
        "mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/AttackTrail";

    [Header("Window [0..1]")]
    [Range(0f, 1f)] public float startN = 0.25f;
    [Range(0f, 1f)] public float endN = 0.52f;
    [Range(1f, 10f)] public float Multiplier = 3f;

    TrailRenderer GetTrail(Animator animator) {
        var tf = animator.transform.Find(trailObjectPath);
        if (!tf) return null;
        return tf.GetComponent<TrailRenderer>();
    }

    static bool InWindow(AnimatorStateInfo info, float a, float b) {
        float n = info.normalizedTime - Mathf.Floor(info.normalizedTime); // 0..1 래핑
        return n >= a && n <= b;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo s, int layerIndex) {
        var trail = GetTrail(animator);
        if (!trail) return;

        var go = trail.gameObject;
        if (!go.activeSelf) go.SetActive(true);

        trail.enabled = true;
        trail.emitting = false;  // 윈도우 전까지 OFF
        trail.Clear();
        trail.widthMultiplier = Multiplier;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo s, int layerIndex) {
        var trail = GetTrail(animator);
        if (!trail) return;

        bool on = InWindow(animator.GetCurrentAnimatorStateInfo(layerIndex), startN, endN);
        if (animator.IsInTransition(layerIndex)) {
            var next = animator.GetNextAnimatorStateInfo(layerIndex);
            on |= InWindow(next, startN, endN);
        }

        if (trail.emitting != on) {
            if (!on) trail.Clear();
            trail.emitting = on;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo s, int layerIndex) {
        var trail = GetTrail(animator);
        if (!trail) return;

        trail.emitting = false;
        trail.enabled = true; // 컴포넌트는 켜둠
        trail.Clear();
    }
}
