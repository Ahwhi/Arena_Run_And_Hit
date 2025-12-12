using System.Net;
using Server.Monitoring;
using ServerCore;
using Server.Infra;
using static System.Formats.Asn1.AsnWriter;
using System.Text;

namespace Server {
    class Program {
        static Listener _listener = new Listener();

        // 고정 룸
        public static readonly Room LoginRoom = new Room(RoomKind.Login);
        public static readonly Room LobbyRoom = new Room(RoomKind.Lobby);

        public static readonly Room MatchmakingSurvive = new Room(RoomKind.Matchmaking, GameMode.Survive);
        public static readonly Room MatchmakingRespawn = new Room(RoomKind.Matchmaking, GameMode.Respawn);
        public static readonly Room MatchmakingRank = new Room(RoomKind.Matchmaking, GameMode.RankSurvive);

        // 동적 룸
        public static readonly List<Room> GameRooms = new();
        public static readonly List<Room> ChatRooms = new();

        public static int totalConnect = 0;

        static readonly Room[] _fixedRooms = {
            LoginRoom, LobbyRoom, MatchmakingSurvive, MatchmakingRespawn, MatchmakingRank
        };

        static void FlushRooms() {
            foreach (var r in _fixedRooms) r.Push(() => { r.Tick(); r.Flush(); });
            foreach (var r in GameRooms) r.Push(() => { r.Tick(); r.Flush(); });
            foreach (var r in ChatRooms) r.Push(() => { r.Tick(); r.Flush(); });

            JobTimer.Instance.Push(FlushRooms, 20);
        }

        static void Main(string[] args) {
            ConsoleVT.EnableVirtualTerminal();
            ConsoleEncodingFix.ForceUtf8();

            var cfg = Config.Load();
            if (cfg == null) return;
            SteamAuthService.Initialize(cfg.Steam);
            var ipAddr = IPAddress.Parse(cfg.BindIp);
            var endPoint = new IPEndPoint(ipAddr, cfg.Port);

            _listener.Init(endPoint, () => SessionManager.Instance.Generate());

            if (cfg.PrometheusEnabled) {
                MetricsBootstrap.Start(port: cfg.MetricsPort, intervalMs: 1000);
            }

            string steamsev = "Unknown";
            if (cfg.Steam.AppId == 4196030) {
                steamsev = "Main";
            } else if (cfg.Steam.AppId == 4205120) {
                steamsev = "Test";
            }
            Log.Sys($"[Server Open] Listening at {cfg.BindIp}:{cfg.Port} / Steam:{steamsev}");


            var catalogPath = Path.Combine(AppContext.BaseDirectory, "Data", "store_catalog.json");
            Store.Catalog = StoreCatalogLoader.LoadFromJson(catalogPath);
            Log.Sys($"[Store] Catalog loaded. version={Store.Catalog.Version}, offers={Store.Catalog.Offers.Count}, items={Store.Catalog.Items.Count}");

            BattlePass.Load();

            LeaderboardService.Start();


            // 리포트용
            //_ = SteamMicroTxnService.TestGetReportOnceAsync();

            // 치명적인 오류시 기록 저장
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                if (e.ExceptionObject is Exception ex)
                    Log.Fatal(ex, "[UnhandledException]");
                else
                    Log.Error("[UnhandledException] non-Exception object");
            };

            TaskScheduler.UnobservedTaskException += (s, e) => {
                Log.Fatal(e.Exception, "[UnobservedTaskException]");
                e.SetObserved();
            };


            JobTimer.Instance.Push(FlushRooms);

            while (true) {
                totalConnect = SessionManager.Instance.Total();
                JobTimer.Instance.Flush();
            }

            //MetricsBootstrap.Stop();
        }
    }
}
