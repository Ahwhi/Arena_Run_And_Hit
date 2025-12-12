using System.IO;
using System.Text.Json;

namespace Server.Infra {

    public class SteamConfig {
        public string PublisherKey { get; set; } = "";
        public uint AppId { get; set; } = 0;
        public string Identity { get; set; } = "MyGameLogin";
    }

    public class ServerConfig {
        public string BindIp { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 7777;
        public int MetricsPort { get; set; } = 9100;
        public bool PrometheusEnabled { get; set; } = false;

        public SteamConfig Steam { get; set; } = new SteamConfig();
    }

    public static class LegalConfig {
        // 버전 올리면 모든 유저가 다시 동의해야 함
        public const int CURRENT_POLICY_VERSION = 1;
    }

    public static class Config {
        public static ServerConfig Load(string path = "Data/appsettings.json") {
            try {
                if (File.Exists(path)) {
                    var json = File.ReadAllText(path);
                    var cfg = JsonSerializer.Deserialize<ServerConfig>(json);
                    if (cfg != null) {
                        // Steam 섹션 누락 대비
                        cfg.Steam ??= new SteamConfig();
                        return cfg;
                    }
                } else {
                    Log.Error($"[Failed to load file] {path}");
                    return null;
                }
            } catch {
                /* ignore */
            }
            return new ServerConfig();
        }
    }
}
