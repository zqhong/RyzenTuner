using System;
using System.Globalization;
using System.Runtime.InteropServices;

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
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

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

            // 检查设置项是否存在（避免升级后残留的旧模式名引发 SettingsPropertyNotFoundException）
            if (Properties.Settings.Default.Properties[mode] == null)
                return false;

            var value = Properties.Settings.Default[mode]?.ToString();

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
    }
}
