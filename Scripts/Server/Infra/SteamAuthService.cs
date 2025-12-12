using System.Net.Http;
using System.Text.Json;

namespace Server.Infra {
    public static class SteamAuthService {
        private static readonly HttpClient _http = new HttpClient() {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static string _publisherKey = "";
        private static uint _appId = 0;
        private static string _identity = "MyGameLogin";
        private static bool _initialized = false;

        public static void Initialize(SteamConfig cfg) {
            if (cfg == null) return;

            _publisherKey = cfg.PublisherKey ?? "";
            _appId = cfg.AppId;
            _identity = string.IsNullOrWhiteSpace(cfg.Identity) ? "MyGameLogin" : cfg.Identity;

            _initialized = true;
        }

        public static async Task<(ulong? steamId, string? err)> AuthenticateTicketAsync(string ticketHex) {
            if (!_initialized || string.IsNullOrWhiteSpace(_publisherKey) || _appId == 0) {
                return (null, "SteamAuthService not initialized. Check appsettings.json Steam section.");
            }

            var url =
                "https://partner.steam-api.com/ISteamUserAuth/AuthenticateUserTicket/v1/" +
                $"?key={_publisherKey}&appid={_appId}&ticket={ticketHex}&identity={_identity}";

            using var res = await _http.GetAsync(url);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (null, $"HTTP {(int)res.StatusCode}: {json}");

            using var doc = JsonDocument.Parse(json);
            var resp = doc.RootElement.GetProperty("response");

            if (resp.TryGetProperty("error", out var errObj)) {
                int code = errObj.GetProperty("errorcode").GetInt32();
                string desc = errObj.GetProperty("errordesc").GetString();
                return (null, $"SteamError {code}: {desc}");
            }

            if (!resp.TryGetProperty("params", out var p) ||
                !p.TryGetProperty("steamid", out var sidElem))
                return (null, $"No params in response: {json}");

            return (ulong.Parse(sidElem.GetString()), null);
        }
    }
}
