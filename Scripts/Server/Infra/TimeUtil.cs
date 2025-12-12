using System;

namespace Server.Infra {
    public static class TimeUtil {
        public static long NowMs() => Environment.TickCount64;
    }
}
