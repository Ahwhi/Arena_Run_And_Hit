namespace Server.Infra {
    public static class IpFilter {
        class Entry {
            public int FailCount;
            public long LastFailMs;
        }

        static readonly object _lock = new();
        static readonly Dictionary<string, Entry> _bad = new();

        // n초 안에 m번 이상 타임아웃 난 IP는 차단
        const int BAN_THRESHOLD = 3;          // 3번
        const int BAN_WINDOW_MS = 40_000;     // 40초

        public static void ReportHandshakeTimeout(string ip) {
            if (string.IsNullOrEmpty(ip))
                return;

            lock (_lock) {
                if (!_bad.TryGetValue(ip, out var e)) {
                    e = new Entry { FailCount = 1, LastFailMs = TimeUtil.NowMs() };
                    _bad[ip] = e;
                } else {
                    long now = TimeUtil.NowMs();
                    if (now - e.LastFailMs > BAN_WINDOW_MS) {
                        // 오래됐으면 리셋
                        e.FailCount = 1;
                        e.LastFailMs = now;
                    } else {
                        e.FailCount++;
                        e.LastFailMs = now;
                    }
                }
            }
        }

        public static bool ShouldReject(string ip) {
            if (string.IsNullOrEmpty(ip))
                return false;

            lock (_lock) {
                if (!_bad.TryGetValue(ip, out var e))
                    return false;

                long now = TimeUtil.NowMs();
                if (now - e.LastFailMs > BAN_WINDOW_MS)
                    return false; // 윈도우 지났으면 풀어줌

                return e.FailCount >= BAN_THRESHOLD;
            }
        }
    }
}
