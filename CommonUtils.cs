using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace RyzenTuner
{
    public class CommonUtils
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
         * 检查系统是否处于锁屏状态
         */
        public static bool IsSystemLocked()
        {
            // logonui，即 Windows Logon User Interface Host，翻译为【登录用户界面】
            return Process.GetProcessesByName("logonui").Length > 0;
        }

        /**
         * 检查字体是否存在
         */
        public static bool IsFontExists(string fontName)
        {
            const float fontSize = 12;

            using var fontTester = new Font(
                fontName,
                fontSize,
                FontStyle.Regular,
                GraphicsUnit.Pixel);

            return fontTester.Name == fontName;
        }

        /**
         * 检查提供的日期是否处于晚上
         */
        public static bool IsNight(DateTime now)
        {
            TimeSpan nightShiftStart = new TimeSpan(23, 59, 0); // 23:59pm 
            TimeSpan nightShiftEnd = new TimeSpan(7, 0, 0); // 7:00am

            if (now.TimeOfDay > nightShiftStart || now.TimeOfDay < nightShiftEnd)
            {
                return true;
            }

            return false;
        }

        /**
         * 返回系统空闲时间，单位：秒
         *
         * 参考：
         * https://stackoverflow.com/questions/203384/how-to-tell-when-windows-is-inactive
         */
        public static int GetIdleSecond()
        {
            int idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            int envTicks = Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                int lastInputTick = (int)lastInputInfo.dwTime;

                idleTime = envTicks - lastInputTick;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : idleTime);
        }

        /**
         * 检查是否支持 EnergyStar
         *
         * EnergyStar 需要 OS Build 版本大于等于 22000，即 Windows 11 21H2。EnergyStar 开发者建议使用 22H2
         * 参考：
         * https://github.com/imbushuo/EnergyStar/blob/master/EnergyStar/Program.cs#L29-L39
         * https://github.com/imbushuo/EnergyStar/issues/10
         */
        public static bool IsSupportEnergyStar()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version.Build >= 22000;
        }

        /**
         * 检查当前是否处于【待机模式】
         */
        public static bool IsSleepMode(float powerLimit)
        {
            return Math.Abs(powerLimit - GetPowerLimitByMode("SleepMode")) < 0.01;
        }

        /**
         * 检查当前是否处于【省电模式】
         */
        public static bool IsPowerSaveModeMode(float powerLimit)
        {
            return Math.Abs(powerLimit - GetPowerLimitByMode("PowerSaveMode")) < 0.01;
        }

        /**
         * 检查当前是否处于【平衡模式】
         */
        public static bool IsBalancedMode(float powerLimit)
        {
            return Math.Abs(powerLimit - GetPowerLimitByMode("BalancedMode")) < 0.01;
        }

        /**
         * 检查当前是否处于【性能模式】
         */
        public static bool IsPerformanceModeMode(float powerLimit)
        {
            return Math.Abs(powerLimit - GetPowerLimitByMode("PerformanceMode")) < 0.01;
        }

        public static void LogInfo(string content)
        {
            var filePath = AppDomain.CurrentDomain.BaseDirectory + "\\runtime\\ryzen-tuner.log.txt";
            File.AppendAllText(filePath,
                string.Format(@"[INFO]{0:yyyy-MM-dd hh:mm:ss} {1}{2}", DateTime.Now, content,
                    Environment.NewLine));
        }

        public static float GetPowerLimitByMode(string mode)
        {
            return float.Parse(Properties.Settings.Default[mode].ToString());
        }
    }
}