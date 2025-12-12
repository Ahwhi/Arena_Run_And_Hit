using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePassItemUI : MonoBehaviour {
    [Header("Refs")]
    public TextMeshProUGUI levelText;
    public Image goodImage;
    public Image lockImage;
    public TextMeshProUGUI goodText;
    public Button getButton;

    int _level;
    bool _isPremium;
    Action<int, bool> _onClick;

    Sprite _originalLockSprite;
    static Sprite sCheckSprite;
    static bool sSpritesLoaded;

    // 머터리얼 캐시
    static Material sMatFree;
    static Material sMatPremium;
    static bool sMatsLoaded;

    void Awake() {
        if (lockImage != null)
            _originalLockSprite = lockImage.sprite;

        EnsureSprites();
        EnsureMaterials();
    }

    void EnsureSprites() {
        if (sSpritesLoaded) return;
        sSpritesLoaded = true;

        sCheckSprite = Resources.Load<Sprite>("Image/RawImage/check");
    }

    void EnsureMaterials() {
        if (sMatsLoaded) return;
        sMatsLoaded = true;

        sMatFree = Resources.Load<Material>("UI/Material/GlassBP/BP");
        sMatPremium = Resources.Load<Material>("UI/Material/GlassBP/BP_P");
    }

    public void Init(int level, bool isPremium, Action<int, bool> onClick) {
        _level = level;
        _isPremium = isPremium;
        _onClick = onClick;


        if (levelText != null) {
            levelText.text = isPremium
                ? $"Lv.{level} Premium"
                : $"Lv.{level}";
        }

        var img = GetComponent<Image>();
        if (img != null) {
            if (isPremium && sMatPremium != null)
                img.material = sMatPremium;
            else if (!isPremium && sMatFree != null)
                img.material = sMatFree;
        }



        if (getButton != null) {
            getButton.onClick.RemoveAllListeners();
            getButton.onClick.AddListener(OnClickGet);
        }
    }

    void OnClickGet() {
        _onClick?.Invoke(_level, _isPremium);
    }

    public void ApplyState(Sprite icon, string text,
                           bool isUnlocked,
                           bool isClaimed,
                           bool lockedByPremium)
    {
        if (goodImage != null) {
            goodImage.sprite = icon;
            goodImage.enabled = (icon != null);
        }

        if (goodText != null)
            goodText.text = text ?? "";

        bool showCheck = isClaimed;
        bool showLock = !isClaimed && (!isUnlocked || lockedByPremium);

        if (lockImage != null) {
            if (showCheck && sCheckSprite != null) {
                lockImage.gameObject.SetActive(true);
                lockImage.sprite = sCheckSprite;
            } else if (showLock) {
                lockImage.gameObject.SetActive(true);
                lockImage.sprite = _originalLockSprite;
            } else {
                lockImage.gameObject.SetActive(false);
            }
        }

        if (getButton != null) {
            getButton.interactable = isUnlocked && !lockedByPremium && !isClaimed;
            getButton.gameObject.SetActive(isUnlocked && !lockedByPremium && !isClaimed);
        }
    }
}
