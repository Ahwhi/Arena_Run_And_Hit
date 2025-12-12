namespace Server.Monitoring {
    internal static class RoomIntrospector {
        public static int TryGetPlayerCount(Room room) {
            try {
                return room.Sessions?.Count ?? 0;
            } catch {
                return 0;
            }
        }

        public static string ModeToName(GameMode mode) => mode switch {
            GameMode.Survive => "Survive",
            GameMode.Respawn => "Respawn",
            GameMode.RankSurvive => "Rank",
            _ => "Unknown"
        };
    }
}
