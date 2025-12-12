using System.Text.Json;

namespace Server.Infra {

    public class BattlePassRewardDef {
        public int Gold { get; set; } = 0;
        public int Star { get; set; } = 0;
        public List<string> ItemSkus { get; set; } = new();
    }

    public class BattlePassLevelDef {
        public int Level { get; set; }
        public BattlePassRewardDef? Free { get; set; }
        public BattlePassRewardDef? Premium { get; set; }
    }

    public class BattlePassConfig {
        public int Version { get; set; } = 1;
        public int MaxLevel { get; set; } = 50;
        public string PriceCurrency { get; set; } = "STAR"; // GOLD or STAR
        public int PriceAmount { get; set; } = 1000;
        public List<BattlePassLevelDef> Levels { get; set; } = new();
    }

    public static class BattlePass {
        public static BattlePassConfig? Config { get; private set; }

        public static void Load(string? path = null) {
            try {
                path ??= Path.Combine(AppContext.BaseDirectory, "Data", "battle_pass.json");
                if (!File.Exists(path)) {
                    Log.Warn($"[BattlePass] battle_pass.json not found. Using default config.");
                    Config = BuildDefaultConfig();
                    return;
                }

                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<BattlePassConfig>(json,
                    new JsonSerializerOptions {
                        PropertyNameCaseInsensitive = true
                    });

                if (cfg == null) {
                    Log.Error("[BattlePass] Failed to parse battle_pass.json. Using default config.");
                    Config = BuildDefaultConfig();
                    return;
                }

                cfg.Levels = cfg.Levels
                    .OrderBy(l => l.Level)
                    .ToList();

                Config = cfg;
                Log.Sys($"[BattlePass] Loaded battle_pass.json v{cfg.Version}, MaxLevel={cfg.MaxLevel}");
            } catch (Exception ex) {
                Log.Error($"[BattlePass] Load error: {ex.Message}. Using default config.");
                Config = BuildDefaultConfig();
            }
        }

        private static BattlePassConfig BuildDefaultConfig() {
            var cfg = new BattlePassConfig {
                Version = 1,
                MaxLevel = 50,
                PriceCurrency = "STAR",
                PriceAmount = 1000,
                Levels = new List<BattlePassLevelDef>()
            };

            for (int lv = 1; lv <= cfg.MaxLevel; lv++) {
                var free = new BattlePassRewardDef {
                    Gold = 100 * lv,
                    Star = 0
                };

                var premium = new BattlePassRewardDef {
                    Gold = 150 * lv,
                    Star = 10 * (lv / 5)
                };

                if (lv % 10 == 0) {
                    premium.ItemSkus.Add($"BP1_CHAR_{lv:00}");
                } else if (lv % 5 == 0) {
                    premium.ItemSkus.Add($"BP1_TRAIL_{lv:00}");
                } else if (lv % 3 == 0) {
                    premium.ItemSkus.Add($"BP1_DANCE_{lv:00}");
                }

                cfg.Levels.Add(new BattlePassLevelDef {
                    Level = lv,
                    Free = free,
                    Premium = premium
                });
            }

            Log.Info("[BattlePass] Default config built in memory.");
            return cfg;
        }

        public static BattlePassLevelDef? GetLevel(int level) {
            var cfg = Config;
            if (cfg == null) return null;
            return cfg.Levels.FirstOrDefault(l => l.Level == level);
        }
    }
}
