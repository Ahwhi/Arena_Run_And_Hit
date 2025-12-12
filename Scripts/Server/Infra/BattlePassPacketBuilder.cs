using Server.DB;
using static Server.DB.DataModel;

namespace Server.Infra {
    internal static class BattlePassPacketBuilder {

        internal static S_BattlePassInfo BuildFor(ClientSession s) {
            using var db = new AppDBContext();
            var acc = db.Accounts.FirstOrDefault(a => a.accountId == s.Account.accountId);
            if (acc == null) {
                return new S_BattlePassInfo {
                    version = 0,
                    bpLevel = 0,
                    hasPremium = false,
                    freeClaimBits = 0,
                    premiumClaimBits = 0
                };
            }

            var state = BattlePassService.GetOrCreateState(db, acc);
            return Build(acc, state);
        }

        public static S_BattlePassInfo Build(Account acc, BattlePassState state) {
            var cfg = BattlePass.Config;
            if (cfg == null) {
                return new S_BattlePassInfo {
                    version = 0,
                    bpLevel = 0,
                    hasPremium = false,
                    freeClaimBits = 0,
                    premiumClaimBits = 0
                };
            }

            var pkt = new S_BattlePassInfo {
                version = cfg.Version,
                bpLevel = state.level,
                hasPremium = state.hasPremium,
                freeClaimBits = unchecked((long)state.freeClaimBits),
                premiumClaimBits = unchecked((long)state.premiumClaimBits)
            };

            foreach (var lvl in cfg.Levels) {
                var row = new S_BattlePassInfo.Levels {
                    level = lvl.Level,
                    freeGold = lvl.Free?.Gold ?? 0,
                    freeStar = lvl.Free?.Star ?? 0,
                    premiumGold = lvl.Premium?.Gold ?? 0,
                    premiumStar = lvl.Premium?.Star ?? 0
                };

                if (lvl.Free != null) {
                    foreach (var sku in lvl.Free.ItemSkus ?? Enumerable.Empty<string>()) {
                        row.freeItemSkuss.Add(new S_BattlePassInfo.Levels.FreeItemSkus {
                            sku = sku
                        });
                    }
                }

                if (lvl.Premium != null) {
                    foreach (var sku in lvl.Premium.ItemSkus ?? Enumerable.Empty<string>()) {
                        row.premiumItemSkuss.Add(new S_BattlePassInfo.Levels.PremiumItemSkus {
                            sku = sku
                        });
                    }
                }

                pkt.levelss.Add(row);
            }

            return pkt;
        }
    }
}
