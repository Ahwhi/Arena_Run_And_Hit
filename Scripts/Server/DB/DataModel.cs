using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Server.DB {
    public class DataModel {
        public class Account {
            public int Id { get; set; }
            public string accountId { get; set; } = null!;

            public long? steamId64 { get; set; }   // SteamID64 저장용 (nullable)
            public string passwordHash { get; set; } = null!;
            public string nickName { get; set; } = null!;
            public string recentIp { get; set; } = null!;
            public string? accessToken { get; set; }
            public DateTime? tokenExpireAt { get; set; }

            // 통화 잔액 (정수, 음수 금지)
            public long gold { get; set; } = 0;
            public long star { get; set; } = 0;

            // 계정 스탯
            public int level { get; set; } = 0;
            public int exp { get; set; } = 0;

            // 닉네임 최초 확정 여부(스팀 유저 전용으로 사용)
            public bool isNickSet { get; set; } = true;

            // 동의서
            public int policyVersion { get; set; } = 0;
            public DateTime? policyAgreedAt { get; set; }
            public byte banStatus { get; set; } = AccountBan.None;

            // 배패
            public ICollection<BattlePassState> BattlePassStates { get; set; } = new List<BattlePassState>();

            // 동시성 제어(낙관적 락) - 충돌 시 재시도
            public ICollection<AccountItem> Items { get; set; } = new List<AccountItem>();
            public ICollection<EquippedItem> Equipped { get; set; } = new List<EquippedItem>();
            public ICollection<SeasonStat> SeasonStats { get; set; } = new List<SeasonStat>();
        }

        // 보유 아이템 (스택/비스택 공용)
        public class AccountItem {
            public long Id { get; set; }
            public int AccountId { get; set; }
            public string sku { get; set; } = null!;
            public int quantity { get; set; } = 1;
            public DateTime createdAt { get; set; } = DateTime.UtcNow;

            public Account Account { get; set; } = null!;
        }

        // 장착 상태 (슬롯당 1개)
        public class EquippedItem {
            public long Id { get; set; }
            public int AccountId { get; set; }
            public string slot { get; set; } = null!; // e.g., "HEAD", "EMOTE"
            public string sku { get; set; } = null!;
            public DateTime updatedAt { get; set; } = DateTime.UtcNow;

            public Account Account { get; set; } = null!;
        }

        // 구매 주문(아이템 지급/통화 차감 트랜잭션의 영수증)
        public class PurchaseOrder {
            public Guid Id { get; set; } = Guid.NewGuid();
            public int AccountId { get; set; }
            public string offerId { get; set; } = null!;
            public string priceCurrency { get; set; } = null!; // GOLD/STAR
            public long priceAmount { get; set; }
            public string status { get; set; } = "Completed"; // Completed/Failed/Refunded 등
            public string? externalReceiptId { get; set; } // IAP 영수증 ID(유료재화 충전 등)
            public string? receiptProvider { get; set; } // Apple/Google/Steam/Backoffice
            public string idempotencyKey { get; set; } = null!; // 중복 구매 방지 키
            public DateTime createdAt { get; set; } = DateTime.UtcNow;
            public DateTime? completedAt { get; set; } = DateTime.UtcNow;

            public Account Account { get; set; } = null!;
        }

        // 잔액 변경 원장(감사로그)
        public class CurrencyLedger {
            public long Id { get; set; }
            public int AccountId { get; set; }
            public string currency { get; set; } = null!; // GOLD/STAR
            public long delta { get; set; } // +충전, -차감
            public long balanceAfter { get; set; }
            public string reason { get; set; } = null!; // "PURCHASE","GRANT","REFUND"
            public string refType { get; set; } = null!; // "ORDER","ADMIN","MATCH_REWARD"
            public string refId { get; set; } = null!;   // ex) 주문ID, 보상배치ID
            public DateTime createdAt { get; set; } = DateTime.UtcNow;

            public Account Account { get; set; } = null!;
        }

        public class SeasonStat {
            public long Id { get; set; }
            public int AccountId { get; set; }

            // 시즌 식별자 (예: "2025-S1" 또는 "2025")
            public string seasonKey { get; set; } = null!;

            // 현재 시즌 표시(한 계정에 seasonKey 단위 1개가 current)
            public bool isCurrent { get; set; } = true;

            // 집계 값(정수 저장)
            public int totalGames { get; set; } = 0;        // 총 경기수
            public int totalRankSum { get; set; } = 0;      // 총 순위(모든 경기의 순위 합)
            public int winCount { get; set; } = 0;          // 승리 횟수
            public int totalKills { get; set; } = 0;        // 총 킬수

            // 랭크/점수(저장)
            public byte rank { get; set; } = 0;             // 0~6
            public int rankScore { get; set; } = 0;         // 0~100

            public DateTime updatedAt { get; set; } = DateTime.UtcNow;

            // 계산값(비저장)
            [System.ComponentModel.DataAnnotations.Schema.NotMapped]
            public double avgRank => totalGames > 0 ? (double)totalRankSum / totalGames : 0.0;

            [System.ComponentModel.DataAnnotations.Schema.NotMapped]
            public double winRate => totalGames > 0 ? (double)winCount / totalGames : 0.0;

            [System.ComponentModel.DataAnnotations.Schema.NotMapped]
            public double avgKill => totalGames > 0 ? (double)totalKills / totalGames : 0.0;

            public Account Account { get; set; } = null!;
        }

        public class RecentGame {
            public long Id { get; set; }
            public int AccountId { get; set; }

            // 0=Survive, 1=Respawn, 2=RankSurvive (GameMode 캐스팅 값 그대로 저장)
            public int mode { get; set; }

            public int rank { get; set; }   // Respawn은 K/D 정렬로 부여된 최종 순위
            public int kills { get; set; }
            public int deaths { get; set; }

            // UTC 기준 게임 시작시간
            public DateTime startedAt { get; set; } = DateTime.UtcNow;

            public Account Account { get; set; } = null!;
        }

        public class BattlePassState {
            public long Id { get; set; }
            public int AccountId { get; set; }

            // 이 유저에게 해당되는 배틀패스 버전 (서버 BattlePassConfig.Version)
            public int version { get; set; }

            // 현재 배틀패스 레벨 (0~MaxLevel, 0이면 아직 미오픈 느낌)
            public int level { get; set; } = 0;

            // 혹시 나중에 BP 전용 경험치 쓸 수도 있으니까 남겨둠 (지금은 안 써도 됨)
            public int exp { get; set; } = 0;

            // 유료 배패 구매 여부
            public bool hasPremium { get; set; } = false;

            // 각 비트가 레벨별 free/premium 보상 수령 여부 (1이면 이미 수령)
            public ulong freeClaimBits { get; set; } = 0UL;
            public ulong premiumClaimBits { get; set; } = 0UL;

            public DateTime updatedAt { get; set; } = DateTime.UtcNow;

            public Account Account { get; set; } = null!;
        }

        public static class AccountBan {
            // 0 = 이상없음, 1 = 이상감지 검토중, 2 = 밴 확정
            public const byte None = 0;
            public const byte Suspicious = 1;
            public const byte Banned = 2;
        }

    }
        
}
