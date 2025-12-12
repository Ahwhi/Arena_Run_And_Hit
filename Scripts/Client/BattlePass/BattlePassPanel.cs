using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePassPanel : MonoBehaviour {
    [Header("Top UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI passLevelText;
    public TextMeshProUGUI isPremiumText;
    public Button allGetButton;
    public Button minimizeButton;

    [Header("List")]
    public Transform content;
    public BattlePassItemUI battlePassPrefab;

    [Header("Scroll")]
    public ScrollRect scrollRect;

    [Header("Minimize")]
    public RectTransform rootRect;
    public RectTransform listRoot;
    public CanvasGroup listCanvasGroup;
    public float minimizedHeight = 160f;
    public float animDuration = 0.2f;

    const int MAX_LEVEL = 50;

    int _version;
    int _bpLevel;
    bool _hasPremium;
    ulong _freeBits;
    ulong _premiumBits;

    Dictionary<int, S_BattlePassInfo.Levels> _levelMap = new();
    readonly List<BattlePassItemUI> _freeItems = new();
    readonly List<BattlePassItemUI> _premiumItems = new();

    bool _built;
    bool _isMinimized = false;
    float _fullHeight = -1f;
    Coroutine _minimizeRoutine;

    void Awake() {
        if (allGetButton != null) {
            allGetButton.onClick.RemoveAllListeners();
            allGetButton.onClick.AddListener(OnClickAllGet);
        }

        if (minimizeButton != null) {
            minimizeButton.onClick.RemoveAllListeners();
            minimizeButton.onClick.AddListener(OnClickToggleMinimize);
            UpdateMinimizeButtonLabel();
        }

        if (rootRect != null) {
            var h = rootRect.sizeDelta.y;
            if (h <= 0f) h = rootRect.rect.height;
            _fullHeight = h;
        }

        if (listCanvasGroup != null) {
            listCanvasGroup.alpha = 1f;
        }
        if (listRoot != null) {
            listRoot.gameObject.SetActive(true);
        }
    }

    void OnEnable() {
        if (!_built) {
            BuildList();
            _built = true;
        }

        if (rootRect != null && _fullHeight <= 0f) {
            var h = rootRect.sizeDelta.y;
            if (h <= 0f) h = rootRect.rect.height;
            _fullHeight = h;
        }

        if (ShopCache.Items == null || ShopCache.Items.Count == 0) {
            return;
        }

        RequestBattlePass();
    }

    void BuildList() {
        if (!content || !battlePassPrefab) {
            Debug.LogError("[BattlePassPanel] content 또는 battlePassPrefab 미지정");
            return;
        }

        for (int i = content.childCount - 1; i >= 0; --i)
            Destroy(content.GetChild(i).gameObject);
        _freeItems.Clear();
        _premiumItems.Clear();

        for (int level = 1; level <= MAX_LEVEL; level++) {
            // Free
            var freeUi = Instantiate(battlePassPrefab, content);
            freeUi.name = $"BP_Free_{level:00}";
            freeUi.Init(level, false, OnClickGetReward);
            _freeItems.Add(freeUi);

            // Premium
            var premiumUi = Instantiate(battlePassPrefab, content);
            premiumUi.name = $"BP_Premium_{level:00}";
            premiumUi.Init(level, true, OnClickGetReward);
            _premiumItems.Add(premiumUi);
        }
    }

    public void RequestBattlePass() {
        var pkt = new C_RequestBattlePass();
        NetworkManager.Send(pkt.Write());
    }

    public static void OnBattlePassInfo(S_BattlePassInfo pkt) {
        var panel = FindAnyObjectByType<BattlePassPanel>();
        if (panel != null)
            panel.ApplyInfo(pkt);
    }

    public static void OnClaimResult(S_ClaimBattlePassRewardResult pkt) {
        var panel = FindAnyObjectByType<BattlePassPanel>();
        if (panel == null) return;

        if (!pkt.isSuccess) {
            UIManager.ShowErrorKey("BP_FAIL_" + pkt.failReason);
            return;
        }

        panel.RequestBattlePass();
    }

    public static void OnClaimAllResult(S_ClaimBattlePassAllResult pkt) {
        var panel = FindAnyObjectByType<BattlePassPanel>();
        if (panel == null) return;

        if (!pkt.isSuccess) {
            UIManager.ShowErrorKey("BP_FAIL_" + pkt.failReason);
            return;
        }

        if (pkt.claimedCount > 0) {
            UIManager.ShowSuccessKey("BTPS5");
        }

        panel.RequestBattlePass();
    }


    public void ApplyInfo(S_BattlePassInfo pkt) {
        _version = pkt.version;
        _bpLevel = pkt.bpLevel;
        _hasPremium = pkt.hasPremium;
        _freeBits = (ulong)pkt.freeClaimBits;
        _premiumBits = (ulong)pkt.premiumClaimBits;

        if (nameText != null)
            nameText.text = LanguageSwitcher.LF("BTPS1", _version);

        if (passLevelText != null)
            passLevelText.text = LanguageSwitcher.LF("BTPS2", _bpLevel, MAX_LEVEL);

        if (isPremiumText != null) {
            isPremiumText.text = _hasPremium ? LanguageSwitcher.L("BTPS3") : LanguageSwitcher.L("BTPS4");
            isPremiumText.color = _hasPremium ? Color.yellow : Color.gray;
        }
            

        _levelMap.Clear();
        foreach (var lv in pkt.levelss) {
            _levelMap[lv.level] = lv;
        }

        RefreshItems();

        if (!_isMinimized)
            StartCoroutine(CoScrollToCurrentLevel());
    }

    System.Collections.IEnumerator CoScrollToCurrentLevel() {
        yield return null;
        ScrollToCurrentLevel();
    }

    void RefreshItems() {
        for (int level = 1; level <= MAX_LEVEL; level++) {
            _levelMap.TryGetValue(level, out var info);
            bool isUnlocked = _bpLevel >= level;

            if (level - 1 < _freeItems.Count) {
                var ui = _freeItems[level - 1];

                BuildRewardVisual(info, level, false,
                    out var icon, out var text);

                bool claimed = IsClaimed(_freeBits, level);
                ui.ApplyState(icon, text, isUnlocked, claimed, lockedByPremium: false);
            }

            if (level - 1 < _premiumItems.Count) {
                var ui = _premiumItems[level - 1];

                BuildRewardVisual(info, level, true,
                    out var icon, out var text);

                bool claimed = IsClaimed(_premiumBits, level);
                bool lockedByPremium = !_hasPremium;
                ui.ApplyState(icon, text, isUnlocked, claimed, lockedByPremium);
            }
        }

        if (allGetButton != null)
            allGetButton.interactable = HasAnyClaimable();

        Canvas.ForceUpdateCanvases();
        if (content is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    bool HasAnyClaimable() {
        for (int level = 1; level <= _bpLevel && level <= MAX_LEVEL; level++) {
            if (!IsClaimed(_freeBits, level))
                return true;

            if (_hasPremium && !IsClaimed(_premiumBits, level))
                return true;
        }
        return false;
    }

    static bool IsClaimed(ulong bits, int level) {
        int idx = level - 1;
        if (idx < 0 || idx >= 64) return false;
        return (bits & (1UL << idx)) != 0;
    }

    void BuildRewardVisual(S_BattlePassInfo.Levels info, int level, bool isPremium,
                       out Sprite icon, out string text) {
        icon = null;
        text = "";

        if (info == null) {
            text = isPremium ? $"프리미엄 Lv.{level}" : $"무료 Lv.{level}";
            return;
        }

        int gold = isPremium ? info.premiumGold : info.freeGold;
        int star = isPremium ? info.premiumStar : info.freeStar;

        List<string> skus;
        if (isPremium)
            skus = info.premiumItemSkuss?.Select(x => x.sku).ToList() ?? new List<string>();
        else
            skus = info.freeItemSkuss?.Select(x => x.sku).ToList() ?? new List<string>();

        string mainSku = skus.Count > 0 ? skus[0] : null;
        string mainItemName = null;

        if (!string.IsNullOrEmpty(mainSku) && ShopCache.Items.TryGetValue(mainSku, out var meta)) {
            if (!string.IsNullOrEmpty(meta.imageKey))
                icon = Resources.Load<Sprite>($"Image/{meta.imageKey}");

            mainItemName = LocalizeIfReady(meta.name);
        }

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(mainItemName)) {
            parts.Add(mainItemName);
            if (skus.Count > 1)
                parts.Add($"외 {skus.Count - 1}개");
        }

        if (gold > 0)
            parts.Add(LanguageSwitcher.LF("BTPS6", gold));

        if (star > 0)
            parts.Add(LanguageSwitcher.LF("BTPS7", star));

        if (parts.Count == 0)
            text = isPremium ? $"프리미엄 Lv.{level}" : $"무료 Lv.{level}";
        else
            text = string.Join(" / ", parts);

        if (icon == null) {
            string currencyIconKey = null;
            if (gold > 0 && star == 0) currencyIconKey = "gold";
            else if (star > 0) currencyIconKey = "star";

            if (!string.IsNullOrEmpty(currencyIconKey))
                icon = Resources.Load<Sprite>($"Image/{currencyIconKey}");
        }
    }

    string LocalizeIfReady(string source) {
        if (string.IsNullOrEmpty(source))
            return source;

        var loc = AutoLocalizeByTextMatch.Instance;
        if (loc == null)
            return source;

        return loc.LocalizeImmediate(source);
    }

    void OnClickGetReward(int level, bool isPremium) {
        SoundManager.I?.Play2D(SfxId.MouseClick);

        var pkt = new C_ClaimBattlePassReward {
            level = level,
            isPremium = isPremium
        };
        NetworkManager.Send(pkt.Write());
    }

    void OnClickAllGet() {
        SoundManager.I?.Play2D(SfxId.MouseClick);

        var pkt = new C_ClaimBattlePassAll();
        NetworkManager.Send(pkt.Write());
    }

    void OnClickToggleMinimize() {
        SetMinimized(!_isMinimized, true);
    }

    void SetMinimized(bool minimize, bool playAnim) {
        if (_isMinimized == minimize)
            return;

        _isMinimized = minimize;
        UpdateMinimizeButtonLabel();
        UpdateAllGetVisible();

        if (_minimizeRoutine != null)
            StopCoroutine(_minimizeRoutine);

        _minimizeRoutine = StartCoroutine(CoMinimizeAnim(minimize, playAnim));
    }

    void UpdateMinimizeButtonLabel() {
        if (minimizeButton == null) return;

        var txt = minimizeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (txt == null) return;

        txt.text = _isMinimized ? "+" : "-";
    }

    System.Collections.IEnumerator CoMinimizeAnim(bool minimize, bool playAnim) {
        if (rootRect == null) yield break;

        if (_fullHeight <= 0f) {
            var h = rootRect.sizeDelta.y;
            if (h <= 0f) h = rootRect.rect.height;
            _fullHeight = h;
        }

        float fromH = rootRect.sizeDelta.y;
        if (fromH <= 0f) fromH = _fullHeight;

        float toH = minimize ? minimizedHeight : _fullHeight;

        float fromA = 1f;
        float toA = minimize ? 0f : 1f;

        if (listCanvasGroup != null)
            fromA = listCanvasGroup.alpha;

        if (!minimize && listRoot != null) {
            listRoot.gameObject.SetActive(true);
        }

        if (!playAnim || animDuration <= 0f) {
            SetPanelHeight(toH);
            if (listCanvasGroup != null)
                listCanvasGroup.alpha = toA;

            if (listRoot != null)
                listRoot.gameObject.SetActive(!minimize);

            yield break;
        }

        float t = 0f;
        while (t < animDuration) {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / animDuration);
            float eased = Mathf.SmoothStep(0f, 1f, k);

            float h = Mathf.Lerp(fromH, toH, eased);
            SetPanelHeight(h);

            if (listCanvasGroup != null)
                listCanvasGroup.alpha = Mathf.Lerp(fromA, toA, eased);

            yield return null;
        }

        SetPanelHeight(toH);

        if (listCanvasGroup != null)
            listCanvasGroup.alpha = toA;

        if (listRoot != null)
            listRoot.gameObject.SetActive(!minimize);
    }

    void SetPanelHeight(float h) {
        if (rootRect == null)
            return;

        var size = rootRect.sizeDelta;
        size.y = h;
        rootRect.sizeDelta = size;
    }

    void UpdateAllGetVisible() {
        if (allGetButton == null) return;
        allGetButton.gameObject.SetActive(!_isMinimized);
    }

    void ScrollToCurrentLevel() {
        int level = Mathf.Clamp(_bpLevel, 1, MAX_LEVEL);
        ScrollToLevelHorizontal(level);
    }

    void ScrollToLevelHorizontal(int level) {
        if (scrollRect == null)
            return;

        if (!scrollRect.horizontal)
            return;

        level = Mathf.Clamp(level, 1, MAX_LEVEL);

        float t = 0f;
        if (MAX_LEVEL > 1)
            t = (level - 1) / (float)(MAX_LEVEL - 1);

        scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(t);
    }


}
