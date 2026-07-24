using System;
using System.Globalization;
using RyzenTuner.Common.Settings;

namespace RyzenTuner.Utils
{
    public static class RyzenTunerUtils
    {
        /// <summary>
        /// 获取指定模式的功率限制值（瓦特）。
        /// </summary>
        /// <param name="mode">模式名称，不能为 null 或空白。</param>
        /// <returns>功率限制值（正数）。</returns>
        /// <exception cref="ArgumentException">mode 为 null 或空白。</exception>
        /// <exception cref="InvalidOperationException">设置缺失或值无效（非数值、零或负数）。</exception>
        public static float GetPowerLimitByMode(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
                throw new ArgumentException("Mode cannot be null or whitespace.", nameof(mode));

            if (!TryGetPowerLimitByMode(mode, out var powerLimit))
                throw new InvalidOperationException(
                    $"Failed to get power limit for mode '{mode}': setting missing or invalid value.");

            return powerLimit;
        }

        /// <summary>
        /// 尝试获取指定模式的功率限制值。
        /// </summary>
        /// <param name="mode">模式名称，为 null 或空白时返回 false。</param>
        /// <param name="powerLimit">成功时返回正数的功率限制值；失败时返回 0。</param>
        /// <returns>成功获取有效正数功率值则返回 true，否则返回 false。</returns>
        public static bool TryGetPowerLimitByMode(string mode, out float powerLimit)
        {
            powerLimit = 0;

            if (string.IsNullOrWhiteSpace(mode))
                return false;

            string? value;
            try
            {
                value = AppSettings.Get(mode);
            }
            catch (InvalidOperationException)
            {
                // AppSettings 未初始化
                return false;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!float.TryParse(value,
                    NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out powerLimit))
            {
                return false;
            }

            // 拒绝非正数值（零、负数），也拒绝 NaN/Infinity
            return powerLimit > 0f && !float.IsInfinity(powerLimit);
        }

        /// <summary>
        /// 返回模式详情文本，用于 UI 按钮/菜单显示（如 "省电模式-16W"）。
        /// </summary>
        /// <param name="mode">模式名称，为 null 或空白时返回 "?-?W"。</param>
        /// <returns>格式化的模式详情文本。</returns>
        public static string GetModeDetailText(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return "?-?W";
            }

            var modeName = GetLocalizedModeName(mode);
            return TryGetPowerLimitByMode(mode, out var powerLimit)
                ? $"{modeName}-{powerLimit.ToString(CultureInfo.InvariantCulture)}W"
                : $"{modeName}-?W";
        }

        /// <summary>
        /// 返回本地化的模式名称。
        /// </summary>
        /// <param name="mode">模式名称，为 null 或空白时返回空字符串。</param>
        /// <returns>本地化名称；若未找到对应资源则返回 mode 本身。</returns>
        public static string GetLocalizedModeName(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return string.Empty;
            }

            return Properties.Strings.ResourceManager.GetString(mode) ?? mode;
        }

        /// <summary>
        /// 返回默认语言代码：zh-CN → "zh-CN"，zh-TW/zh-HK/zh-MO → "zh-TW"，其他系统 → "en-US"
        /// </summary>
        public static string DetectDefaultLanguageCode()
        {
            var culture = CultureInfo.CurrentUICulture;

            if (culture.TwoLetterISOLanguageName != "zh")
                return "en-US";

            var name = culture.Name;
            if (name.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("zh-MO", StringComparison.OrdinalIgnoreCase))
                return "zh-TW";

            return "zh-CN";
        }
    }
}
