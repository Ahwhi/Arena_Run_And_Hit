namespace Server {
    public class LevelData {
        // 틱
        public static readonly int TICK_RATE = 50;
        public static readonly int MAX_CONSUME_PER_TICK = 12;
        public static readonly float DT = 1f / TICK_RATE;

        // 게임 준비 틱
        public static readonly float GAMEREADY_TICK_COUNT = 3f;

        // 공격 쿨다운 틱
        public static readonly int ATTACK_COOLDOWN_TICKS = 14;
        public static readonly int BOT_ATTACK_COOLDOWN_TICKS = 25;

        // 피격 딜레이 틱 (0ms기준 8이 적정값인데.... 핑때문에 낮춰야할듯) (20ms기준 5?)
        public static readonly int ATTACK_HIT_DELAY_TICKS = 5;

        // 타격 판정 가능 각도
        public static readonly float MELEE_ARC_DEG = 150f;

        // 기본 데미지
        public static readonly int BASE_DAMAGE = 15;
        public static readonly float ONSUPPLY_DAMAGE_MUL = 2.0f;

        // 구토
        public static readonly float VOMIT_RADIUS = 6.0f;
        public static readonly int VOMIT_DAMAGE = 40;

        // 사정거리
        public static readonly float DEFAULT_RANGE = 2.5f; // 2.6
        public static readonly float ONSUPPLY_RANGE = 5.0f; // 5.2

        // 스폰
        public static readonly float SPAWN_RADIUS = 18f;
        public static readonly float SPAWN_JITTER = 2f;

        // 보급품
        public static readonly int SUPPLY_INTERVAL_MS = 10_000;
        public static readonly int SUPPLY_LIFETIME_MS = 10_000;
        public static readonly int DURATION_MS_10S = 10_000;
        public static readonly float SUPPLY_PICKUP_RADIUS = 1.5f;

        // 매칭
        public static readonly int MAX_PER_ROOM = 10; // 게임방 최대 인원
        public static readonly int MIN_START_PLAYERS = 10; // 테스트 편의상 1
        public static readonly int WAIT_MS_BACKOFF = 30_000;
        public static bool RankAllowBots = false;

        // 계정
        public static readonly int MAX_ACCOUNT_EXP = 10;
        public static readonly int LEVELUP_GOLD = 100;
        public static readonly int LEVELUP_STAR = 10;

        public static readonly string[] BlockedNickWords = new[] {
        "운영자", "관리자", "개새", "개새끼", "새끼", "새기", "새귀", "빙신",
        "븅신", "뵹신", "싀발", "씌발", "쒸발", "쉬발", "줫", "좆", "병신", 
        "씨발", "시발", "염병", "지랄", "애미", "뒤진", "디진", "뒈진", "느금",
        "니엄", "창녀", "앰창", "gm", "admin", "moderator", "fuck", "shit",
        "nigger", "nigro", "chigga", "ching", "chang", "chong", "육갑", "섹스", "자지", "보지",
        "고추", "잠지", "짬지", "penis", "dick", "pussy", "pussi", "육봉", "좆물", "보짓물", "색스",
        "성교", "성관계", "여상위", "펠라치오", "오르가즘", "팝니다", "삽니다", "sex", "쎅스",
        "이벤트", "nigga", "niga", "gook", "현금", "거래", "작업", "장애", "년", "딜도", "콘돔",
        "젖", "유두", "꼭지", "항문", "애널", "anal", "oral", "오랄", "체위", "supervisor" };

        // 상점
        public static readonly (int cents, int stars, uint itemId, string desc)[] Packs = {
            (100,   100,   10001, "100 Stars"),
            (500,   600,   10002, "600 Stars"),
            (1000,  1500,  10003, "1500 Stars"),
            (3000,  5000, 10004, "5000 Stars"),
            (4900, 10000, 10005, "10000 Stars"),
        };

        // 맵
        public static readonly float MAP_MIN_X = -25f;
        public static readonly float MAP_MAX_X = 25f;
        public static readonly float MAP_MIN_Z = -25f;
        public static readonly float MAP_MAX_Z = 25f;
        public static (float x, float z) RandomPointInMap(Random rng) {
            float x = (float)(MAP_MIN_X + rng.NextDouble() * (MAP_MAX_X - MAP_MIN_X));
            float z = (float)(MAP_MIN_Z + rng.NextDouble() * (MAP_MAX_Z - MAP_MIN_Z));
            return (x, z);
        }

    }
}
