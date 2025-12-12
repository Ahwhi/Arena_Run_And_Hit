using Microsoft.EntityFrameworkCore;
using Server.DB;
using static Server.DB.DataModel;

namespace Server.Infra {
    public static class BattlePassService {

        public static BattlePassState GetOrCreateState(AppDBContext db, Account acc) {
            var cfg = BattlePass.Config;
            if (cfg == null)
                throw new InvalidOperationException("BattlePass config is null");

            var state = db.Set<BattlePassState>()
                .FirstOrDefault(x => x.AccountId == acc.Id && x.version == cfg.Version);

            if (state == null) {
                state = new BattlePassState {
                    AccountId = acc.Id,
                    version = cfg.Version,
                    level = 1,
                    exp = 0,
                    hasPremium = false,
                    freeClaimBits = 0,
                    premiumClaimBits = 0,
                    updatedAt = DateTime.UtcNow
                };
                db.Set<BattlePassState>().Add(state);
                db.SaveChanges();
            }

            return state;
        }

        public static void OnAccountLevelUp(AppDBContext db, Account acc, int fromLevel, int toLevel) {
            if (toLevel <= fromLevel) return;
            var cfg = BattlePass.Config;
            if (cfg == null) return;

            var state = GetOrCreateState(db, acc);
            int delta = toLevel - fromLevel;
            int newLevel = state.level + delta;
            if (newLevel > cfg.MaxLevel)
                newLevel = cfg.MaxLevel;

            state.level = newLevel;
            state.updatedAt = DateTime.UtcNow;
            db.SaveChanges();

            Log.Info($"[BattlePass] {acc.nickName} BP level {state.level} (acc {fromLevel}->{toLevel})");
        }

        public static BattlePassRewardDef? GetReward(int level, bool isPremium) {
            var lvl = BattlePass.GetLevel(level);
            if (lvl == null) return null;
            return isPremium ? lvl.Premium : lvl.Free;
        }

        private static void GrantCurrency(AppDBContext db, Account acc, string currency, int amount, string refId) {
            if (amount == 0) return;

            currency = (currency ?? "").ToUpperInvariant();
            if (currency == "GOLD") acc.gold += amount;
            else if (currency == "STAR") acc.star += amount;
            else return;

            var ledger = new CurrencyLedger {
                AccountId = acc.Id,
                currency = currency,
                delta = amount,
                balanceAfter = (currency == "GOLD") ? acc.gold : acc.star,
                reason = "GRANT",
                refType = "BATTLEPASS_REWARD",
                refId = refId,
                createdAt = DateTime.UtcNow
            };
            db.Set<CurrencyLedger>().Add(ledger);
        }

        public static void GrantLevelReward(AppDBContext db, Account acc, BattlePassState state, int level, bool isPremium) {
            var reward = GetReward(level, isPremium);
            if (reward == null) return;

            string refId = $"BP{state.version}_L{level}_{(isPremium ? "P" : "F")}";

            GrantCurrency(db, acc, "GOLD", reward.Gold, refId);
            GrantCurrency(db, acc, "STAR", reward.Star, refId);

            var items = db.Set<AccountItem>()
                .Where(i => i.AccountId == acc.Id)
                .ToList();

            foreach (var sku in reward.ItemSkus ?? Enumerable.Empty<string>()) {
                if (string.IsNullOrWhiteSpace(sku)) continue;

                var own = items.FirstOrDefault(i => i.sku == sku);
                if (own == null) {
                    own = new AccountItem {
                        AccountId = acc.Id,
                        sku = sku,
                        quantity = 1,
                        createdAt = DateTime.UtcNow
                    };
                    db.Set<AccountItem>().Add(own);
                    items.Add(own);
                } else {
                    own.quantity += 1;
                }
            }

            ulong mask = 1UL << (level - 1);
            if (isPremium) state.premiumClaimBits |= mask;
            else state.freeClaimBits |= mask;

            state.updatedAt = DateTime.UtcNow;
        }

        public static bool CanClaimLevel(BattlePassState state, int level, bool isPremium) {
            if (level <= 0) return false;
            if (state.level < level) return false;

            ulong mask = 1UL << (level - 1);
            if (isPremium) {
                if (!state.hasPremium) return false;
                return (state.premiumClaimBits & mask) == 0;
            } else {
                return (state.freeClaimBits & mask) == 0;
            }
        }

        public static (bool ok, int failReason) BuyPremium(AppDBContext db, Account acc, BattlePassState state) {
            var cfg = BattlePass.Config;
            if (cfg == null) return (false, 1);
            if (state.hasPremium) return (true, 0);

            state.hasPremium = true;
            state.updatedAt = DateTime.UtcNow;

            return (true, 0);
        }
    }
}
