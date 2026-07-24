using System;
using System.Diagnostics;
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
        /// PPT 功率限制最大值（单位：W），与 UI 范围一致。
        /// </summary>
        private const float MaxPowerLimit = 100f;

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

        /// <summary>
        /// 将整数值限定在 [min, max] 范围内。
        /// </summary>
        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        /// <summary>
        /// 将浮点数值限定在 [min, max] 范围内。
        /// NaN/Infinity 会触发回退到 min。
        /// </summary>
        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return min;
            }

            return Math.Max(min, Math.Min(max, value));
        }

        public static float GetPowerLimit()
        {
            var mode = AppSettings.Get("CurrentMode", "BalancedMode");

            // 确保 mode 不为空，避免空模式名导致极端回退
            if (string.IsNullOrWhiteSpace(mode))
            {
                Trace.WriteLine(
                    $"[RyzenAdjUtils] GetPowerLimit: CurrentMode is empty/whitespace, using BalancedMode");
                mode = "BalancedMode";
            }

            // 先尝试获取已保存的功率限制，若未找到则回退到最小安全值
            if (!RyzenTunerUtils.TryGetPowerLimitByMode(mode, out var powerLimit))
            {
                Trace.WriteLine(
                    $"[RyzenAdjUtils] GetPowerLimit: TryGetPowerLimitByMode('{mode}') failed, falling back to {MinPowerLimit}W");
                powerLimit = MinPowerLimit;
            }

            // 确保在 [MinPowerLimit, MaxPowerLimit] 范围内
            return Clamp(powerLimit, MinPowerLimit, MaxPowerLimit);
        }

        public static uint GetTctlTemp()
        {
            var result = Clamp(
                AppSettings.Get("TctlTemp", DefaultTctlTemp),
                MinTemperature,
                MaxTctlTemp);
            Debug.Assert(result >= 0, "TctlTemp must be non-negative");
            return (uint)result;
        }

        public static uint GetApuSkinTemp()
        {
            var result = Clamp(
                AppSettings.Get("ApuSkinTemp", DefaultApuSkinTemp),
                MinTemperature,
                MaxApuSkinTemp);
            Debug.Assert(result >= 0, "ApuSkinTemp must be non-negative");
            return (uint)result;
        }
    }
}
