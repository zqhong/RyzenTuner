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
                : throw new InvalidOperationException($"未找到功率限制设置: {mode}");
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

            // 优先使用 InvariantCulture 解析（所有值均以 InvariantCulture 存储）
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out powerLimit) ||
                   float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out powerLimit);
        }

        public static string GetModeDetailText(string mode)
        {
            if (TryGetPowerLimitByMode(mode, out var powerLimit))
            {
                return $"{GetLocalizedModeName(mode)}-{powerLimit}W";
            }

            return $"{GetLocalizedModeName(mode)}-?W";
        }

        public static string GetLocalizedModeName(string mode)
        {
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
