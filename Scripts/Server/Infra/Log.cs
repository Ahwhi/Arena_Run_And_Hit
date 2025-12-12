using System;
using System.Text;

namespace Server.Infra {
    public static class Log {
        private const string TIME_FMT = "MM/dd HH:mm:ss";
        private static readonly string _logDir =
            Path.Combine(AppContext.BaseDirectory, "Logs");
        private static readonly string _fatalFile =
            Path.Combine(_logDir, "fatal.log");

        public static void Info(string msg) => WriteConsole("\x1b[92mINFO\x1b[0m ", msg);
        public static void Warn(string msg) => WriteConsole("\x1b[93mWARN\x1b[0m ", msg);
        public static void Error(string msg) => WriteConsole("\x1b[91mERROR\x1b[0m", msg);
        public static void Sys(string msg) => WriteConsole("\x1b[96mSYS\x1b[0m  ", msg);
        public static void Pay(string msg) => WriteConsole("\x1b[95mPAY\x1b[0m  ", msg);
        public static void CNT(string msg) => WriteConsole("\x1b[94mCNT\x1b[0m  ", msg);

        // === 치명적 예외 파일 저장 ===
        public static void Fatal(Exception ex, string? context = null) {
            string ts = DateTime.Now.ToString(TIME_FMT);

            if (!string.IsNullOrEmpty(context))
                Console.WriteLine($"[{ts}] \x1b[91mFATAL\x1b[0m {context}\n{ex}");
            else
                Console.WriteLine($"[{ts}] \x1b[91mFATAL\x1b[0m {ex}");

            try {
                Directory.CreateDirectory(_logDir);
                var sb = new StringBuilder();
                sb.Append($"[{ts}] FATAL ");
                if (!string.IsNullOrEmpty(context)) sb.Append(context).Append(' ');
                sb.AppendLine();
                sb.AppendLine(ex.ToString()); // stacktrace 포함
                sb.AppendLine("--------------------------------------------------");
                File.AppendAllText(_fatalFile, sb.ToString(), Encoding.UTF8);
            } catch { /* 파일 실패는 무시 */ }
        }

        private static void WriteConsole(string levelAnsi, string msg) {
            string ts = DateTime.Now.ToString(TIME_FMT);
            Console.WriteLine($"[{ts}] {levelAnsi} {msg}");
        }
    }
}
