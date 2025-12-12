using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour {
    [Header("Map Area")]
    public RectTransform mapRect;            // 미니맵 패널
    public float worldExtent = 35f;          // 월드 ±X/Z -> 맵 가장자리

    [Header("Player Dots")]
    public Image dotPrefab;                  // 동그라미 이미지(UGUI Image)
    public float myDotScale = 1.2f;
    public float otherDotScale = 0.9f;
    public Color deadTint = new Color(1, 1, 1, 0.35f);
    public float fadeLerp = 12f;

    [Header("Supply Dots")]
    public Image supplyDotPrefab;            // ★ 노란 별/동그라미 하나 준비
    public Color supplyColor = new Color(1f, 0.95f, 0.4f, 1f);
    public float sparkleSpeed = 3.0f;        // 반짝 주기
    public float sparkleAmp = 0.15f;         // 스케일 가감
    public float supplyBaseScale = 1.1f;

    [Header("Supply Outline (sparkle)")]
    public Color supplyOutlineColor = Color.white;   // 외곽선 색
    public float outlineAlphaMin = 0.25f;            // 알파 최소
    public float outlineAlphaMax = 0.95f;            // 알파 최대
    public float outlineThickMin = 1.0f;             // 두께 최소(px)
    public float outlineThickMax = 3.0f;             // 두께 최대(px)

    // 팔레트 등 기존 내용…
    public Color[] Palette = {
        new Color(1.00f,0.20f,0.20f),
        new Color(0.20f,0.90f,0.30f),
        new Color(0.25f,0.55f,1.00f),
        new Color(0.65f,0.35f,0.95f),
        new Color(0.98f,0.85f,0.18f),
        new Color(1.00f,0.55f,0.10f),
        new Color(0.10f,0.90f,0.90f),
        new Color(1.00f,0.20f,0.70f),
        new Color(0.60f,1.00f,0.20f),
        new Color(0.90f,0.90f,0.90f),
    };

    // 기존: 플레이어 점 관리
    readonly Dictionary<int, Image> _dots = new();
    readonly Dictionary<int, float> _dotAlpha = new();

    bool _supplyHooked;   // 늦게 생긴 SupplyManager에 한 번만 연결하기 위한 플래그

    // ★ 추가: 보급품 점 관리
    class SupplyDot {
        public Image img;
        public float phase;
        public Outline outline;
    }
    readonly Dictionary<int, SupplyDot> _supplyDots = new();  // dropId -> dot

    void OnEnable() {
        TryHookSupplyEvents();  // 즉시 시도
    }

    void OnDisable() {
        if (SupplyManager.Instance != null) {
            SupplyManager.Instance.OnSupplySpawned -= OnSupplySpawned;
            SupplyManager.Instance.OnSupplyDespawned -= OnSupplyDespawned;
        }
        _supplyHooked = false;
    }

    // === 보급품 이벤트 핸들러 ===
    void OnSupplySpawned(int dropId, Vector3 worldPos) {
        if (!supplyDotPrefab || _supplyDots.ContainsKey(dropId)) return;
        var img = Instantiate(supplyDotPrefab, mapRect);
        img.color = supplyColor;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = WorldToMini(worldPos);
        rt.localScale = Vector3.one * supplyBaseScale;

        // ★ 외곽선 추가
        var outline = img.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(supplyOutlineColor.r, supplyOutlineColor.g, supplyOutlineColor.b, outlineAlphaMin);
        outline.effectDistance = new Vector2(outlineThickMin, outlineThickMin);
        outline.useGraphicAlpha = false; // 이미지 알파와 독립적으로 외곽선 알파 제어

        _supplyDots[dropId] = new SupplyDot {
            img = img,
            phase = Random.value * Mathf.PI * 2f,
            outline = outline
        };
    }

    void OnSupplyDespawned(int dropId) {
        if (_supplyDots.TryGetValue(dropId, out var d) && d.img) Destroy(d.img.gameObject);
        _supplyDots.Remove(dropId);
    }

    // === 좌표 변환 공통 함수 ===
    Vector2 WorldToMini(Vector3 world) {
        // worldExtent을 기준으로 -extent~+extent -> 미니맵 Rect 내부로 선형 매핑
        float nx = Mathf.Clamp(world.x / worldExtent, -1f, 1f);
        float nz = Mathf.Clamp(world.z / worldExtent, -1f, 1f);
        float rx = (nx * 0.5f + 0.5f) * mapRect.rect.width;
        float ry = (nz * 0.5f + 0.5f) * mapRect.rect.height;
        return new Vector2(rx - mapRect.rect.width * 0.5f, ry - mapRect.rect.height * 0.5f);
    }

    void LateUpdate() {
        if (!_supplyHooked) TryHookSupplyEvents();  // 매니저가 더 늦게 생겨도 결국 붙도록

        var aliveNow = PlayerManager.FindAllPlayer;  // 키 캐시

        foreach (var kv in aliveNow) {
            int id = kv.Key;
            var p = kv.Value;

            if (!_dots.TryGetValue(id, out var dot)) {
                dot = Instantiate(dotPrefab, mapRect);
                dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _dots[id] = dot;
                _dotAlpha[id] = 1f;
            }

            // 위치
            dot.rectTransform.anchoredPosition = WorldToMini(p.transform.position);

            // ★ 색상: 사람/봇 모두 ColorIndex → 팔레트
            int ci = (p.ColorIndex < 0) ? 9 : Mathf.Clamp(p.ColorIndex, 0, Palette.Length - 1);
            Color baseColor = Palette[ci];
            // (서버 규칙상 11번째부터 흰색이면 ci==9가 들어오므로 그대로 흰색)

            // 생존/사망 알파 보간
            float targetA = (p.HP > 0 ? 1f : deadTint.a);
            float a = Mathf.Lerp(_dotAlpha[id], targetA, Time.deltaTime * fadeLerp);
            _dotAlpha[id] = a;
            dot.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);

            // 스케일
            float scale = (p is MyPlayer) ? myDotScale : otherDotScale;
            dot.rectTransform.localScale = Vector3.one * scale;
        }

        // ----- (추가) 보급품 점 업데이트 + 반짝 ----- //
        float t = Time.unscaledTime; // UI는 보통 unscaled가 보기 좋음
        foreach (var kv in _supplyDots) {
            var sd = kv.Value;
            if (!sd.img) continue;

            // 위치 스케일 유지
            float k = 0.5f + 0.5f * Mathf.Sin(t * sparkleSpeed + sd.phase);

            // 외곽선 알파 펄스
            float a = Mathf.Lerp(outlineAlphaMin, outlineAlphaMax, k);
            var c = supplyOutlineColor; c.a = a;
            if (sd.outline) sd.outline.effectColor = c;

            // 외곽선 두께 펄스(선택)
            float thick = Mathf.Lerp(outlineThickMin, outlineThickMax, k);
            if (sd.outline) sd.outline.effectDistance = new Vector2(thick, thick);

            // 점 자체는 노란색 고정, 약간의 크기 펄스가 필요하면 아래 한 줄 유지
            //sd.img.rectTransform.localScale = Vector3.one * (supplyBaseScale * (1f + sparkleAmp * (k * 2f - 1f)));
        }
    }

    void TryHookSupplyEvents() {
        if (_supplyHooked) return;
        if (SupplyManager.Instance == null) return;

        SupplyManager.Instance.OnSupplySpawned += OnSupplySpawned;
        SupplyManager.Instance.OnSupplyDespawned += OnSupplyDespawned;
        _supplyHooked = true;

        // 이미 떠 있던 보급품들을 한 번에 찍어주기
        foreach (var (id, pos) in SupplyManager.Instance.EnumerateAliveDrops()) {
            OnSupplySpawned(id, pos);
        }
    }
}
