using System;
using Vanara.PInvoke;
using static Vanara.PInvoke.PowrProf;

namespace RyzenTuner.Common
{
    public class PowerConfig
    {
        private const uint Disabled = 0;
        private const uint Enabled = 1;

        /**
         * 获取当前激活的电源方案的 GUID
         */
        private static Guid? GetActiveScheme()
        {
            var result = PowerGetActiveScheme(default, out var activeGuidHandle);
            if (result != Win32Error.NO_ERROR)
            {
                return null;
            }

            using (activeGuidHandle)
            {
                return activeGuidHandle.ToStructure<Guid>();
            }
        }

        /**
         * 检查是否开启了 Cpu Boost
         * 返回值：true = 已启用，false = 已禁用，null = 读取失败（状态未知）
         */
        public bool? IsCpuBoostEnabled()
        {
            var activeGuid = GetActiveScheme();
            if (activeGuid == null) return null;

            var acResult = PowerReadACValueIndex(
                default,
                activeGuid.Value,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                out var acValue
            );

            var dcResult = PowerReadDCValueIndex(
                default,
                activeGuid.Value,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                out var dcValue
            );

            if (acResult != Win32Error.NO_ERROR || dcResult != Win32Error.NO_ERROR)
                return null;

            return acValue == Enabled && dcValue == Enabled;
        }

        /**
         * 写入 AC 和 DC 的 Cpu Boost 值，并激活电源方案
         */
        private bool SetCpuBoost(uint value)
        {
            var activeGuid = GetActiveScheme();
            if (activeGuid == null) return false;

            var writeAc = PowerWriteACValueIndex(
                default,
                activeGuid.Value,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                value
            );

            var writeDc = PowerWriteDCValueIndex(
                default,
                activeGuid.Value,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                value
            );

            var activate = PowerSetActiveScheme(default, activeGuid.Value);

            return writeAc == Win32Error.NO_ERROR && writeDc == Win32Error.NO_ERROR && activate == Win32Error.NO_ERROR;
        }

        /**
         * 启用 Cpu Boost
         */
        public bool EnableCpuBoost()
        {
            return SetCpuBoost(Enabled);
        }

        /**
         * 关闭 Cpu Boost
         */
        public bool DisableCpuBoost()
        {
            return SetCpuBoost(Disabled);
        }
    }
}
