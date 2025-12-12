using System;
using System.Collections.Generic;

namespace Server.Infra {
    public class ItemDef {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "cosmetic";
        public bool Stackable { get; set; } = false;
        public string? EquipSlot { get; set; }
        public bool DefaultOwned { get; set; } = false;
        public string? Rarity { get; set; }
        public string Category { get; set; } = "misc";
        public string ImageKey { get; set; } = "";
    }

    // 가격 한 줄
    public class OfferPrice {
        public string Currency { get; set; } = "GOLD";
        public long Amount { get; set; }
    }

    // 지급 품목 한 줄
    public class OfferGrant {
        public string Sku { get; set; } = string.Empty;
        public int Qty { get; set; } = 1;
    }

    // 실제 판매 단위(묶음)
    public class Offer {
        public string OfferId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "";   // UI 표기명
        public string ImageKey { get; set; } = "";      // 메인 썸네일 (기본은 첫 지급 아이템 ImageKey)
        public string Category { get; set; } = "misc";  // 탭 분류
        public List<OfferGrant> Items { get; set; } = new();
        public List<OfferPrice> Prices { get; set; } = new();
        public DateTime StartUtc { get; set; } = DateTime.UnixEpoch;
        public DateTime? EndUtc { get; set; }
        public int LimitPerAccount { get; set; } = 0;
        public bool Visible { get; set; } = true;
        public List<string> Tags { get; set; } = new();
    }

    // 카탈로그 루트 (메모리 캐시용)
    public sealed class StoreCatalog {
        public int Version { get; init; }
        public IReadOnlyDictionary<string, Offer> Offers { get; init; } = new Dictionary<string, Offer>();
        public IReadOnlyDictionary<string, ItemDef> Items { get; init; } = new Dictionary<string, ItemDef>();
        public string? Checksum { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
