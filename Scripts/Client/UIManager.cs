using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    public static UIManager Instance { get; private set; }

    [Header("Assign in Inspector")]
    [Tooltip("구조: popupPrefab > Canvas > Panel(Image) > TitleText(TextMeshProUGUI), SubText(TextMeshProUGUI), Image(Image)")]
    public GameObject popupPrefab;

    [Header("Toast Timing (seconds)")]
    public float fadeIn = 0.15f;
    public float hold = 1.2f;
    public float fadeOut = 0.25f;

    [Header("Pop / Collapse Motion")]
    public float showScaleFrom = 0.75f;
    public float showOvershoot = 1.06f;
    public float showDuration = 0.18f;
    public float hideDuration = 0.22f;
    public bool useUnscaledTime = true;

    [SerializeField] float bottomMargin = 80f; // 하단 여백(px)

    GameObject _popupInstance;
    Canvas _canvas;
    RectTransform _panelRect;
    CanvasGroup _panelGroup;
    Image _panelImage;
    TextMeshProUGUI _titleTMP;
    TextMeshProUGUI _subTMP;
    Image _iconImg;

    Coroutine _toastCo;


    [Serializable]
    public struct ToastOptions {
        public string iconName; 
        public Color? iconTint;

        public Color? titleColor;
        public float? titleFontSize;
        public FontStyles? titleStyle;

        public Color? subColor;
        public float? subFontSize;
        public FontStyles? subStyle;
    }

    public enum ToastTheme { None, Success, Error, Warning, Info }

    static string L(string key) {
        const string TABLE = "GameTexts";

        var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(TABLE, key);
        if (!op.IsDone) op.WaitForCompletion();

        var v = op.Result;
        return string.IsNullOrEmpty(v) ? key : v;
    }

    string LF(string key, params object[] args) {
        var fmt = L(key);
        return (args != null && args.Length > 0) ? string.Format(fmt, args) : fmt;
    }


    void Awake() {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!popupPrefab) { Debug.LogError("[UIManager] popupPrefab 미지정"); return; }
        SpawnOnce();
        EnsureEventSystem();
    }

    void SpawnOnce() {
        if (_popupInstance) return;

        _popupInstance = Instantiate(popupPrefab);
        _popupInstance.name = "PopupCanvas (Toast)";
        DontDestroyOnLoad(_popupInstance);

        _canvas = _popupInstance.GetComponentInChildren<Canvas>(true);

        var panelTr = _popupInstance.transform.Find("Canvas/Panel");
        if (!panelTr) panelTr = _popupInstance.transform.Find("Panel");
        if (!panelTr) { Debug.LogError("[UIManager] Canvas/Panel 찾기 실패"); return; }

        _panelRect = (RectTransform)panelTr;
        _panelGroup = panelTr.GetComponent<CanvasGroup>() ?? panelTr.gameObject.AddComponent<CanvasGroup>();
        _panelImage = panelTr.GetComponent<Image>();

        _titleTMP = panelTr.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        _subTMP = panelTr.Find("SubText")?.GetComponent<TextMeshProUGUI>();

        var iconTr = panelTr.Find("Image");
        if (iconTr) {
            _iconImg = iconTr.GetComponent<Image>();
            if (_iconImg) {
                _iconImg.raycastTarget = false;
                _iconImg.preserveAspect = true; // 비율 유지
            }
        } else {
            Debug.LogWarning("[UIManager] Panel 하위에 'Image' 오브젝트가 없습니다. 아이콘 없이 동작합니다.");
        }

        // 정중앙 기준
        //_panelRect.pivot = _panelRect.anchorMin = _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        //_panelRect.anchoredPosition = Vector2.zero;

        // 아래 중앙 기준
        _panelRect.anchorMin = new Vector2(0.5f, 0f);
        _panelRect.anchorMax = new Vector2(0.5f, 0f);
        _panelRect.pivot = new Vector2(0.5f, 0f);
        _panelRect.anchoredPosition = new Vector2(0f, bottomMargin); // 아래에서 위로 여백

        // 위 중앙 기준
        //_panelRect.anchorMin = new Vector2(0.5f, 1f);
        //_panelRect.anchorMax = new Vector2(0.5f, 1f);
        //_panelRect.pivot = new Vector2(0.5f, 1f);
        //_panelRect.anchoredPosition = new Vector2(0f, -bottomMargin);

        _panelRect.localScale = Vector3.one;

        _panelGroup.alpha = 0f;
        _panelRect.gameObject.SetActive(false);
    }

    void EnsureEventSystem() {
        if (!FindFirstObjectByType<EventSystem>())
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }



    public static void ShowToast(string message, float? holdOverride = null) {
        if (!Instance) { Debug.LogError("[UIManager] Instance 없음"); return; }
        Instance._ShowToast(null, message, ToastTheme.Info, null, holdOverride);
    }

    public static void ShowToast(string message, ToastTheme theme, float? holdOverride = null) {
        if (!Instance) { Debug.LogError("[UIManager] Instance 없음"); return; }
        Instance._ShowToast(null, message, theme, null, holdOverride);
    }

    public static void ShowToast(string title, string sub = null, ToastTheme theme = ToastTheme.None, ToastOptions? options = null, float? holdOverride = null) {
        if (!Instance) { Debug.LogError("[UIManager] Instance 없음"); return; }
        Instance._ShowToast(title, sub, theme, options, holdOverride);
    }

    public static void ShowSuccess(string sub, string icon = "check") => ShowToast(null, sub, ToastTheme.Success, new ToastOptions { iconName = icon });
    public static void ShowError(string sub, string icon = "error") => ShowToast(null, sub, ToastTheme.Error, new ToastOptions { iconName = icon });
    public static void ShowWarning(string sub, string icon = "warning") => ShowToast(null, sub, ToastTheme.Warning, new ToastOptions { iconName = icon });
    public static void ShowInfo(string sub, string icon = "info") => ShowToast(null, sub, ToastTheme.Info, new ToastOptions { iconName = icon });


    const string TABLE = "GameTexts";
    public static void ShowErrorKey(string key, params object[] args) {
        if (!Instance) { Debug.LogError("[UIManager] Instance 없음"); return; }
        Instance._ShowToast(null, Instance.LF(key, args), ToastTheme.Error, null, null);
    }

    public static void ShowSuccessKey(string key, params object[] args) {
        if (!Instance) { Debug.LogError("[UIManager] Instance 없음"); return; }
        Instance._ShowToast(null, Instance.LF(key, args), ToastTheme.Success, null, null);
    }

    public static void ShowWarningKey(string key, params object[] args) {
        if (!Instance) { Debug.LogError("[UIManager] Instance 없음"); return; }
        Instance._ShowToast(null, Instance.LF(key, args), ToastTheme.Warning, null, null);
    }

    public static void ShowInfoKey(string key, params object[] args) {
        if (!Instance) { Debug.LogError("[UIManager] Instance 없음"); return; }
        Instance._ShowToast(null, Instance.LF(key, args), ToastTheme.Info, null, null);
    }

    public static void ShowLevelUp(int fromLevel, int toLevel, int rewardGold, int rewardStar) {
        if (!Instance) {
            Debug.LogError("[UIManager] Instance 없음 (ShowLevelUp)");
            return;
        }

        string title = string.Format(L("TOAST_LEVELUP_TITLE"), toLevel);

        string sub = "";
        if (rewardGold > 0 || rewardStar > 0) {
            if (rewardGold > 0)
                sub += $"{L("CURRENCY_GOLD")} +{rewardGold:N0}";
            if (rewardStar > 0) {
                if (rewardGold > 0) sub += "   ";
                sub += $"{L("CURRENCY_STAR")} +{rewardStar:N0}";
            }
        }

        var opt = new ToastOptions {
            iconName = "levelup",
            titleFontSize = 30f,
            titleStyle = FontStyles.Bold,
            subFontSize = 22f
        };


        float longHold = Instance.hold * 4.0f;

        Instance._ShowToast(title, sub, ToastTheme.Success, opt, longHold);
    }



    string TitleForTheme(ToastTheme theme) {
        switch (theme) {
            case ToastTheme.Success: return L("TOAST_TITLE_SUCCESS");
            case ToastTheme.Error: return L("TOAST_TITLE_ERROR");
            case ToastTheme.Warning: return L("TOAST_TITLE_WARNING");
            case ToastTheme.Info:
            case ToastTheme.None:
            default: return L("TOAST_TITLE_INFO");
        }
    }

    void _ShowToast(string title, string sub, ToastTheme theme, ToastOptions? options, float? holdOverride) {
        if (!_panelRect || !_panelGroup) return;


        string finalTitle = string.IsNullOrEmpty(title) ? TitleForTheme(theme) : title;

        if (_titleTMP) { _titleTMP.text = finalTitle; _titleTMP.fontStyle = FontStyles.Normal; }
        if (_subTMP) { _subTMP.text = sub ?? ""; _subTMP.gameObject.SetActive(!string.IsNullOrEmpty(sub)); }


        Color tCol = Color.white;
        Color sCol = new Color(1, 1, 1, 0.82f);
        string defaultIcon = null;
        Color iconTint = Color.white;

        switch (theme) {
            case ToastTheme.Success: tCol = new Color(0.70f, 1f, 0.92f); defaultIcon = "check"; break;
            case ToastTheme.Error: tCol = new Color(1f, 0.62f, 0.62f); defaultIcon = "error"; break;
            case ToastTheme.Warning: tCol = new Color(1f, 0.93f, 0.65f); defaultIcon = "warning"; break;
            case ToastTheme.Info: tCol = new Color(0.70f, 0.92f, 1f); defaultIcon = "info"; break;
        }


        var o = options ?? default;
        if (_titleTMP) {
            _titleTMP.color = o.titleColor ?? tCol;
            if (o.titleFontSize.HasValue) _titleTMP.fontSize = o.titleFontSize.Value;
            _titleTMP.fontStyle = o.titleStyle ?? _titleTMP.fontStyle;
        }
        if (_subTMP) {
            _subTMP.color = o.subColor ?? sCol;
            if (o.subFontSize.HasValue) _subTMP.fontSize = o.subFontSize.Value;
            _subTMP.fontStyle = o.subStyle ?? _subTMP.fontStyle;
        }


        if (_iconImg) {
            string iconName = o.iconName ?? defaultIcon;
            if (!string.IsNullOrEmpty(iconName)) {
                var sp = Resources.Load<Sprite>($"Image/RawImage/{iconName}");
                ApplyIconSprite(sp, o.iconTint ?? iconTint);
            } else {
                ApplyIconSprite(null, Color.white);
            }
        }

        // 표시
        _panelRect.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRect);
        if (_toastCo != null) StopCoroutine(_toastCo);
        _toastCo = StartCoroutine(CoToast(holdOverride ?? hold));
    }

    void ApplyIconSprite(Sprite sp, Color tint) {
        if (!_iconImg) return;
        _iconImg.sprite = sp;
        _iconImg.color = sp ? tint : Color.clear;
        _iconImg.enabled = sp != null;

    }


    IEnumerator CoToast(float holdTime) {
        _panelGroup.alpha = 0f;
        _panelRect.localScale = Vector3.one * Mathf.Max(0.0001f, showScaleFrom);
        yield return ScaleAndFade(_panelRect, _panelGroup, showScaleFrom, showOvershoot, 0f, 1f, showDuration, EaseOutBack);
        yield return ScaleOnly(_panelRect, showOvershoot, 1f, showDuration * 0.35f, EaseOutCubic);
        yield return Wait(holdTime);
        yield return ScaleAndFade(_panelRect, _panelGroup, 1f, 0.0f, 1f, 0f, hideDuration, EaseInCubic);
        _panelRect.gameObject.SetActive(false);
        _toastCo = null;
    }

    IEnumerator ScaleAndFade(RectTransform rt, CanvasGroup cg, float s0, float s1, float a0, float a1, float dur, Func<float, float> ease) {
        if (dur <= 0f) { rt.localScale = Vector3.one * s1; cg.alpha = a1; yield break; }
        float t = 0f; rt.localScale = Vector3.one * s0; cg.alpha = a0;
        while (t < dur) {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / dur); float e = ease != null ? ease(k) : k;
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(s0, s1, e);
            cg.alpha = Mathf.LerpUnclamped(a0, a1, e);
            yield return null;
        }
        rt.localScale = Vector3.one * s1; cg.alpha = a1;
    }

    IEnumerator ScaleOnly(RectTransform rt, float s0, float s1, float dur, Func<float, float> ease) {
        if (dur <= 0f) { rt.localScale = Vector3.one * s1; yield break; }
        float t = 0f; rt.localScale = Vector3.one * s0;
        while (t < dur) {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / dur); float e = ease != null ? ease(k) : k;
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(s0, s1, e);
            yield return null;
        }
        rt.localScale = Vector3.one * s1;
    }

    IEnumerator Wait(float sec) {
        if (sec <= 0f) yield break;
        if (useUnscaledTime) { float t = 0f; while (t < sec) { t += Time.unscaledDeltaTime; yield return null; } } else yield return new WaitForSeconds(sec);
    }

    static float EaseOutBack(float x) { const float s = 1.70158f; float t = x - 1f; return t * t * ((s + 1f) * t + s) + 1f; }
    static float EaseOutCubic(float x) { return 1f - Mathf.Pow(1f - x, 3f); }
    static float EaseInCubic(float x) { return x * x * x; }
}
