using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class PlayerUIManager : MonoBehaviour {
    public static PlayerUIManager Instance { get; private set; }

    [Header("Refs")]
    public Canvas canvas;
    public Camera worldCamera;
    public RectTransform container;
    public HpBar hpBarPrefab;
    public GameObject namePrefab;
    public GameObject toastPrefab;

    readonly Dictionary<Player, List<ToastItem>> _toastsByPlayer = new();
    const float LINE_HEIGHT = 22f;        // 줄 간격(px)
    const int MAX_LINES = 3;              // 한 번에 최대 3줄만

    [Header("Options")]
    public bool hideIfOffscreen = false;
    [Tooltip("이름표를 체력바 기준 위로 얼마나 올릴지(px)")]
    public float nameYOffset = 26f;

    [Header("Dead UI")]
    [Range(0f, 1f)] public float deadAlpha = 0.2f;  // ★ 죽었을 때 투명도
    [Range(1f, 30f)] public float fadeLerpSpeed = 10f; // ★ 페이드 속도

    [Header("Buff Clock")]
    public BuffClockUI buffClockPrefab;
    [Tooltip("버프 타이머 Y 오프셋(px) - 이름표 기준 위")]
    public float buffYOffset = 48f; // 체력바 위 26 + 추가 22 정도

    class UiPair {
        public HpBar bar;
        public RectTransform nameRect;
        public TextMeshProUGUI nameText;

        public CanvasGroup barCg;
        public CanvasGroup nameCg;

        public float curAlphaBar = 1f;
        public float curAlphaName = 1f;

        public BuffClockUI clock;
        public RectTransform clockRect;
    }

    Dictionary<int, UiPair> ui = new();
    RectTransform canvasRect;

    void Awake() {
        Instance = this;
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        if (!worldCamera) worldCamera = Camera.main;
        if (!container) container = canvasRect;
    }

    public void Attach(Player p) {
        if (!p) return;
        if (ui.ContainsKey(p.PlayerId)) return;

        // 1) 체력바
        var bar = Instantiate(hpBarPrefab, container);
        bar.rect.pivot = new Vector2(0.5f, 0f);
        bar.Bind(p);

        // CanvasGroup 확보(없으면 추가)
        var barCg = bar.GetComponent<CanvasGroup>();
        if (!barCg) barCg = bar.gameObject.AddComponent<CanvasGroup>();
        barCg.alpha = 1f;

        // 2) 이름표
        RectTransform nameRect = null;
        TextMeshProUGUI nameText = null;
        CanvasGroup nameCg = null;
        bool enableNameplate = SettingsApplier.IsNameplateEnabled;

        if (namePrefab) {
            var go = Instantiate(namePrefab, container);
            nameRect = go.transform as RectTransform;
            if (nameRect) nameRect.pivot = new Vector2(0.5f, 0f);

            nameText = go.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText) {
                string label = string.IsNullOrEmpty(p.NickName) ? $"P{p.PlayerId}" : p.NickName;
                nameText.text = label;
                if (p is MyPlayer) nameText.color = Color.green;
            }

            nameCg = go.GetComponent<CanvasGroup>();
            if (!nameCg) nameCg = go.AddComponent<CanvasGroup>();
            nameCg.alpha = 1f;
        }

        // 3) ★ 버프 시계
        BuffClockUI clock = null; RectTransform clockRect = null;
        if (buffClockPrefab) {
            clock = Instantiate(buffClockPrefab, container);
        } else {
            // 프리팹이 없으면 런타임 생성
            var go = new GameObject("BuffClock", typeof(RectTransform), typeof(BuffClockUI));
            go.transform.SetParent(container, false);
            clock = go.GetComponent<BuffClockUI>();
        }
        clockRect = clock.transform as RectTransform;
        if (clockRect) clockRect.pivot = new Vector2(0.5f, 0f);
        clock.Hide();

        ui[p.PlayerId] = new UiPair {
            bar = bar,
            barCg = barCg,
            nameRect = nameRect,
            nameText = nameText,
            nameCg = nameCg,
            curAlphaBar = 1f,
            curAlphaName = 1f,
            clock = clock,
            clockRect = clockRect
        };
    }

    public void Detach(Player p) {
        if (!p) return;
        if (ui.TryGetValue(p.PlayerId, out var pair)) {
            if (pair.bar) Destroy(pair.bar.gameObject);
            if (pair.nameRect) Destroy(pair.nameRect.gameObject);
            ui.Remove(p.PlayerId);
        }
    }

    void LateUpdate() {
        if (ui.Count == 0) return;

        foreach (var kv in ui) {
            var pair = kv.Value;
            var view = pair.bar;
            var p = view ? view.target : null;

            if (!p || !view) {
                if (view) view.gameObject.SetActive(false);
                if (pair.nameRect) pair.nameRect.gameObject.SetActive(false);
                continue;
            }

            // 1) 월드→스크린
            Vector3 worldPos = p.uiAnchor ? p.uiAnchor.position : (p.transform.position + p.uiOffset);
            var cam = worldCamera ? worldCamera : Camera.main;
            Vector3 screen = cam.WorldToScreenPoint(worldPos);

            // 2) 가시성
            bool behind = screen.z < 0f;
            bool offscreen = (screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height);
            bool visible = !(hideIfOffscreen && (behind || offscreen));

            // ★ 카운트다운 타이머는 화면 밖이면 무조건 숨김
            if (pair.clock != null) {
                bool clockVisible = !(behind || offscreen);
                pair.clock.SetWorldVisible(clockVisible);
            }

            if (view) view.gameObject.SetActive(visible);

            bool nameEnabled = SettingsApplier.IsNameplateEnabled;
            if (Input.GetKey(KeyCode.LeftAlt)) {
                if (nameEnabled) {
                    nameEnabled = false;
                } else {
                    nameEnabled = true;
                }
                
            }

            if (pair.nameRect) pair.nameRect.gameObject.SetActive(visible && nameEnabled);
            if (!visible) continue;

            // 3) 스크린→캔버스 로컬
            Vector2 uiPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, new Vector2(screen.x, screen.y),
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out uiPos
            );

            // 4) 위치 적용
            view.rect.anchoredPosition = uiPos;
            if (pair.nameRect) pair.nameRect.anchoredPosition = uiPos + new Vector2(0f, nameYOffset);

            // 5) 체력바 값
            view.RefreshInstant();

            // 6) ★ 생사 상태에 따라 알파 페이드
            float targetAlpha = (p.HP > 0) ? 1f : deadAlpha;
            float k = Time.deltaTime * fadeLerpSpeed;

            if (pair.barCg) {
                pair.curAlphaBar = Mathf.Lerp(pair.curAlphaBar, targetAlpha, k);
                // 소수점 흔들림 방지: 근접시 스냅
                if (Mathf.Abs(pair.curAlphaBar - targetAlpha) < 0.01f) pair.curAlphaBar = targetAlpha;
                pair.barCg.alpha = pair.curAlphaBar;
            }
            if (pair.nameCg) {
                pair.curAlphaName = Mathf.Lerp(pair.curAlphaName, targetAlpha, k);
                if (Mathf.Abs(pair.curAlphaName - targetAlpha) < 0.01f) pair.curAlphaName = targetAlpha;
                pair.nameCg.alpha = pair.curAlphaName;
            }
            //if (pair.clockRect) {
            //    // 이름표가 있으면 그 위에, 없으면 체력바 기준
            //    Vector2 basePos;
            //    if (pair.nameRect && nameEnabled)
            //        basePos = pair.nameRect.anchoredPosition;
            //    else
            //        basePos = view.rect.anchoredPosition + new Vector2(0f, nameYOffset);
            //
            //    pair.clockRect.anchoredPosition = basePos + new Vector2(0f, buffYOffset);
            //}
            if (pair.clockRect) {
                Vector2 pos;

                if (pair.nameRect && nameEnabled) {
                    // 이름표 켜져 있을 때: 이름표 기준 위로 buffYOffset
                    pos = pair.nameRect.anchoredPosition + new Vector2(0f, buffYOffset);
                } else {
                    // 이름표 꺼져 있을 때:
                    // 체력바에서 nameYOffset만큼 올린 "원래 이름표 자리"에 버프 시계 배치
                    pos = view.rect.anchoredPosition + new Vector2(0f, nameYOffset);
                }

                pair.clockRect.anchoredPosition = pos;
            }
        }
    }

    public void StartBuffTimer(Player p, SupplyEffect eff, float seconds) {
        if (!p) return;
        if (!ui.TryGetValue(p.PlayerId, out var pair) || pair.clock == null) return;

        // 효과별 색상 맵
        Color col = eff switch {
            SupplyEffect.RangeUp => new Color(0.95f, 0.65f, 0.20f),
            SupplyEffect.SpeedUp => new Color(0.25f, 0.60f, 1.00f),
            SupplyEffect.DamageUp => new Color(1.00f, 0.30f, 0.30f),
            SupplyEffect.Vomit => new Color(0.40f, 0.95f, 0.30f),
            SupplyEffect.Invincible => new Color(0.95f, 0.95f, 0.35f),
            SupplyEffect.Giant => new Color(0.75f, 0.55f, 0.95f),
            _ => Color.white
        };

        // 지속시간 없는 HealFull은 표시 안 함
        if (eff == SupplyEffect.HealFull) { pair.clock.Hide(); return; }

        pair.clock.Show(seconds, col);
    }

    public void ShowToast(Player p, string msg, Color col, float life = 1.2f, bool coalesceSame = true) {
        if (p == null || toastPrefab == null) return;

        // 내 캐릭터만 토스트 (원하면 제거)
        //if (!(p is MyPlayer)) return;

        if (!_toastsByPlayer.TryGetValue(p, out var list)) {
            list = new List<ToastItem>(4);
            _toastsByPlayer[p] = list;
        }

        // 같은 메시지면 갱신으로 합치기(시간 연장 + 카운트 x2 같은 꼬리표)
        if (coalesceSame) {
            for (int i = 0; i < list.Count; i++) {
                if (list[i] && list[i].TryCoalesce(msg)) {
                    // 갱신되면 레이아웃만 정리하고 끝
                    LayoutToasts(p, list);
                    return;
                }
            }
        }

        // 새 토스트 생성
        var go = Instantiate(toastPrefab, container);
        var ti = go.GetComponent<ToastItem>();
        if (!ti) ti = go.AddComponent<ToastItem>();
        ti.Init(this, p, msg, col, life);

        list.Insert(0, ti); // 위에 쌓기

        // 최대 줄 제한
        while (list.Count > MAX_LINES) {
            var last = list[list.Count - 1];
            if (last) last.FastKill();
            list.RemoveAt(list.Count - 1);
        }

        LayoutToasts(p, list);
    }

    void LayoutToasts(Player p, List<ToastItem> list) {
        // 각 줄 y 오프셋 배치
        for (int i = 0; i < list.Count; i++) {
            if (!list[i]) continue;
            list[i].SetLineOffset(i * LINE_HEIGHT);
        }
    }

    // 매 프레임 토스트의 화면 위치 갱신 요청
    public void UpdateToastAnchor(Player p, ToastItem item, float lineOffset) {
        if (p == null || item == null) return;

        // 앵커: 이름표 위치 위쪽 살짝
        var world = (p.uiAnchor ? p.uiAnchor.position : p.transform.position + p.uiOffset) + Vector3.up * 0.45f;
        var screenPos = RectTransformUtility.WorldToScreenPoint(worldCamera ? worldCamera : Camera.main, world);

        // 스크린 좌표 -> 캔버스 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            container, screenPos, null, out var local);

        // 줄 오프셋 반영 (위로 쌓기)
        local.y += lineOffset;

        item.rect.anchoredPosition = local;
    }

    // 토스트가 수명 종료될 때 호출
    public void OnToastFinished(Player p, ToastItem item) {
        if (p == null) return;
        if (_toastsByPlayer.TryGetValue(p, out var list)) {
            list.Remove(item);
            LayoutToasts(p, list);
        }
    }

    public void ClearAllBuffClocks() {
        var clocks = FindObjectsByType<BuffClockUI>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);

        foreach (var clock in clocks) {
            if (clock != null) {
                clock.Hide();
            }
        }
    }
}
