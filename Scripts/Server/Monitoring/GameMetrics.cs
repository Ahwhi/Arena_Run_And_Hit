using Prometheus;
using System;
using System.Collections.Generic;

namespace Server.Monitoring {
    internal static class GameMetrics {
        // ---- Gauges / Histograms ----
        private static readonly Gauge OnlineTotal = Metrics.CreateGauge(
            "game_online_total", "Current total online players");

        private static readonly Gauge RoomsTotal = Metrics.CreateGauge(
            "game_rooms_total", "Current total game rooms");

        // mode: Survive / Respawn / AI / Unknown
        private static readonly Gauge OnlineByMode = Metrics.CreateGauge(
            "game_online_by_mode", "Online by mode", new GaugeConfiguration { LabelNames = new[] { "mode" } });

        // room 라벨까지(필요 시 Grafana에서 표로 조회)
        private static readonly Gauge OnlineByRoom = Metrics.CreateGauge(
            "game_online_by_room", "Online by room and mode",
            new GaugeConfiguration { LabelNames = new[] { "mode", "room_id" } });

        // 서버 틱(FlushRoom 주기 포함) 시간 관측용 (선택)
        private static readonly Histogram TickDurationMs = Metrics.CreateHistogram(
            "game_tick_duration_ms", "Tick/Flush duration (ms)",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(1, 1.5, 12) });

        // 공개 API -------------------------------------------------------------

        public static void ObserveTick(double elapsedMs) {
            TickDurationMs.Observe(elapsedMs);
        }

        public static void PublishSnapshot(IReadOnlyList<(string mode, string roomId, int count)> rooms) {
            // 집계
            int total = 0, survive = 0, respawn = 0, rank = 0, unknown = 0;

            // 일단 전체 clear (라벨이 많아질 경우에는 scrape 주기 내 자연 소거를 권장)
            // 여기서는 단순화를 위해 매 스냅샷 시 재설정
            OnlineByMode.WithLabels("Survive").Set(0);
            OnlineByMode.WithLabels("Respawn").Set(0);
            OnlineByMode.WithLabels("Rank").Set(0);
            OnlineByMode.WithLabels("Unknown").Set(0);

            foreach (var (mode, roomId, count) in rooms) {
                total += count;

                switch (mode) {
                    case "Survive": survive += count; break;
                    case "Respawn": respawn += count; break;
                    case "Rank": rank += count; break;
                    default: unknown += count; break;
                }

                // room 단위도 기록
                OnlineByRoom.WithLabels(mode, roomId).Set(count);
            }

            OnlineTotal.Set(total);
            RoomsTotal.Set(rooms.Count);

            OnlineByMode.WithLabels("Survive").Set(survive);
            OnlineByMode.WithLabels("Respawn").Set(respawn);
            OnlineByMode.WithLabels("Rank").Set(rank);
            OnlineByMode.WithLabels("Unknown").Set(unknown);
        }
    }
}
