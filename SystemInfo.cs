using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RyzenTuner
{
    public static class SystemInfo
    {
        /**
         * 获取 GPU 占用
         *
         * 参考：
         * https://github.com/rocksdanister/lively/blob/d4972447531a0a670ad8f8c4724c7faf7c619d8b/src/livelywpf/livelywpf/Helpers/HWUsageMonitor.cs#L143-L185
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

                System.Threading.Thread.Sleep(1000);

                gpuCounters.ForEach(x => { result += x.NextValue(); });

                return result;
            }
            catch (Exception e)
            {
                new CommonUtils().LogInfo(e.Message);
                return 0f;
            }
        }


        /**
         * 返回当前 CPU 的占用
         * 
         * 参考：
         * https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-processor
         */
        public static float GetCpuUsage()
        {
            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("SELECT `LoadPercentage` FROM Win32_Processor");

            foreach (var obj in searcher.Get())
            {
                new CommonUtils().LogInfo("GetCpuUsage: " + obj.ToString());

                // uint16
                object loadObj = obj["LoadPercentage"];
                if (loadObj != null)
                {
                    return Int16.Parse(loadObj.ToString());
                }
            }

            return 0f;
        }
    }
}