using System;
using RyzenTuner.Common.Settings;

namespace RyzenTuner.Utils
{
    public static class RyzenAdjUtils
    {
        /// <summary>
        /// PPT 功率限制最小值（单位：W）。
        /// </summary>
        private const float MinPowerLimit = 1f;

        /// <summary>
        /// Tctl/Tdie 温度上限默认值（单位：C）。
        /// </summary>
        private const int DefaultTctlTemp = 100;

        /// <summary>
        /// APU 皮肤温度上限默认值（单位：C）。
        /// </summary>
        private const int DefaultApuSkinTemp = 43;

        /// <summary>
        /// 温度下限（单位：C）。
        /// </summary>
        private const int MinTemperature = 1;

        /// <summary>
        /// Tctl/Tdie 温度上限最大值（单位：C）。
        /// </summary>
        private const int MaxTctlTemp = 115;

        /// <summary>
        /// APU 皮肤温度上限最大值（单位：C）。
        /// </summary>
        private const int MaxApuSkinTemp = 100;

        public static float GetPowerLimit()
        {
            var mode = AppSettings.Get("CurrentMode", "BalancedMode");

            // 先尝试获取已保存的功率限制，若未找到则回退到最小安全值
            if (!RyzenTunerUtils.TryGetPowerLimitByMode(mode, out var powerLimit))
            {
                powerLimit = MinPowerLimit;
            }

            // 数值修正：负值、0、NaN、Infinity 都视为无效，回退到最小安全值 1W
            if (powerLimit <= 0 || float.IsNaN(powerLimit) || float.IsInfinity(powerLimit))
            {
                powerLimit = MinPowerLimit;
            }

            return powerLimit;
        }

        public static uint GetTctlTemp()
        {
            var value = AppSettings.Get("TctlTemp", DefaultTctlTemp);
            value = Math.Max(MinTemperature, value);
            return (uint)Math.Min(value, MaxTctlTemp);
        }

        public static uint GetApuSkinTemp()
        {
            var value = AppSettings.Get("ApuSkinTemp", DefaultApuSkinTemp);
            value = Math.Max(MinTemperature, value);
            return (uint)Math.Min(value, MaxApuSkinTemp);
        }
    }
}
