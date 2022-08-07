using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RyzenTuner
{
    public class CommonUtils
    {
        // https://www.pinvoke.net/default.aspx/user32.GetLastInputInfo
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-lastinputinfo
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)] public int cbSize;
            [MarshalAs(UnmanagedType.U4)] public UInt32 dwTime;
        }

        /**
         * 检查提供的日期是否出于晚上
         */
        private bool isNight(DateTime now)
        {
            TimeSpan nightShiftStart = new TimeSpan(23, 59, 0); // 23:59pm 
            TimeSpan nightShiftEnd = new TimeSpan(7, 0, 0); // 7am

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
        public int GetIdleSecond()
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

        public void LogInfo(string content)
        {
            var filePath = AppDomain.CurrentDomain.BaseDirectory + "\\runtime\\ryzen-tuner.log.txt";
            File.AppendAllText(filePath,
                string.Format(@"[INFO]{0:yyyy-MM-dd hh:mm:ss} {1}{2}", DateTime.Now, content,
                    Environment.NewLine));
        }

        /**
         * 计算自动模式下的限制功率（单位：瓦）
         *
         * 参考：
         * https://github.com/slimbook/slimbookbattery/blob/main/src/configuration/slimbookbattery.conf
         *
         * TLP - Optimize Linux Laptop Battery Life
         * https://github.com/linrunner/TLP/blob/main/defaults.conf
         */
        public int AutoModePowerLimit(float cpuUsage)
        {
            // 参考变量：当前 CPU 占用、5 分钟内 CPU 占用、白天/晚上
            var isNight = this.isNight(DateTime.Now);

            // 三个档位：low（待机）、medium（平衡）、high（高性能）
            // 插电模式下
            var low = 1;
            var medium = 16;
            var high = 30;

            // 电池模式下
            if (SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline)
            {
                low = 1;
                medium = 8;
                high = 16;
            }

            // 夜晚
            if (isNight)
            {
                low = 1;
                medium = 8;
                high = 16;
            }

            // 默认使用 medium（平衡）
            // CPU 超过某个占用后，使用 high（高性能）
            // CPU x 分钟内占用低于 x，使用 low（待机）
            var powerLimit = medium;

            // 符合下面条件的情况下，使用 low（待机）
            var idleSecond = this.GetIdleSecond();
            if (
                // 条件1、白天 && 非活跃时间超过5分钟 && CPU 占用小于 10%
                (!isNight && idleSecond >= 5 * 60 && cpuUsage < 10) ||
                // 条件2、夜晚 && 非活跃时间超过1分钟 && CPU 占用小于 10%
                (isNight && idleSecond >= 1 * 60 && cpuUsage < 10)
            )
            {
                powerLimit = low;
            }

            if (cpuUsage >= 50)
            {
                powerLimit = high;
            }

            this.LogInfo(string.Format(@"power line status: {0}, power limit: {1}, last input time: {2}",
                SystemInformation.PowerStatus.PowerLineStatus,
                powerLimit,
                this.GetIdleSecond()
            ));

            return powerLimit;
        }

        /**
         * 返回当前 CPU 占用
         * 
         * 参考：
         * https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.performancecounter
         * https://stackoverflow.com/a/4711455
         */
        public float CpuUsage()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            var cpuUsage = cpuCounter.NextValue();
            return cpuUsage;
        }
    }
}