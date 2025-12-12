using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Server.Infra {
    public static class ConsoleEncodingFix {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleCP(uint wCodePageID);

        public static void ForceUtf8() {
            try {
                // chcp 65001 과 동일
                SetConsoleOutputCP(65001);
                SetConsoleCP(65001);

                Console.OutputEncoding = new UTF8Encoding(false); // BOM 없음
                Console.InputEncoding = Encoding.UTF8;
            } catch (Exception ex) {
                Console.WriteLine("[EncodingFix] failed: " + ex);
            }
        }
    }
}
