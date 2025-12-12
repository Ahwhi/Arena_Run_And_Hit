using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using static Server.DB.DataModel;

namespace Server.DB {
    public class AppDBContext : DbContext {
        public AppDBContext() { }
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { } // 디자인타임/DI용 옵션 생성자
        public DbSet<Account> Accounts { get; set; }
        public DbSet<RecentGame> RecentGames { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            var baseDir = AppContext.BaseDirectory;
            var dbPath = Path.Combine(baseDir, "Data", "GameDB.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDBContext> {
            public AppDBContext CreateDbContext(string[] args) {
                // 디자인타임은 작업 디렉터리 기준(솔루션/프로젝트 루트)
                var baseDir = Directory.GetCurrentDirectory();
                var dbPath = Path.Combine(baseDir, "Data", "GameDB.db");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

                var options = new DbContextOptionsBuilder<AppDBContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                return new AppDBContext(options);
            }
        }

        protected override void OnModelCreating(ModelBuilder b) {
            b.Entity<Account>().ToTable("Accounts");
            b.Entity<Account>().HasIndex(x => x.accountId).IsUnique();
            b.Entity<Account>().HasIndex(x => x.nickName).IsUnique();

            b.Entity<Account>().HasIndex(x => x.steamId64).IsUnique(); // 스팀
            b.Entity<Account>().Property(x => x.isNickSet);

            b.Entity<AccountItem>().ToTable("AccountItems");
            b.Entity<AccountItem>().HasIndex(x => new { x.AccountId, x.sku }).IsUnique();

            b.Entity<EquippedItem>().ToTable("EquippedItems");
            b.Entity<EquippedItem>().HasIndex(x => new { x.AccountId, x.slot }).IsUnique();

            b.Entity<PurchaseOrder>().ToTable("PurchaseOrders");
            b.Entity<PurchaseOrder>().HasIndex(x => new { x.AccountId, x.idempotencyKey }).IsUnique();
            b.Entity<PurchaseOrder>().HasIndex(x => new { x.AccountId, x.offerId });

            b.Entity<CurrencyLedger>().ToTable("CurrencyLedger");
            b.Entity<CurrencyLedger>().HasIndex(x => new { x.AccountId, x.createdAt });

            // SeasonStat 매핑(추가)
            b.Entity<SeasonStat>().ToTable("SeasonStats");
            b.Entity<SeasonStat>()
                .HasIndex(x => new { x.AccountId, x.seasonKey })
                .IsUnique(); // 한 시즌Key당 1개

            b.Entity<SeasonStat>().HasIndex(x => new { x.isCurrent, x.rank, x.rankScore, x.updatedAt });

            // FK
            b.Entity<SeasonStat>()
                .HasOne(x => x.Account)
                .WithMany(a => a.SeasonStats)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);


            b.Entity<RecentGame>().ToTable("RecentGames");
            b.Entity<RecentGame>()
                .HasIndex(x => new { x.AccountId, x.startedAt }); // 최신순 조회 최적화

            b.Entity<RecentGame>()
                .HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==== BattlePassState ====
            b.Entity<BattlePassState>().ToTable("BattlePassStates");
            b.Entity<BattlePassState>()
                .HasIndex(x => new { x.AccountId, x.version })
                .IsUnique();

            b.Entity<BattlePassState>()
                .HasOne(x => x.Account)
                .WithMany(a => a.BattlePassStates)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
