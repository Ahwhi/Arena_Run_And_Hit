using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.DB;
using static Server.DB.DataModel;

namespace Server.Infra
{
    class StatProfiler
    {
        private static string CurrentSeasonKey() {
            return DateTime.UtcNow.Year.ToString();
        }

        public static SeasonStat GetOrCreateCurrentSeason(AppDBContext db, Account acc) {
            string key = CurrentSeasonKey();
            var cur = db.Set<SeasonStat>().FirstOrDefault(s => s.AccountId == acc.Id && s.seasonKey == key);
            if (cur == null) {
                var olds = db.Set<SeasonStat>().Where(s => s.AccountId == acc.Id && s.isCurrent).ToList();
                foreach (var o in olds) o.isCurrent = false;

                cur = new SeasonStat {
                    AccountId = acc.Id,
                    seasonKey = key,
                    isCurrent = true,
                    totalGames = 0,
                    totalRankSum = 0,
                    totalKills = 0,
                    winCount = 0,
                    rank = 0,
                    rankScore = 0,
                    updatedAt = DateTime.UtcNow
                };
                db.Set<SeasonStat>().Add(cur);
                db.SaveChanges();
            }
            return cur;
        }
    }
}
