using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.DB;
using Microsoft.EntityFrameworkCore;
using static Server.DB.DataModel;

namespace Server.Infra
{
    public static class LeaderboardService {
        public sealed class Row {
            public string nickName;
            public int tier;
            public int score;
            public int totalGames;
            public double winRate;
            public double avgRank;
            public double avgKill;
        }

        public sealed class Snapshot {
            public DateTime lastUpdatedUtc;
            public List<Row> rows = new();
        }

        // 멀티스레드 안전 교체
        static volatile Snapshot _snapshot;

        public static void Start() {
            // 즉시 1회 실행 후, 1시간 주기로 반복
            JobTimer.Instance.Push(Refresh, 0);
        }

        public static Snapshot Current => _snapshot;

        static void ScheduleNext() {
            JobTimer.Instance.Push(Refresh, 1 * 60 * 1000); // 1분
        }

        static void Refresh() {
            try {
                using var db = new AppDBContext();

                // 현재 시즌만, 티어 desc → 점수 desc → updatedAt desc
                var query = db.Set<SeasonStat>()
                              .AsNoTracking()
                              .Include(s => s.Account)
                              .Where(s => s.isCurrent)
                              .OrderByDescending(s => s.rank)
                              .ThenByDescending(s => s.rankScore)
                              .ThenByDescending(s => s.updatedAt)
                              .Select(s => new Row {
                                  nickName = s.Account.nickName,
                                  tier = (int)s.rank,
                                  score = s.rankScore,
                                  totalGames = s.totalGames,
                                  winRate = s.totalGames > 0 ? (double)s.winCount / s.totalGames * 100.0 : 0.0,
                                  avgRank = s.totalGames > 0 ? (double)s.totalRankSum / s.totalGames : 0.0,
                                  avgKill = s.totalGames > 0 ? (double)s.totalKills / s.totalGames : 0.0
                              });

                // 상한을 정하고 싶으면 .Take(5000) 같은 컷 추가
                var list = query.ToList();

                var snap = new Snapshot {
                    lastUpdatedUtc = DateTime.UtcNow,
                    rows = list
                };

                // 원자적으로 교체
                _snapshot = snap;
                //Log.Sys($"[Leaderboard] snapshot updated. rows={list.Count}");
            } catch (Exception ex) {
                Log.Error($"[Leaderboard] refresh error: {ex.Message}");
                // 실패해도 이전 스냅샷 유지
            } finally {
                ScheduleNext();
            }
        }

        // 페이징 편의
        public static (int lastUpdatedSec, List<Row> page, int baseRank) GetPage(int offset, int limit) {
            var snap = _snapshot;
            if (snap == null || snap.rows == null) {
                return (0, new List<Row>(), offset + 1);
            }
            offset = Math.Max(0, offset);
            limit = Math.Clamp(limit, 1, 200); // 과도한 요청 방지
            var page = snap.rows.Skip(offset).Take(limit).ToList();
            int lastUpdated = (int)new DateTimeOffset(DateTime.SpecifyKind(snap.lastUpdatedUtc, DateTimeKind.Utc))
                                .ToUnixTimeSeconds();
            return (lastUpdated, page, offset + 1);
        }
    }
}
