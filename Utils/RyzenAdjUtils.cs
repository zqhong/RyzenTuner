using System;
using RyzenTuner.Common.Settings;

namespace RyzenTuner.Utils
{
    public static class RyzenAdjUtils
    {
        public static float GetPowerLimit()
        {
            var powerLimit = RyzenTunerUtils.GetPowerLimitByMode(AppSettings.Get("CurrentMode", "BalancedMode"));

            // 数值修正：负值或 NaN 都视为无效，回退到最小安全值 1W
            if (powerLimit < 0 || float.IsNaN(powerLimit))
            {
                powerLimit = 1;
            }

            return powerLimit;
        }

        public static uint GetTctlTemp()
        {
            return (uint)Math.Max(1, AppSettings.Get("TctlTemp", 100));
        }

        public static uint GetApuSkinTemp()
        {
            return (uint)Math.Max(1, AppSettings.Get("ApuSkinTemp", 43));
        }
    }
}
