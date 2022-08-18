using RyzenTuner.Common.Container;

namespace RyzenTuner.Utils
{
    public static class DebugUtils
    {
        /// <summary>
        /// 记录 Cpu 在不同限制功耗下的数据
        ///
        /// 备注：仅在开发使用
        /// </summary>
        public static void LogCpuInfo()
        {
            AppContainer.HardwareMonitor().Monitor();

            AppContainer.PowerConfig().DisableCpuBoost();
            AppContainer.Logger().Debug("关闭睿频");
            _logCpuInfo();

            AppContainer.PowerConfig().EnableCpuBoost();
            AppContainer.Logger().Debug("开启睿频");
            _logCpuInfo();
        }

        private static void _logCpuInfo()
        {
            const int maxPowerLimit = 30;
            var hardware = AppContainer.HardwareMonitor();

            for (var i = 1; i <= maxPowerLimit; i++)
            {
                AppContainer.AmdProcessor().SetAllTdpLimit(i);
                System.Threading.Thread.Sleep(1000);
                AppContainer.HardwareMonitor().Monitor();

                AppContainer.Logger()
                    .Debug(
                        $"限制{i}瓦，CPU：{hardware.CpuUsage}%、{hardware.CpuTemperature}℃、{hardware.CpuFreq}MHz，GPU：{hardware.VideoCard3DUsage}");
            }
        }
    }
}