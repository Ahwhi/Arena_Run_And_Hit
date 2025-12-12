using UnityEngine;
using UnityEngine.UI;

public class AutoFadeIn : MonoBehaviour {
    [Header("Timing (seconds)")]
    public float lifeTime = 5f;        // 총 동작 시간
    public float fadeStartTime = 0f;   // 이 시점부터 페이드 시작

    [Header("Options")]
    public bool useUnscaledTime = false;          // 타임스케일 무시 여부
    public bool addCanvasGroupIfMissing = true;   // 없으면 자동 추가
    public bool destroyOnFinish = false;          // 종료 후 오브젝트 삭제

    CanvasGroup _group;
    Coroutine _co;

    void Awake() {
        _group = GetComponent<CanvasGroup>();
        if (_group == null && addCanvasGroupIfMissing)
            _group = gameObject.AddComponent<CanvasGroup>();

        if (_group != null) {
            _group.alpha = 0f;            // 시작값: 투명
            _group.interactable = false;  // 처음엔 클릭 막기(선택)
            _group.blocksRaycasts = false;
        }
    }

    void OnEnable() {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoLife());
    }

    System.Collections.IEnumerator CoLife() {
        float t = 0f;
        float fadeDuration = Mathf.Max(0f, lifeTime - fadeStartTime);

        while (t < lifeTime) {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            if (_group != null) {
                if (t >= fadeStartTime && fadeDuration > 0f) {
                    float k = Mathf.Clamp01((t - fadeStartTime) / fadeDuration); // 0→1
                    _group.alpha = k; // 페이드인
                } else {
                    _group.alpha = 0f; // 시작 전엔 항상 0
                }

                // 거의 다 보이면 인터랙션 허용
                bool visible = _group.alpha >= 0.99f;
                _group.interactable = visible;
                _group.blocksRaycasts = visible;
            }

            yield return null;
        }

        if (_group != null) {
            _group.alpha = 1f;            // 확정 1
            _group.interactable = true;
            _group.blocksRaycasts = true;
        }

        if (destroyOnFinish)
            Destroy(gameObject);
    }
}
