using RyzenTuner.Common.Settings;

namespace RyzenTuner.Utils
{
    public static class RyzenAdjUtils
    {
        public static float GetPowerLimit()
        {
            var powerLimit = RyzenTunerUtils.GetPowerLimitByMode(AppSettings.Get("CurrentMode", "BalancedMode"));

            // 数值修正
            if (powerLimit < 0)
            {
                powerLimit = 1;
            }

            return powerLimit;
        }

        public static int GetTctlTemp()
        {
            return AppSettings.Get("TctlTemp", 100);
        }

        public static int GetApuSkinTemp()
        {
            return AppSettings.Get("ApuSkinTemp", 43);
        }
    }
}