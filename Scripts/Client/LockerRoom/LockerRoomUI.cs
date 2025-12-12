using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LockerRoomUI : MonoBehaviour {
    [Header("Prefab & Parents")]
    public GameObject ItemPrefab;
    public Transform CharacterPanel; // 캐릭터용 Grid/Content
    public Transform TrailPanel;     // 트레일용 Grid/Content
    public Transform DancePanel;     // 댄스용 Grid/Content
    public TextMeshProUGUI gold;
    public TextMeshProUGUI star;

    // === Dance header & slots ===
    [Header("Dance Header (Keys)")]
    public TextMeshProUGUI Key1TMP;
    public TextMeshProUGUI Key2TMP;
    public TextMeshProUGUI Key3TMP;
    public TextMeshProUGUI Key4TMP;

    [Header("Dance Slots (Drop targets)")]
    public Transform Equip1Panel;
    public Transform Equip2Panel;
    public Transform Equip3Panel;
    public Transform Equip4Panel;

    static readonly Color EQUIP_GREEN = new Color(0.35f, 1f, 0.35f, 0.9f);
    static readonly Color EQUIP_OFF = new Color(0f, 0f, 0f, 0f);

    // =============== PUBLIC ===============
    public void RefreshFromCache() {
        gold.text = ShopCache.Gold.ToString();
        star.text = ShopCache.Star.ToString();

        if (ItemPrefab == null) return;

        Clear(CharacterPanel);
        Clear(TrailPanel);
        Clear(DancePanel);

        RefreshDanceHeader();   // 키 레이블 업데이트 (Key1TMP~Key4TMP)
        RefreshDanceSlots();    // 슬롯(Equip1~4)에 현재 장착 표시

        // 소유한 sku만 정렬해서 뿌린다 (character/trail/dance)
        foreach (var sku in ShopCache.Owned.OrderBy(s => s)) {
            if (!ShopCache.Items.TryGetValue(sku, out var meta)) continue;
            var cat = (meta.category ?? "misc").ToLower();
            Transform parent = null;
            switch (cat) {
                case "character": parent = CharacterPanel; break;
                case "trail": parent = TrailPanel; break;
                case "dance": parent = DancePanel; break;
                default: continue; // etc/misc는 락커에 표시 안함
            }

            var go = Instantiate(ItemPrefab, parent, false);
            var t = go.transform;

            // 썸네일
            var imgPanel = t.Find("ImagePanel")?.GetComponent<Image>();
            if (imgPanel != null) {
                var sprite = Resources.Load<Sprite>($"Image/{meta.imageKey}");
                if (sprite == null)
                    sprite = Resources.Load<Sprite>("Image/DefaultImage");
                imgPanel.sprite = sprite;
                imgPanel.enabled = (sprite != null);
            }

            // 이름
            var nameText = t.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            //if (nameText) nameText.text = string.IsNullOrWhiteSpace(meta.name) ? sku : meta.name;
            if (nameText) {
                string raw = string.IsNullOrWhiteSpace(meta.name) ? sku : meta.name;

                // ★ 여기서 바로 현재 로케일 기준으로 변환
                if (AutoLocalizeByTextMatch.Instance != null) {
                    raw = AutoLocalizeByTextMatch.Instance.LocalizeImmediate(raw);
                }

                nameText.text = raw;
            }

            // 장착 마크(댄스는 슬롯 방식이라 리스트에서는 마크 끔)
            var equipMark = t.Find("EquipMarkPanel")?.GetComponent<Image>();
            if (equipMark) {
                bool equipped = ShopCache.IsEquipped(sku); // 캐릭/트레일은 기존 규칙 유지
                //if (cat == "dance") equipped = false;
                equipMark.color = equipped ? EQUIP_GREEN : EQUIP_OFF;
            }

            // === 상호작용 ===
            // 1) 캐릭/트레일: 기존처럼 클릭 장착
            // 2) 댄스: 드래그&드롭. 클릭하면 "첫 빈 슬롯"에 꽂기 (편의)
            var btn = go.GetComponentInChildren<Button>();
            string skuLocal = sku;
            if (btn != null) {
                if (cat == "dance") {
                    //btn.onClick.AddListener(() => QuickEquipDanceToFirstEmpty(skuLocal));
                } else {
                    string slot = cat; // character / trail
                    btn.onClick.AddListener(() => {
                        SoundManager.I?.Play2D(SfxId.MouseClick);
                        // 미장착템만 장착 패킷 발송 가능
                        if (!ShopCache.IsEquipped(sku)) {
                            var req = new C_EquipItem { slot = slot, sku = skuLocal };
                            NetworkManager.Send(req.Write());
                        }
                    });
                }
            }

            // 3) 드래그 소스(댄스만)
            if (cat == "dance") {
                var drag = go.GetComponent<UIDragDanceItem>();
                if (!drag) drag = go.AddComponent<UIDragDanceItem>();
                drag.Init(this, skuLocal);
            }
        }

        // 드롭 타깃 보장(씬에 스크립트 안붙어있으면 자동 부착)
        EnsureDropTarget(Equip1Panel, 0);
        EnsureDropTarget(Equip2Panel, 1);
        EnsureDropTarget(Equip3Panel, 2);
        EnsureDropTarget(Equip4Panel, 3);
    }

    // 서버에서 S_EquipResult 후 S_StoreCatalog 재수신 안 하더라도
    // 간단히 색/슬롯만 즉시 갱신하고 싶다면 이 public 메서드 호출
    public void MarkEquippedInstant(string slot, string sku) {
        ShopCache.EquippedBySlot[slot] = sku;
        RefreshFromCache();
    }

    // 드래그 드롭 → 슬롯 인덱스로 장착
    public void OnDropDanceToSlot(int targetIndex, string sku, int fromIndex = -1) {
        string targetSlot = SlotNameForIndex(targetIndex);
        // 1) 서버: 타겟 슬롯 장착
        NetworkManager.Send(new C_EquipItem { slot = targetSlot, sku = sku }.Write());
        ShopCache.EquippedBySlot[targetSlot] = sku;

        // ★ 로비 플레이어에 현재 슬롯의 sku 기록 (targetIndex는 0~3 이라고 가정)
        LobbyPlayer.SetDanceSku(targetIndex, sku);

        // 2) 만약 다른 슬롯에서 왔다면 원본 슬롯 해제
        if (fromIndex >= 0 && fromIndex != targetIndex) {
            string fromSlot = SlotNameForIndex(fromIndex);
            // 서버: 해제(아래 2-서버 수정 참고) 
            NetworkManager.Send(new C_EquipItem { slot = fromSlot, sku = "" }.Write());
            ShopCache.EquippedBySlot[fromSlot] = null;

            // 원래 슬롯에는 더 이상 댄스 없음
            LobbyPlayer.SetDanceSku(fromIndex, null);
        }

        RefreshDanceSlots();
    }

    public void OnUnequipDanceSlot(int slotIndex) {
        string slot = SlotNameForIndex(slotIndex);
        NetworkManager.Send(new C_EquipItem { slot = slot, sku = "" }.Write()); // ★ 서버 해제 지원 필수
        ShopCache.EquippedBySlot[slot] = null;

        // ★ 로비 플레이어 쪽도 비워주기
        LobbyPlayer.SetDanceSku(slotIndex, null);

        RefreshDanceSlots();
    }

    // =============== PRIVATE ===============
    void Clear(Transform p) {
        if (!p) return;
        for (int i = p.childCount - 1; i >= 0; --i)
            Destroy(p.GetChild(i).gameObject);
    }

    // 상단 키라벨 업데이트
    void RefreshDanceHeader() {
        var kb = SettingsApplier.Current?.keys ?? new KeyBindings();
        if (Key1TMP) Key1TMP.text = ToReadable(kb.dance1);
        if (Key2TMP) Key2TMP.text = ToReadable(kb.dance2);
        if (Key3TMP) Key3TMP.text = ToReadable(kb.dance3);
        if (Key4TMP) Key4TMP.text = ToReadable(kb.dance4);
    }

    string ToReadable(KeyCode k) => (k == KeyCode.None) ? "None" : k.ToString();

    // 슬롯 내 프리팹 재구성
    void RefreshDanceSlots() {
        // 모두 비우고
        Clear(Equip1Panel); Clear(Equip2Panel); Clear(Equip3Panel); Clear(Equip4Panel);

        var sku0 = GetEquippedDanceSku(0);
        var sku1 = GetEquippedDanceSku(1);
        var sku2 = GetEquippedDanceSku(2);
        var sku3 = GetEquippedDanceSku(3);

        // dance1~dance4 읽기
        TrySpawnDanceInSlot(Equip1Panel, sku0, 0);
        TrySpawnDanceInSlot(Equip2Panel, sku1, 1);
        TrySpawnDanceInSlot(Equip3Panel, sku2, 2);
        TrySpawnDanceInSlot(Equip4Panel, sku3, 3);

        // ★ 여기서 LobbyPlayer 쪽도 항상 최신 상태로 맞춰주기
        LobbyPlayer.SetDanceSku(0, sku0);
        LobbyPlayer.SetDanceSku(1, sku1);
        LobbyPlayer.SetDanceSku(2, sku2);
        LobbyPlayer.SetDanceSku(3, sku3);
    }

    string GetEquippedDanceSku(int idx) {
        string slot = SlotNameForIndex(idx);
        if (ShopCache.EquippedBySlot.TryGetValue(slot, out var sku) && !string.IsNullOrEmpty(sku))
            return sku;
        return null;
    }

    string SlotNameForIndex(int idx) {
        return idx switch {
            0 => "dance1",
            1 => "dance2",
            2 => "dance3",
            3 => "dance4",
            _ => "dance1"
        };
    }

    void TrySpawnDanceInSlot(Transform slot, string sku, int slotIndex = -1) {
        if (!slot || string.IsNullOrEmpty(sku)) return;

        // 슬롯 비우고
        Clear(slot);

        // ★ 위 리스트에서 쓰는 ItemPrefab을 '그대로' 슬롯에 한 번 더 만든다
        var go = Instantiate(ItemPrefab, slot, false);
        

        // 메타로 텍스트/이미지 채우기 (위 리스트와 동일)
        if (ShopCache.Items.TryGetValue(sku, out var meta)) {
            var nameText = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            //if (nameText) nameText.text = string.IsNullOrWhiteSpace(meta.name) ? sku : meta.name;
            if (nameText) {
                string raw = string.IsNullOrWhiteSpace(meta.name) ? sku : meta.name;
                if (AutoLocalizeByTextMatch.Instance != null) {
                    raw = AutoLocalizeByTextMatch.Instance.LocalizeImmediate(raw);
                }
                nameText.text = raw;
            }

            var img = go.transform.Find("ImagePanel")?.GetComponent<Image>();
            if (img) {
                var sprite = Resources.Load<Sprite>($"Image/{meta.imageKey}")
                             ?? Resources.Load<Sprite>("Image/DefaultImage");
                img.sprite = sprite;
                img.enabled = (sprite != null);
            }
        }

        var markTr = go.transform.Find("EquipMarkPanel");
        if (markTr) {
            var markImg = markTr.GetComponent<Image>(); if (markImg) markImg.enabled = false;
        }

        // 슬롯 안 카드도 드래그 소스가 되게 설정 (★ 중요)
        var drag = go.GetComponent<UIDragDanceItem>();
        if (!drag) drag = go.AddComponent<UIDragDanceItem>();
        drag.Init(this, sku, slotIndex); // slotIndex 넘겨줘서 "슬롯에서 드래그"임을 표시

        // 슬롯 안의 버튼은 눌림 금지
        var btn = go.GetComponentInChildren<Button>();
        if (btn) btn.interactable = false;

        // 레이아웃 꽉 채우기(슬롯 패널이 Stretch일 때)
        if (go.transform is RectTransform rt) {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one; rt.localPosition = Vector3.zero;
        }
    }

    void EnsureDropTarget(Transform panel, int idx) {
        if (!panel) return;
        var drop = panel.GetComponent<UIDropDanceSlot>();
        if (!drop) drop = panel.gameObject.AddComponent<UIDropDanceSlot>();
        drop.Bind(this, idx);
    }
}
