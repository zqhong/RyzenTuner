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
        private static Guid GetActiveScheme()
        {
            HKEY powerKey = default;
            var result = PowerGetActiveScheme(powerKey, out var activeGuidHandle);
            if (result != Win32Error.NO_ERROR)
            {
                throw new Exception(result.ToString());
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
            HKEY powerKey = default;
            var activeGuid = GetActiveScheme();

            var acResult = PowerReadACValueIndex(
                powerKey,
                activeGuid,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                out var acValue
            );

            var dcResult = PowerReadDCValueIndex(
                powerKey,
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
         * 启用 Cpu Boost
         */
        public bool EnableCpuBoost()
        {
            if (IsCpuBoostEnabled())
            {
                return true;
            }

            HKEY powerKey = default;
            var activeGuid = GetActiveScheme();

            var r1 = PowerWriteACValueIndex(
                powerKey,
                activeGuid,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                Enabled
            );

            var r2 = PowerWriteDCValueIndex(
                powerKey,
                activeGuid,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                Enabled
            );

            var r3 = PowerSetActiveScheme(powerKey, activeGuid);

            return r1 == Win32Error.NO_ERROR && r2 == Win32Error.NO_ERROR && r3 == Win32Error.NO_ERROR;
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

            HKEY powerKey = default;
            var activeGuid = GetActiveScheme();

            var r1 = PowerWriteACValueIndex(
                powerKey,
                activeGuid,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                Disabled
            );

            var r2 = PowerWriteDCValueIndex(
                powerKey,
                activeGuid,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                Disabled
            );

            var r3 = PowerSetActiveScheme(powerKey, activeGuid);
            
            return r1 == Win32Error.NO_ERROR && r2 == Win32Error.NO_ERROR && r3 == Win32Error.NO_ERROR;
        }
    }
}
