using System;
using System.Globalization;
using RyzenTuner.Common.Settings;

namespace RyzenTuner.Utils
{
    public static class RyzenTunerUtils
    {
        public static float GetPowerLimitByMode(string mode)
        {
            return TryGetPowerLimitByMode(mode, out var powerLimit)
                ? powerLimit
                : throw new InvalidOperationException($"Power limit setting not found: {mode}");
        }

        public static bool TryGetPowerLimitByMode(string mode, out float powerLimit)
        {
            powerLimit = 0;

            if (mode == null)
            {
                return false;
            }

            var value = AppSettings.Get(mode);

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out powerLimit);
        }

        public static string GetModeDetailText(string mode)
        {
            if (string.IsNullOrEmpty(mode))
            {
                return "?-?W";
            }

            var modeName = GetLocalizedModeName(mode);
            return TryGetPowerLimitByMode(mode, out var powerLimit)
                ? $"{modeName}-{powerLimit}W"
                : $"{modeName}-?W";
        }

        public static string GetLocalizedModeName(string mode)
        {
            if (string.IsNullOrEmpty(mode))
            {
                return mode ?? string.Empty;
            }

            return Properties.Strings.ResourceManager.GetString(mode) ?? mode;
        }

        /// <summary>
        /// 返回默认语言代码：zh-* 系统 → "zh-CN"，其他系统 → "en-US"
        /// </summary>
        public static string DetectDefaultLanguageCode()
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh"
                ? "zh-CN"
                : "en-US";
        }
    }
}
