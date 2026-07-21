using System;
using Vanara.PInvoke;
using static Vanara.PInvoke.PowrProf;

namespace RyzenTuner.Common
{
    public class PowerConfig
    {
        private const uint Disabled = 0;
        private const uint Enabled = 1;

        private static readonly HKEY PowerKey = default;

        /**
         * 获取当前激活的电源方案的 GUID
         */
        private static Guid GetActiveScheme()
        {
            var result = PowerGetActiveScheme(PowerKey, out var activeGuidHandle);
            if (result != Win32Error.NO_ERROR)
            {
                throw new InvalidOperationException($"Failed to get active power scheme: {result}");
            }

            using (activeGuidHandle)
            {
                return activeGuidHandle.ToStructure<Guid>();
            }
        }

        /**
         * 检查是否开启了 Cpu Boost
         */
        public bool IsCpuBoostEnabled()
        {
            var activeGuid = GetActiveScheme();

            var acResult = PowerReadACValueIndex(
                PowerKey,
                activeGuid,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                out var acValue
            );

            var dcResult = PowerReadDCValueIndex(
                PowerKey,
                activeGuid,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                out var dcValue
            );

            // 读取失败时保守地返回 false（视为已禁用），避免意外覆盖用户设置
            if (acResult != Win32Error.NO_ERROR || dcResult != Win32Error.NO_ERROR)
                return false;

            return acValue == Enabled && dcValue == Enabled;
        }

        /**
         * 写入 AC 和 DC 的 Cpu Boost 值，并激活电源方案
         */
        private bool SetCpuBoost(uint value)
        {
            var activeGuid = GetActiveScheme();

            var writeAc = PowerWriteACValueIndex(
                PowerKey,
                activeGuid,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                value
            );

            var writeDc = PowerWriteDCValueIndex(
                PowerKey,
                activeGuid,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                value
            );

            var activate = PowerSetActiveScheme(PowerKey, activeGuid);

            return writeAc == Win32Error.NO_ERROR && writeDc == Win32Error.NO_ERROR && activate == Win32Error.NO_ERROR;
        }

        /**
         * 启用 Cpu Boost
         */
        public bool EnableCpuBoost()
        {
            if (IsCpuBoostEnabled())
            {
                return true;
            }

            return SetCpuBoost(Enabled);
        }

        /**
         * 关闭 Cpu Boost
         */
        public bool DisableCpuBoost()
        {
            if (!IsCpuBoostEnabled())
            {
                return true;
            }

            return SetCpuBoost(Disabled);
        }
    }
}
