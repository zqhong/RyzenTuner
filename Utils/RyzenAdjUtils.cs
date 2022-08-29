using System;
using System.Windows.Forms;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Utils
{
    public class RyzenAdjUtils
    {
        /**
         * 计算自动模式下的限制功率（单位：瓦）
         *
         * 改进：
         * 1、适配不同型号 CPU
         */
        private static float AutoModePowerLimit()
        {
            var hardwareMonitor = AppContainer.HardwareMonitor();

            var cpuUsage = hardwareMonitor.CpuUsage;
            var videoCard3DUsage = hardwareMonitor.VideoCard3DUsage;
            var cpuTemperature = hardwareMonitor.CpuTemperature;

            var isNight = CommonUtils.IsNight(DateTime.Now);

            // 自动模式下，在几个模式下切换：待机、平衡、性能
            // 插电模式下
            var sleepPower = RyzenTunerUtils.GetPowerLimitByMode("SleepMode");
            var balancedPower = RyzenTunerUtils.GetPowerLimitByMode("BalancedMode");
            var performancePower = RyzenTunerUtils.GetPowerLimitByMode("PerformanceMode");


            // 电池模式下，最高性能为【平衡】
            if (SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline)
            {
                performancePower = RyzenTunerUtils.GetPowerLimitByMode("BalancedMode");
            }

            // 夜晚，最高性能为【平衡】
            if (isNight)
            {
                performancePower = RyzenTunerUtils.GetPowerLimitByMode("BalancedMode");
            }

            // 默认使用【平衡】
            var powerLimit = balancedPower;

            // 符合下面条件之一的情况下，使用【待机】
            var idleSecond = RyzenTunerUtils.GetIdleSecond();
            if (
                // 条件1：在短时间不活跃使用且笔记本负载较低的情况下，进入待机模式
                (idleSecond > 2 && cpuUsage < 5 && videoCard3DUsage < 5) ||

                // 条件2：白天 && 非活跃时间超过 x 分钟 && 笔记本低负载
                (!isNight && idleSecond > 4 * 60 && cpuUsage < 10 && videoCard3DUsage < 10) ||

                // 条件3：夜晚 && 非活跃时间超过 x 分钟 && 笔记本中负载
                (isNight && idleSecond > 4 * 60 && cpuUsage < 20 && videoCard3DUsage < 20) ||

                // 条件4：锁屏状态下 && 非活跃时间超过 x 秒 && 笔记本中负载
                (CommonUtils.IsSystemLocked() && idleSecond > 2 && cpuUsage < 20 && videoCard3DUsage < 20) ||

                // 条件5：锁屏状态下 && 非活跃时间超过 x 秒 && 温度大于 x 度
                (CommonUtils.IsSystemLocked() && idleSecond > 2 && cpuTemperature > 52)
            )
            {
                powerLimit = sleepPower;
            }

            // 非待机模式 && CPU 占用大于 50%，温度在 65 度以内，使用【性能模式】
            if (!RyzenTunerUtils.IsSleepMode(powerLimit) && cpuUsage >= 50 && cpuTemperature < 65)
            {
                powerLimit = performancePower;
            }

            return powerLimit;
        }

        public static float GetPowerLimit()
        {
            var powerLimit = RyzenTunerUtils.GetPowerLimitByMode(Properties.Settings.Default.CurrentMode);

            // 自动模式下，根据系统状态自动调整
            if (Properties.Settings.Default.CurrentMode == "AutoMode")
            {
                powerLimit = AutoModePowerLimit();
            }

            // 数值修正
            if (powerLimit < 0)
            {
                powerLimit = 1;
            }

            return powerLimit;
        }

        public static int GetTctlTemp()
        {
            return 90;
        }
    }
}