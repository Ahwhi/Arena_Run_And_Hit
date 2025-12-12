using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Server.Infra {
    internal static class StoreCatalogLoader {
        // JSON 스키마를 위한 DTO
        private class JsonRoot {
            public int version { get; set; }
            public string? updatedAt { get; set; }
            public string? checksum { get; set; }
            public List<JsonItem> items { get; set; } = new();
            public List<JsonOffer> offers { get; set; } = new();
        }

        private class JsonItem {
            public string sku { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = "cosmetic";
            public bool stackable { get; set; }
            public string? equipSlot { get; set; }
            public bool defaultOwned { get; set; }
            public string? rarity { get; set; }

            public string? category { get; set; }   // character|trail|dance|misc
            public string? imageKey { get; set; }   // Resources/Image/<imageKey>
        }

        private class JsonOffer {
            public string offerId { get; set; } = string.Empty;
            public List<JsonGrant> items { get; set; } = new();
            public List<JsonPrice> prices { get; set; } = new();
            public string startUtc { get; set; } = "1970-01-01T00:00:00Z";
            public string? endUtc { get; set; }
            public int limitPerAccount { get; set; }
            public bool visible { get; set; } = true;
            public List<string> tags { get; set; } = new();

            public string? displayName { get; set; } // UI 표기명
            public string? imageKey { get; set; }    // 썸네일 키
            public string? category { get; set; }    // 탭 분류
        }

        private class JsonGrant { public string sku { get; set; } = string.Empty; public int qty { get; set; } = 1; }
        private class JsonPrice { public string currency { get; set; } = "GOLD"; public long amount { get; set; } }

        public static StoreCatalog LoadFromJson(string path) {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Store catalog file not found: {path}");

            var json = File.ReadAllText(path);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var root = JsonSerializer.Deserialize<JsonRoot>(json, opts) ?? new JsonRoot();

            var items = new Dictionary<string, ItemDef>(StringComparer.Ordinal);
            foreach (var it in root.items) {
                if (string.IsNullOrWhiteSpace(it.sku))
                    throw new Exception("Item sku missing in catalog.");

                if (items.ContainsKey(it.sku))
                    throw new Exception($"Duplicate item sku: {it.sku}");

                var category = string.IsNullOrWhiteSpace(it.category) ? "misc" : it.category!;
                var imageKey = string.IsNullOrWhiteSpace(it.imageKey) ? it.sku : it.imageKey!;

                items[it.sku] = new ItemDef {
                    Sku = it.sku,
                    Name = it.name ?? it.sku,
                    Type = it.type ?? "cosmetic",
                    Stackable = it.stackable,
                    EquipSlot = it.equipSlot,
                    DefaultOwned = it.defaultOwned,
                    Rarity = it.rarity,
                    Category = category,
                    ImageKey = imageKey
                };
            }

            var offers = new Dictionary<string, Offer>(StringComparer.Ordinal);
            foreach (var of in root.offers) {
                if (string.IsNullOrWhiteSpace(of.offerId))
                    throw new Exception("OfferId missing in catalog.");

                if (offers.ContainsKey(of.offerId))
                    throw new Exception($"Duplicate offerId: {of.offerId}");

                DateTime start = DateTime.Parse(of.startUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                DateTime? end = string.IsNullOrWhiteSpace(of.endUtc) ? null
                                 : DateTime.Parse(of.endUtc!, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

                var grants = of.items.Select(g => new OfferGrant { Sku = g.sku, Qty = Math.Max(1, g.qty) }).ToList();

                // 참조 무결성 체크
                foreach (var g in grants)
                    if (!items.ContainsKey(g.Sku))
                        throw new Exception($"Offer '{of.offerId}' references unknown sku '{g.Sku}'.");

                var prices = of.prices.Select(p => new OfferPrice { Currency = p.currency.ToUpperInvariant(), Amount = p.amount }).ToList();
                if (prices.Count == 0) throw new Exception($"Offer '{of.offerId}' has no prices.");

                string displayName = of.displayName;
                if (string.IsNullOrWhiteSpace(displayName)) {
                    if (grants.Count > 0 && items.TryGetValue(grants[0].Sku, out var firstItem))
                        displayName = firstItem.Name;
                    else
                        displayName = of.offerId;
                }

                string imageKey = of.imageKey;
                if (string.IsNullOrWhiteSpace(imageKey)) {
                    if (grants.Count > 0 && items.TryGetValue(grants[0].Sku, out var firstItem))
                        imageKey = firstItem.ImageKey;
                    else
                        imageKey = of.offerId;
                }

                string category = of.category;
                if (string.IsNullOrWhiteSpace(category)) {
                    if (grants.Count > 0 && items.TryGetValue(grants[0].Sku, out var firstItem))
                        category = firstItem.Category;
                    else
                        category = "misc";
                }

                offers[of.offerId] = new Offer {
                    OfferId = of.offerId,
                    DisplayName = displayName,
                    ImageKey = imageKey,
                    Category = category,
                    Items = grants,
                    Prices = prices,
                    StartUtc = start,
                    EndUtc = end,
                    LimitPerAccount = of.limitPerAccount,
                    Visible = of.visible,
                    Tags = of.tags ?? new List<string>()
                };
            }

            DateTime? updatedAt = null;
            if (!string.IsNullOrWhiteSpace(root.updatedAt))
                updatedAt = DateTime.Parse(root.updatedAt!, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

            return new StoreCatalog {
                Version = root.version,
                Items = items,
                Offers = offers,
                Checksum = root.checksum,
                UpdatedAt = updatedAt
            };
        }
    }
}
