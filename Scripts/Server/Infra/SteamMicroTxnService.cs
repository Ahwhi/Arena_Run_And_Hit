using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace Server.Infra {
    public static class SteamMicroTxnService {
        static readonly HttpClient _http = new HttpClient() {
            Timeout = TimeSpan.FromSeconds(5)
        };

        // 1) 유저 통화 얻기 (권장)
        public static async Task<string> GetUserCurrencyAsync(string publisherKey, uint appId, ulong steamId) {
            // GET https://partner.steam-api.com/ISteamMicroTxn/GetUserInfo/v2/
            var url = "https://partner.steam-api.com/ISteamMicroTxn/GetUserInfo/v2/";
            var qs = $"?key={publisherKey}&appid={appId}&steamid={steamId}";
            var res = await _http.GetStringAsync(url + qs);

            using var doc = JsonDocument.Parse(res);
            var currency = doc.RootElement
                .GetProperty("response")
                .GetProperty("params")
                .GetProperty("currency")
                .GetString();

            return string.IsNullOrWhiteSpace(currency) ? "USD" : currency!;
        }

        // 2) InitTxn
        public static async Task<(bool ok, ulong orderId, ulong transId, string err)> InitStarsTxnAsync(
    string publisherKey, uint appId, ulong steamId,
    int packIndex, string language = "ko"
) {
            if (packIndex < 0 || packIndex >= LevelData.Packs.Length)
                return (false, 0, 0, "invalid packIndex");

            var pack = LevelData.Packs[packIndex];

            long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int orderId32 = (int)(ms % int.MaxValue);
            ulong orderId = (ulong)orderId32;

            string currency = "USD";
            // currency = await GetUserCurrencyAsync(publisherKey, appId, steamId);

            var form = new Dictionary<string, string> {
                ["key"] = publisherKey,
                ["orderid"] = orderId32.ToString(),
                ["steamid"] = steamId.ToString(),
                ["appid"] = appId.ToString(),
                ["itemcount"] = "1",
                ["language"] = language,
                ["currency"] = currency,
                ["usersession"] = "client",
                ["itemid[0]"] = pack.itemId.ToString(),
                ["qty[0]"] = "1",
                ["amount[0]"] = pack.cents.ToString(),
                ["description[0]"] = pack.desc
            };

            var res = await _http.PostAsync(
                "https://partner.steam-api.com/ISteamMicroTxn/InitTxn/v3/",
                new FormUrlEncodedContent(form)
            );

            var json = await res.Content.ReadAsStringAsync();
            //Console.WriteLine("[InitTxn] raw = " + (json.Length > 200 ? json.Substring(0, 200) + "..." : json));

            try {
                using var doc = JsonDocument.Parse(json);

                var root = doc.RootElement.GetProperty("response");
                var result = root.GetProperty("result").GetString();

                if (!string.Equals(result, "OK", StringComparison.OrdinalIgnoreCase)) {
                    string err = root.TryGetProperty("error", out var e)
                                 && e.TryGetProperty("errordesc", out var d)
                                 ? d.GetString() ?? "InitTxn failed"
                                 : "InitTxn failed";

                    return (false, orderId, 0, err);
                }

                var paramsEl = root.GetProperty("params");
                ulong transId = ulong.Parse(paramsEl.GetProperty("transid").GetString()!);
                return (true, orderId, transId, "");
            } catch (JsonException ex) {
                Log.Error("[InitTxn] JSON parse failed: " + ex.Message);
                return (false, orderId, 0, "JsonParse: " + ex.Message);
            }
        }


        public sealed class QueryResult {
            public bool ok;
            public bool isApproved;
            public ulong orderId;
            public ulong transId;
            public int packIndex;
            public string currency = "USD";
            public int amountCents;
            public string error = "";
        }

        public static async Task<QueryResult> QueryTxnAsync(
    string publisherKey, uint appId,
    ulong orderId, ulong transId
) {
            var url = "https://partner.steam-api.com/ISteamMicroTxn/QueryTxn/v3/";
            var qs = $"?key={publisherKey}&appid={appId}&orderid={orderId}&transid={transId}";

            try {
                var res = await _http.GetAsync(url + qs);
                var json = await res.Content.ReadAsStringAsync();

                Log.Pay($"[QueryTxn] status={(int)res.StatusCode} {res.StatusCode}");
                Log.Pay($"[QueryTxn] raw = {json}");

                var qr = new QueryResult {
                    ok = false,
                    isApproved = false,
                    orderId = orderId,
                    transId = transId,
                    currency = "USD",
                    amountCents = 0,
                    packIndex = -1,
                    error = ""
                };

                if (!res.IsSuccessStatusCode) {
                    qr.error = $"HTTP {(int)res.StatusCode}: {res.StatusCode}";
                    return qr;
                }

                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("response", out var root)) {
                    qr.error = "no 'response' in json";
                    return qr;
                }

                string result = root.GetProperty("result").GetString() ?? "";
                if (!string.Equals(result, "OK", StringComparison.OrdinalIgnoreCase)) {
                    qr.ok = false;

                    if (root.TryGetProperty("error", out var errEl) &&
                        errEl.TryGetProperty("errordesc", out var descEl)) {
                        qr.error = descEl.GetString() ?? "QueryTxn Failure";
                    } else {
                        qr.error = "QueryTxn Failure (result != OK)";
                    }

                    return qr;
                }

                qr.ok = true;

                if (!root.TryGetProperty("params", out var @params)) {
                    qr.ok = false;
                    qr.error = "no 'params' in response";
                    return qr;
                }

                string status = @params.GetProperty("status").GetString() ?? "";
                qr.isApproved =
                    status.Equals("Authorized", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Settled", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Completed", StringComparison.OrdinalIgnoreCase);

                if (@params.TryGetProperty("currency", out var curEl)) {
                    qr.currency = curEl.GetString() ?? "USD";
                }

                if (@params.TryGetProperty("items", out var itemsEl) &&
                    itemsEl.ValueKind == JsonValueKind.Array &&
                    itemsEl.GetArrayLength() > 0) {

                    var it0 = itemsEl[0];

                    if (it0.TryGetProperty("amount", out var amtEl)) {
                        if (amtEl.ValueKind == JsonValueKind.Number) {
                            qr.amountCents = amtEl.GetInt32(); 
                        } else {
                            var amtStr = amtEl.GetString() ?? "0";
                            if (!int.TryParse(amtStr, out qr.amountCents))
                                qr.amountCents = 0;
                        }
                    }

                    if (it0.TryGetProperty("itemid", out var idEl)) {
                        uint itemId = 0;

                        if (idEl.ValueKind == JsonValueKind.Number) {
                            itemId = idEl.GetUInt32();
                        } else {
                            var idStr = idEl.GetString();
                            if (!uint.TryParse(idStr, out itemId))
                                itemId = 0;
                        }

                        if (itemId != 0) {
                            for (int i = 0; i < LevelData.Packs.Length; i++) {
                                if (LevelData.Packs[i].itemId == itemId) {
                                    qr.packIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }

                return qr;
            } catch (Exception ex) {
                Log.Error("[QueryTxn] EX: " + ex.Message);
                return new QueryResult {
                    ok = false,
                    isApproved = false,
                    orderId = orderId,
                    transId = transId,
                    currency = "USD",
                    amountCents = 0,
                    packIndex = -1,
                    error = "Exception: " + ex.Message
                };
            }
        }

        public static async Task<string> GetReportRawAsync(
    string publisherKey,
    uint appId,
    string type = "GAMESALES",
    int daysAgo = 10,
    int maxResults = 1000
) {
            //var sinceUtc = DateTime.UtcNow.Date.AddDays(-daysAgo);
            var sinceUtc = DateTime.UtcNow.AddHours(-1); // 최근 1시간
            string timeStr = sinceUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

            var baseUrl = "https://partner.steam-api.com/ISteamMicroTxn/GetReport/v5/";
            var qs = $"?key={publisherKey}" +
                     $"&appid={appId}" +
                     $"&time={Uri.EscapeDataString(timeStr)}" +
                     $"&type={type}" +
                     $"&maxresults={maxResults}";

            var res = await _http.GetAsync(baseUrl + qs);
            var body = await res.Content.ReadAsStringAsync();

            //var preview = body.Length > 300 ? body[..300] + "..." : body;

            //Log.Pay($"[GetReport] status={(int)res.StatusCode} {res.StatusCode} type={type} daysAgo={daysAgo} len={body.Length}");
            //Log.Pay($"[GetReport] preview = {preview}");

            return body;
        }

        public static async Task TestGetReportOnceAsync() {
            try {
                var cfg = Config.Load();
                const string reportType = "GAMESALES";
                int daysAgo = 2;

                var rawJson = await SteamMicroTxnService.GetReportRawAsync(
                    cfg.Steam.PublisherKey,
                    (uint)cfg.Steam.AppId,
                    type: reportType,
                    daysAgo: daysAgo
                );

                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement.GetProperty("response");

                string result = root.GetProperty("result").GetString() ?? "UNKNOWN";
                int count = 0;

                if (root.TryGetProperty("params", out var p) &&
                    p.TryGetProperty("count", out var c)) {
                    count = c.GetInt32();
                }

                Log.Sys($"[GetReportOnce] result={result} count={count} type={reportType} daysAgo={daysAgo}");

                var prettyJson = JsonSerializer.Serialize(
                    doc.RootElement,
                    new JsonSerializerOptions {
                        WriteIndented = true
                    }
                );

                // 파일로 저장 (예: ./Logs/SteamReport/steam_report_SETTLEMENT_20251128_235543.json)
                string dir = Path.Combine(AppContext.BaseDirectory, "Logs", "SteamReport");
                Directory.CreateDirectory(dir);

                string fileName = $"steam_report_{reportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                string fullPath = Path.Combine(dir, fileName);

                File.WriteAllText(fullPath, prettyJson);

                //Log.Sys($"[GetReportOnce] saved to {fullPath}");
            } catch (Exception ex) {
                //Log.Sys($"[GetReportOnce] EX: {ex.Message}");
            }
        }



        public static int GetStarsOfPack(int packIndex)
            => (packIndex >= 0 && packIndex < LevelData.Packs.Length) ? LevelData.Packs[packIndex].stars : 0;
    }
}
