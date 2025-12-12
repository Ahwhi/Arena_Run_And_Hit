using Prometheus;
using Server.Infra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Server.Monitoring {
    internal static class MetricsBootstrap {
        private static KestrelMetricServer? _metricServer;
        private static Timer? _timer;

        public static void Start(int port = 9100, int intervalMs = 1000) {
            _metricServer = new KestrelMetricServer(port: port);
            _metricServer.Start();

            _timer = new Timer(_ => SafeCollect(), null, dueTime: 1000, period: intervalMs);
            Log.Sys($"[Metrics] Exposed at :{port}");
        }

        public static void Stop() {
            try {
                _timer?.Dispose();
                _timer = null;
                _metricServer?.Stop();
                _metricServer = null;
                Log.Info("[Metrics] Stopped");
            } catch (Exception ex) {
                Log.Warn($"[Metrics] Stop error: {ex.Message}");
            }
        }

        private static void SafeCollect() {
            try {
                var sw = Stopwatch.StartNew();

                var list = new List<(string mode, string roomId, int count)>();

                // 실제 게임룸
                for (int i = 0; i < Program.GameRooms.Count; i++) {
                    var room = Program.GameRooms[i];
                    int cnt = RoomIntrospector.TryGetPlayerCount(room);
                    string mode = RoomIntrospector.ModeToName(room.Mode);
                    string roomId = $"game-{i}";
                    list.Add((mode, roomId, cnt));
                }

                // 원하면 로비/매칭룸도 포함
                void addNamed(string name, Room r) {
                    int cnt = RoomIntrospector.TryGetPlayerCount(r);
                    string mode = RoomIntrospector.ModeToName(r.Mode);
                    list.Add((mode, name, cnt));
                }

                addNamed("login", Program.LoginRoom);
                addNamed("lobby", Program.LobbyRoom);
                addNamed("mm_survive", Program.MatchmakingSurvive);
                addNamed("mm_respawn", Program.MatchmakingRespawn);
                addNamed("mm_rank", Program.MatchmakingRank);

                GameMetrics.PublishSnapshot(list);

                sw.Stop();
                GameMetrics.ObserveTick(sw.Elapsed.TotalMilliseconds);
            } catch (Exception ex) {
                Log.Warn($"[Metrics] Collect error: {ex.Message}");
            }
        }
    }
}
