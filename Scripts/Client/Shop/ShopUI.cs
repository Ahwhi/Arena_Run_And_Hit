using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public static class ShopCache {
    public static int Version = -1;

    // Wallet
    public static int Gold, Star;

    // Items meta: sku -> (name, category, imageKey)
    public class ItemMeta { public string name, category, imageKey; }
    public static readonly Dictionary<string, ItemMeta> Items = new();

    // Ownership & Equipped
    public static readonly HashSet<string> Owned = new();
    public static readonly Dictionary<string, string> EquippedBySlot = new(); // slot->sku
    public static bool IsEquipped(string sku) => EquippedBySlot.Values.Contains(sku);

    // Store offers (Shop UI 전용)
    public class StoreOfferVo {
        public string offerId, displayName, imageKey, category;
        public bool visible;
        public List<(string currency, int amount)> prices = new();
    }
    public static readonly List<StoreOfferVo> Offers = new();
}

public class ShopUI : MonoBehaviour {
    public static ShopUI Instance;

    [Header("Prefabs & Parents")]
    public GameObject storePrefab;
    public Transform panelCharacter;
    public Transform panelTrail;
    public Transform panelDance;
    public Transform panelMisc;
    public Transform panelBuy;

    [Header("Shop Materials")]
    public Material ownedThumbnailMat;   // 보유 썸네일용
    public Material defaultThumbnailMat; // 기본 썸네일용(없으면 기존 유지)

    // ★ 구매 팝업 내부 위젯들
    Image _buyKindImage;
    TextMeshProUGUI _buyNameText;
    TextMeshProUGUI _buyPriceText;
    Button _buyButton;
    Button _cancelButton;

    // ★ 현재 선택된 상품
    string _pendingOfferId;

    // ★ 3D 프리뷰 관련
    [Header("3D Preview")]
    public Transform previewRoot;      // ShopCharacterPreviewRoot
    public Transform previewAnchor;    // PreviewAnchor (캐릭터 놓는 자리)

    GameObject _previewInstance;
    public GameObject Panel_Preview;

    private void Awake() {
        Instance = this;
        InitBuyPanel();   // ★ 팝업 초기화
        if (Panel_Preview != null)
            Panel_Preview.SetActive(false);   // 시작은 꺼두기
    }

    void InitBuyPanel() {
        if (!panelBuy) return;

        // panelBuy의 자식에서 필요한 컴포넌트 찾아오기
        _buyKindImage = panelBuy.transform.Find("KindImage")?.GetComponent<Image>();
        _buyNameText = panelBuy.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        _buyPriceText = panelBuy.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
        _buyButton = panelBuy.transform.Find("Button_Buy")?.GetComponent<Button>();
        _cancelButton = panelBuy.transform.Find("Button_Cancel")?.GetComponent<Button>();

        if (_buyButton != null) {
            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(OnClickConfirmBuy);
        }

        if (_cancelButton != null) {
            _cancelButton.onClick.RemoveAllListeners();
            _cancelButton.onClick.AddListener(() => {
                SoundManager.I?.Play2D(SfxId.MouseClick);
                panelBuy.gameObject.SetActive(false);
                _pendingOfferId = null;
                HidePreview();
            });
        }

        // 시작할 때는 닫혀있게
        panelBuy.gameObject.SetActive(false);
    }

    void OnClickConfirmBuy() {
        if (string.IsNullOrEmpty(_pendingOfferId)) {
            panelBuy.gameObject.SetActive(false);
            return;
        }

        SoundManager.I?.Play2D(SfxId.MouseClick);

        var req = new C_BuyOffer {
            offerId = _pendingOfferId,
            idempotencyKey = System.Guid.NewGuid().ToString("N")
        };
        NetworkManager.Send(req.Write());

        panelBuy.gameObject.SetActive(false);
        _pendingOfferId = null;
        HidePreview();
    }

    void OpenBuyPanel(ShopCache.StoreOfferVo offer) {
        if (!panelBuy) return;
        if (_buyNameText == null || _buyPriceText == null || _buyKindImage == null) {
            InitBuyPanel(); // 혹시 Awake 전에 호출되면 대비
        }

        _pendingOfferId = offer.offerId;

        // 가격(첫 가격 기준)
        var price = offer.prices.FirstOrDefault();

        if (_buyNameText != null)
            _buyNameText.text = offer.displayName;

        if (_buyPriceText != null)
            _buyPriceText.text = price.amount.ToString("N0");

        if (_buyKindImage != null) {
            var icon = Resources.Load<Sprite>($"Image/{price.currency.ToLower()}"); // gold or star
            _buyKindImage.sprite = icon;
            _buyKindImage.enabled = (icon != null);
        }

        panelBuy.gameObject.SetActive(true);

        // ★ 캐릭터면 프리뷰, 아니면 끄기
        if ((offer.category ?? "").ToLower() == "character") {
            string sku = ResolveSkuForOffer(offer);
            if (!string.IsNullOrEmpty(sku))
                ShowPreviewCharacter(sku);
            else
                HidePreview();  // sku 못 찾으면 안전하게 끔
        } else {
            HidePreview();
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

    public void RebuildFromCache() {
        // 기존 자식 제거
        Clear(panelCharacter); Clear(panelTrail); Clear(panelDance); Clear(panelMisc);

        foreach (var o in ShopCache.Offers) {
            if (!o.visible) continue; //숨김

            var parent = PanelOf(o.category);
            if (parent == null) parent = panelMisc;

            var go = Instantiate(storePrefab, parent);

            // ImagePanel (메인 썸네일)
            var imgMain = go.transform.Find("ImagePanel")?.GetComponent<Image>();
            if (imgMain) {
                var sprite = Resources.Load<Sprite>($"Image/{o.imageKey}");
                if (sprite == null)
                    sprite = Resources.Load<Sprite>("Image/DefaultImage");  // 기본 이미지 폴백

                imgMain.sprite = sprite;
                imgMain.enabled = (sprite != null);
            }

            // NameText
            var nameText = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            //if (nameText) nameText.text = o.displayName;
            if (nameText) {
                string name = LocalizeIfReady(o.displayName);
                nameText.text = name;
            }

            // 가격(첫 가격만 표기, 멀티 통화면 원하는 정책대로)
            var priceText = go.transform.Find("PricePanel/PriceText")?.GetComponent<TextMeshProUGUI>();
            var kindImage = go.transform.Find("PricePanel/KindImagePanel")?.GetComponent<Image>();

            var price = o.prices.FirstOrDefault();
            if (priceText) priceText.text = price.amount.ToString("N0");
            if (kindImage) {
                var icon = Resources.Load<Sprite>($"Image/{price.currency.ToLower()}"); // gold|star
                kindImage.sprite = icon;
                kindImage.enabled = (icon != null);
            }

            // === ★ 보유 여부 계산 (PDL에 offer.items가 없으므로 클라에서 추론) ===
            // items 테이블에서 imageKey && category가 같은 첫 sku를 찾는다.
            string grantSku = ResolveSkuForOffer(o);
            bool alreadyOwned = !string.IsNullOrEmpty(grantSku) && ShopCache.Owned.Contains(grantSku);

            // AlreadyText 활성화
            var alreadyGo = go.transform.Find("AlreadyText")?.gameObject;
            if (alreadyGo) alreadyGo.SetActive(alreadyOwned);

            if (alreadyOwned && alreadyGo) {
                string owntxt = LocalizeIfReady("보유");
                alreadyGo.GetComponentInChildren<TextMeshProUGUI>().text = owntxt;
            }

            // 썸네일 알파 20%
            if (imgMain) {
                var c = imgMain.color;
                c.a = alreadyOwned ? 0.05f : 1f;
                imgMain.color = c;

                var d = kindImage.color;
                d.a = alreadyOwned ? 0.05f : 1f;
                kindImage.color = d;

                var e = priceText.color;
                e.a = alreadyOwned ? 0.05f : 1f;
                priceText.color = e;

                // ★ 썸네일 머터리얼 교체
                if (alreadyOwned) {
                    if (ownedThumbnailMat) go.GetComponent<Image>().material = ownedThumbnailMat;
                } else {
                    // 기본 머터리얼로 복원(있으면)
                    if (defaultThumbnailMat) go.GetComponent<Image>().material = defaultThumbnailMat;
                    else go.GetComponent<Image>().material = null; // Image의 기본 머터리얼로
                }
            }


            // 버튼 비활성화
            var btn = go.GetComponentInChildren<Button>();
            if (btn) {
                btn.interactable = !alreadyOwned;

                // ★ 구매 바인딩(보유가 아닐 때만)
                if (!alreadyOwned) {
                    string offerId = o.offerId;
                    var capturedOffer = o; // 클로저용

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        SoundManager.I?.Play2D(SfxId.MouseClick);
                        OpenBuyPanel(capturedOffer);
                    });
                }
            }
        }
    }

    private static string ResolveSkuForOffer(ShopCache.StoreOfferVo o) {
        foreach (var kv in ShopCache.Items) {
            var im = kv.Value;
            if (!string.IsNullOrEmpty(im.imageKey) &&
                im.imageKey == o.imageKey &&
                string.Equals(im.category ?? "", o.category ?? "", System.StringComparison.OrdinalIgnoreCase)) {
                return kv.Key; // sku
            }
        }
        return null;
    }

    private Transform PanelOf(string cat) {
        switch ((cat ?? "misc").ToLower()) {
            case "character": return panelCharacter;
            case "trail": return panelTrail;
            case "dance": return panelDance;
            default: return panelMisc;
        }
    }

    private void Clear(Transform t) {
        if (!t) return;
        for (int i = t.childCount - 1; i >= 0; --i) Destroy(t.GetChild(i).gameObject);
    }

    void ShowPreviewCharacter(string sku) {
        if (!previewRoot || !previewAnchor)
            return;

        // 기존 인스턴스 제거
        if (_previewInstance != null) {
            Destroy(_previewInstance);
            _previewInstance = null;
        }

        var prefab = Resources.Load<GameObject>($"Characters/{sku}");
        if (!prefab) {
            HidePreview();          // 프리팹 없으면 그냥 프리뷰 끔
            return;
        }

        _previewInstance = Instantiate(prefab, previewAnchor);
        _previewInstance.transform.localPosition = Vector3.zero;
        _previewInstance.transform.localRotation = Quaternion.Euler(0, 180f, 0);

        if (previewRoot)
            previewRoot.gameObject.SetActive(true);

        if (Panel_Preview != null)
            Panel_Preview.SetActive(true);    // ★ 캐릭터일 때만 켜짐
    }

    void HidePreview() {
        if (_previewInstance != null) {
            Destroy(_previewInstance);
            _previewInstance = null;
        }
        if (previewRoot)
            previewRoot.gameObject.SetActive(false);

        if (Panel_Preview != null)
            Panel_Preview.SetActive(false);   // ★ 무조건 끔
    }
}
