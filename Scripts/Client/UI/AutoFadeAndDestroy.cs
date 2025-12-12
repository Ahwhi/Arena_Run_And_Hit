using UnityEngine;
using UnityEngine.UI;

public class AutoFadeAndDestroyAll : MonoBehaviour {
    [Header("Timing (seconds)")]
    public float lifeTime = 5f;        // 총 생존 시간
    public float fadeStartTime = 2f;   // 이 시점부터 페이드 시작

    [Header("Options")]
    public bool useUnscaledTime = false;          // 타임스케일 무시 여부
    public bool addCanvasGroupIfMissing = true;   // 없으면 자동 추가

    CanvasGroup _group;

    void Awake() {
        _group = GetComponent<CanvasGroup>();
        if (_group == null && addCanvasGroupIfMissing)
            _group = gameObject.AddComponent<CanvasGroup>();

        if (_group != null)
            _group.alpha = 1f; // 시작값 보장
    }

    void OnEnable() {
        StartCoroutine(CoLife());
    }

    System.Collections.IEnumerator CoLife() {
        float t = 0f;
        float fadeDuration = Mathf.Max(0f, lifeTime - fadeStartTime);

        while (t < lifeTime) {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            if (_group != null && t >= fadeStartTime && fadeDuration > 0f) {
                float k = Mathf.Clamp01((t - fadeStartTime) / fadeDuration); // 0→1
                _group.alpha = 1f - k; // 1→0
            }

            yield return null;
        }

        if (_group != null) _group.alpha = 0f; // 확정 0
        Destroy(gameObject);
    }
}
