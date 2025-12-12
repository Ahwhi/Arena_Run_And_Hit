using System;

namespace Server.Infra {
    public static class MathUtil {
        public static float NormalizeYaw(float yaw) {
            if (yaw < 0f) yaw += 360f;
            else if (yaw >= 360f) yaw -= 360f;
            return yaw;
        }

        // z+가 정면일 때 yaw = atan2(x,z)
        public static float YawDegLookAt(float fromX, float fromZ, float toX, float toZ) {
            float dx = toX - fromX;
            float dz = toZ - fromZ;
            double rad = Math.Atan2(dx, dz);
            float deg = (float)(rad * 180.0 / Math.PI);
            if (deg < 0f) deg += 360f;
            return deg;
        }
    }
}
