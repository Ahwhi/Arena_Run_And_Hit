using UnityEngine;
using UnityEngine.UI;

public class HpBar : MonoBehaviour {
    [Header("Refs")]
    public RectTransform rect;
    public Slider slider;

    [Header("Color")]
    public Gradient colorByHp;     // 0=빨강, 0.5=노랑, 1=초록 등으로 에디터에서 세팅
    public float colorLerpSpeed = 12f; // 색 전환 부드러움

    [HideInInspector] public Player target;

    Image _fill;   // Slider Fill 이미지
    float _lastMax;

    void Awake() {
        if (!rect) rect = GetComponent<RectTransform>();
        if (!slider) slider = GetComponent<Slider>();

        // Slider의 Fill 이미지 참조(슬라이더 하위 구조: Fill Area/Fill)
        if (slider && slider.fillRect)
            _fill = slider.fillRect.GetComponent<Image>();
    }

    public void Bind(Player p) {
        target = p;
        slider.wholeNumbers = true;
        slider.minValue = 0;
        slider.maxValue = p.MaxHP;
        _lastMax = p.MaxHP;
        RefreshInstant();
    }

    public void RefreshInstant() {
        if (!target || !slider) return;

        // MaxHP 변동 대응
        if (!Mathf.Approximately(_lastMax, target.MaxHP)) {
            slider.maxValue = target.MaxHP;
            _lastMax = target.MaxHP;
        }

        slider.value = target.HP;

        // ---- 색상 업데이트 ----
        if (_fill) {
            float t = Mathf.InverseLerp(0f, (float)target.MaxHP, (float)target.HP);
            // 현재 색에서 목표 색으로 부드럽게
            Color targetColor = colorByHp.Evaluate(t);
            _fill.color = Color.Lerp(_fill.color, targetColor, Time.deltaTime * colorLerpSpeed);
        }
    }

    void Update() {
        // 매 프레임 값/색 갱신(원한다면 호출 빈도 줄여도 됨)
        RefreshInstant();
    }
}
