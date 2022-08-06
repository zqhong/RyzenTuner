using System.Diagnostics;

namespace RyzenTuner
{
    public class CommonUtils
    {
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
