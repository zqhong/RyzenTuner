using System;
using System.Globalization;
using System.Runtime.InteropServices;
using RyzenTuner.Common.Settings;

namespace RyzenTuner.Utils
{
    public static class RyzenTunerUtils
    {
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // https://www.pinvoke.net/default.aspx/user32.GetLastInputInfo
        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-lastinputinfo
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            [MarshalAs(UnmanagedType.U4)] public int cbSize;
            [MarshalAs(UnmanagedType.U4)] public UInt32 dwTime;
        }

        /**
         * 返回系统空闲时间，单位：秒
         *
         * 参考：
         * https://stackoverflow.com/questions/203384/how-to-tell-when-windows-is-inactive
         */
        public static int GetIdleSecond()
        {
            var lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            if (!GetLastInputInfo(ref lastInputInfo))
            {
                return 0;
            }

            uint envTicks = unchecked((uint)Environment.TickCount);
            uint idleTime = envTicks - lastInputInfo.dwTime;

            return (int)(idleTime / 1000);
        }


        public static float GetPowerLimitByMode(string mode)
        {
            if (TryGetPowerLimitByMode(mode, out var powerLimit))
            {
                return powerLimit;
            }

            throw new FormatException($"Invalid power limit setting: {mode}");
        }

        public static bool TryGetPowerLimitByMode(string mode, out float powerLimit)
        {
            powerLimit = 0;

            var value = AppSettings.Get(mode);

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out powerLimit) ||
                   float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out powerLimit);
        }

        public static string GetModeDetailText(string mode)
        {
            return $"{GetLocalizedModeName(mode)}-{GetPowerLimitByMode(mode)}W";
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
            return CultureInfo.CurrentUICulture.ToString().StartsWith("zh-")
                ? "zh-CN"
                : "en-US";
        }
    }
}
