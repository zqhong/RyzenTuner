using RyzenTuner.Properties;

namespace RyzenTuner.Utils
{
    public class RyzenAdjUtils
    {
        public static float GetPowerLimit()
        {
            var powerLimit = RyzenTunerUtils.GetPowerLimitByMode(Settings.Default.CurrentMode);

            // 数值修正
            if (powerLimit < 0)
            {
                powerLimit = 1;
            }

            return powerLimit;
        }

        public static int GetTctlTemp()
        {
            return Settings.Default.TctlTemp;
        }

        public static int GetApuSkinTemp()
        {
            return Settings.Default.ApuSkinTemp;
        }
    }
}