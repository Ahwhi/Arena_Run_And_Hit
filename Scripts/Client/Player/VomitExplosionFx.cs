using UnityEngine;

public class VomitExplosionFx : MonoBehaviour {
    [Tooltip("반경 3 기준으로 만든 프리팹이면 3으로 두고, 실제 반경에 맞춰 자동 스케일")]
    public float baseRadius = 3f;

    [Tooltip("터지는 스프레이 파티클들")]
    public ParticleSystem[] sprays;

    [Tooltip("바닥 퍼짐 링(선택)")]
    public ParticleSystem groundRing;

    public void Play(float radius) {
        float scale = Mathf.Max(0.01f, radius / Mathf.Max(0.001f, baseRadius));
        // XYXZ로 넓게만 스케일(높이는 유지)
        transform.localScale = new Vector3(scale, 1f, scale);

        // 파티클 재생
        float lifeMax = 0f;
        if (sprays != null) {
            foreach (var ps in sprays) {
                if (!ps) continue;
                var main = ps.main;

                // 반경 커지면 속도/수량도 살짝 스케일(취향껏)
                var vel = main.startSpeed;
                if (vel.mode == ParticleSystemCurveMode.Constant) {
                    main.startSpeed = vel.constant * Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(scale - 1f));
                }
                ps.Play();
                lifeMax = Mathf.Max(lifeMax,
                    main.duration + (main.startLifetime.mode == ParticleSystemCurveMode.Constant ? main.startLifetime.constant : 1f));
            }
        }
        if (groundRing) {
            var main = groundRing.main;
            groundRing.Play();
            lifeMax = Mathf.Max(lifeMax,
                main.duration + (main.startLifetime.mode == ParticleSystemCurveMode.Constant ? main.startLifetime.constant : 1f));
        }

        Destroy(gameObject, Mathf.Max(0.5f, lifeMax + 0.25f));
    }
}
