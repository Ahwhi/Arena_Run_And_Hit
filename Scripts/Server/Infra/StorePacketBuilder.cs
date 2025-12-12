using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.DB;

namespace Server.Infra
{
    internal static class StorePacketBuilder {
        public static S_StoreCatalog BuildFor(ClientSession s) {
            var cat = Store.Catalog;
            var pkt = new S_StoreCatalog { version = cat.Version };

            using (var db = new AppDBContext()) {
                var account = db.Accounts
                    .FirstOrDefault(a => a.accountId == s.Account.accountId);

                if (account != null) {
                    var entry = db.Entry(account);
                    entry.Collection(a => a.Items).Load();
                    entry.Collection(a => a.Equipped).Load();
                }

                pkt.gold = account != null ? (int)Math.Clamp(account.gold, int.MinValue, (long)int.MaxValue) : 0;
                pkt.star = account != null ? (int)Math.Clamp(account.star, int.MinValue, (long)int.MaxValue) : 0;


                if (account != null) {
                    foreach (var it in account.Items) {
                        pkt.inventorys.Add(new S_StoreCatalog.Inventory { sku = it.sku, qty = it.quantity });
                    }
                    foreach (var eq in account.Equipped) {
                        pkt.equippeds.Add(new S_StoreCatalog.Equipped { slot = eq.slot, sku = eq.sku });
                        s.Game.EquippedBySlot[eq.slot] = eq.sku;
                    }
                }
            }

            foreach (var kv in cat.Items) {
                var idef = kv.Value;
                pkt.itemss.Add(new S_StoreCatalog.Items {
                    sku = idef.Sku,
                    name = string.IsNullOrWhiteSpace(idef.Name) ? idef.Sku : idef.Name,
                    category = string.IsNullOrWhiteSpace(idef.Category) ? "misc" : idef.Category,
                    imageKey = string.IsNullOrWhiteSpace(idef.ImageKey) ? idef.Sku : idef.ImageKey
                });
            }

            foreach (var kv in cat.Offers) {
                var of = kv.Value;
                var po = new S_StoreCatalog.Offers {
                    offerId = of.OfferId,
                    displayName = string.IsNullOrWhiteSpace(of.DisplayName) ? of.OfferId : of.DisplayName,
                    imageKey = string.IsNullOrWhiteSpace(of.ImageKey)
                        ? (of.Items.Count > 0 ? of.Items[0].Sku : of.OfferId)
                        : of.ImageKey,
                    category = string.IsNullOrWhiteSpace(of.Category) ? "misc" : of.Category,
                    visible = of.Visible
                };
                foreach (var pr in of.Prices)
                    po.pricess.Add(new S_StoreCatalog.Offers.Prices { currency = pr.Currency.ToUpperInvariant(), amount = (int)pr.Amount });
                pkt.offerss.Add(po);
            }

            return pkt;
        }
    }

}
