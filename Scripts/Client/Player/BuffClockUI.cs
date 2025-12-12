using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffClockUI : MonoBehaviour {
    [Header("Optional")]
    public Sprite circleSprite;     // (권장) 인스펙터에 원형 스프라이트 넣기

    [Header("Auto-created if null")]
    public Image ringBg;
    public Image ringFill;
    public TextMeshProUGUI label;

    static Sprite _cachedCircle;    // 절차 생성한 스프라이트 캐시

    // ====== 타이머 상태 ======
    float _remain, _total;
    bool _active;

    public void SetWorldVisible(bool on) {
        if (ringBg) ringBg.enabled = on;
        if (ringFill) ringFill.enabled = on;
        if (label) label.enabled = on;
    }

    void Awake() {
        var rt = GetComponent<RectTransform>();
        if (!rt) rt = gameObject.AddComponent<RectTransform>();

        // ★ 스프라이트 확보: 인스펙터 > 캐시 > 절차생성
        var circle = circleSprite ? circleSprite : (_cachedCircle ??= CreateProceduralCircle(64));

        if (!ringBg) {
            var bg = new GameObject("RingBg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(transform, false);
            ringBg = bg.GetComponent<Image>();
            ringBg.raycastTarget = false;
            ringBg.color = new Color(0, 0, 0, 0.35f);
        }
        ringBg.sprite = circle;
        ringBg.type = Image.Type.Simple;
        ringBg.preserveAspect = true;

        if (!ringFill) {
            var fill = new GameObject("RingFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(transform, false);
            ringFill = fill.GetComponent<Image>();
            ringFill.raycastTarget = false;
        }
        ringFill.sprite = circle;
        ringFill.type = Image.Type.Filled;
        ringFill.fillMethod = Image.FillMethod.Radial360;
        ringFill.fillOrigin = (int)Image.Origin360.Top;
        ringFill.fillClockwise = false;
        ringFill.preserveAspect = true;
        ringFill.color = new Color(1f, 0.9f, 0.3f, 1f);

        if (!label) {
            var t = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            t.transform.SetParent(transform, false);
            label = t.GetComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 16f;
            label.color = Color.white;
            label.text = "10";
        }

        var size = new Vector2(32, 32);
        rt.sizeDelta = size;
        (ringBg.transform as RectTransform).sizeDelta = size;
        (ringFill.transform as RectTransform).sizeDelta = size;
        (label.transform as RectTransform).sizeDelta = size;

        Hide();
    }

    public void Show(float seconds, Color color) {
        _total = Mathf.Max(0.01f, seconds);
        _remain = _total;
        ringFill.color = color;
        ringFill.fillAmount = 1f;
        label.text = Mathf.CeilToInt(_remain).ToString();
        _active = true;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // 다시 켤 때는 항상 보이게
        SetWorldVisible(true);
    }

    public void Hide() {
        _active = false;
        SetWorldVisible(false);
        gameObject.SetActive(false);
    }

    void Update() {
        if (!_active) return;
        _remain = Mathf.Max(0f, _remain - Time.deltaTime);
        ringFill.fillAmount = Mathf.InverseLerp(0f, _total, _remain); // 1→0
        label.text = Mathf.CeilToInt(_remain).ToString();
        if (_remain <= 0f) Hide();
    }

    // ====== 절차적 원형 스프라이트 생성 ======
    static Sprite CreateProceduralCircle(int size) {
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false) {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        float cx = (size - 1) * 0.5f, cy = (size - 1) * 0.5f;
        float r = (size - 2) * 0.5f, r2 = r * r;

        var px = new Color32[size * size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dx = x - cx, dy = y - cy;
                bool inside = (dx * dx + dy * dy) <= r2;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(inside ? 255 : 0));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
