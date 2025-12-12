using System;
using System.Collections.Generic;

namespace Server.Infra {
    public static class RandUtil {
        private static readonly Random _rng = new();

        public static float Range(float min, float max)
            => (float)(min + _rng.NextDouble() * (max - min));

        public static void Shuffle<T>(IList<T> list) {
            for (int i = list.Count - 1; i > 0; --i) {
                int j = _rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
