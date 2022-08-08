using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace RyzenTuner
{
    public class CpuUsage
    {
        private FILETIME _preIdleTime;
        private FILETIME _preKernelTime;
        private FILETIME _preUserTime;

        [DllImport("kernel32.dll", SetLastError = true)]

        // http://www.pinvoke.net/default.aspx/kernel32/GetSystemTimes.html
        static extern bool GetSystemTimes(
            out FILETIME lpIdleTime,
            out FILETIME lpKernelTime,
            out FILETIME lpUserTime
        );

        private ulong CompareFileTime2(FILETIME time1,
            FILETIME time2)
        {
            var a = ((ulong)time1.dwHighDateTime << 32) + (uint)time1.dwLowDateTime;
            var b = ((ulong)time2.dwHighDateTime << 32) + (uint)time2.dwLowDateTime;

            return b - a;
        }

        /**
         * 通过 GetSystemTimes 计算 CPU 占用
         * 参考：https://github.com/zhongyang219/TrafficMonitor/blob/master/TrafficMonitor/CPUUsage.cpp#L23-L49
         */
        public double GetCpuUsage()
        {
            GetSystemTimes(out var idleTime, out var kernelTime, out var userTime);

            var idleTimeLong = this.CompareFileTime2(_preIdleTime, idleTime);
            var kernelTimeLong = this.CompareFileTime2(_preKernelTime, kernelTime);
            var userTimeLong = this.CompareFileTime2(_preUserTime, userTime);

            double cpuUsage;
            if (kernelTimeLong + userTimeLong == 0)
            {
                cpuUsage = 0;
            }
            else
            {
                //（总的时间-空闲时间）/总的时间=占用cpu的时间就是使用率
                cpuUsage = (kernelTimeLong + userTimeLong - idleTimeLong) * 100.0 / (kernelTimeLong + userTimeLong);
            }

            _preIdleTime = idleTime;
            _preKernelTime = kernelTime;
            _preUserTime = userTime;

            return cpuUsage;
        }
    }
}