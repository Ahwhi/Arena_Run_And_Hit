using UnityEngine;

public class UISpinner : MonoBehaviour {
    [SerializeField] float speed = 180f;
    [SerializeField] bool playOnEnable = true;

    bool _isPlaying;

    void OnEnable() {
        if (playOnEnable)
            _isPlaying = true;
    }

    void Update() {
        if (!_isPlaying) return;
        transform.Rotate(0f, 0f, -speed * Time.unscaledDeltaTime);
    }

    // 외부에서 호출
    public void Play() {
        _isPlaying = true;
        gameObject.SetActive(true);   // 숨겨져 있으면 보이게
    }

    public void Stop() {
        _isPlaying = false;
        // 필요하면 여기서 안보이게
        // gameObject.SetActive(false);
    }

    // 외부에서 속도 바꾸고 싶을 때
    public void SetSpeed(float newSpeed) {
        speed = newSpeed;
    }
}
