using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace RyzenTuner
{
    public class SystemInfo
    {
        /**
         * 获取 GPU 占用
         *
         * 注意：需要间隔1秒后调用，参考 GetCpuUsage
         * 
         * 参考：
         * https://github.com/rocksdanister/lively/blob/d4972447531a0a670ad8f8c4724c7faf7c619d8b/src/livelywpf/livelywpf/Helpers/HWUsageMonitor.cs#L143
         * https://stackoverflow.com/questions/56830434/c-sharp-get-total-usage-of-gpu-in-percentage
         */
        public static float GetGpuUsage()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();
                var gpuCounters = new List<PerformanceCounter>();
                var result = 0f;

                foreach (string counterName in counterNames)
                {
                    if (counterName.EndsWith("engtype_3D"))
                    {
                        foreach (PerformanceCounter counter in category.GetCounters(counterName))
                        {
                            if (counter.CounterName == "Utilization Percentage")
                            {
                                gpuCounters.Add(counter);
                            }
                        }
                    }
                }

                gpuCounters.ForEach(x => { _ = x.NextValue(); });

                gpuCounters.ForEach(x => { result += x.NextValue(); });

                return result;
            }
            catch
            {
                return 0f;
            }
        }


        /**
         * 返回当前 CPU 1W 下的占用
         *
         * 需要间隔1秒后调用，示例：
         * ```csharp
         * SystemInfo.CpuUsage();
         * System.Threading.Thread.Sleep(1000);
         * var cpuUsage = SystemInfo.CpuUsage();
         * ```
         * 
         * 参考：
         * https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.performancecounter
         * https://docs.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2003/cc780836(v=ws.10)?redirectedfrom=MSDN
         * https://stackoverflow.com/a/4711455
         */
        public static float GetCpuUsage()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var cpuUsage = cpuCounter.NextValue();
            return cpuUsage;
        }
    }
}