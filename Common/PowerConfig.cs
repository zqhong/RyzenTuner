using System;
using RyzenTuner.Common.Container;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.PowrProf;

namespace RyzenTuner.Common
{
    public class PowerConfig
    {
        private const uint BoostModeDisabled = 0;
        private const uint BoostModeEnabled = 1;
        private readonly object _configLock = new();

        /// <summary>
        /// 获取当前激活的电源方案的 GUID
        /// </summary>
        private Guid? GetActiveScheme()
        {
            var result = PowerGetActiveScheme(null, out var activeGuidHandle);
            if (result != Win32Error.NO_ERROR || activeGuidHandle is null || activeGuidHandle.IsInvalid)
            {
                return null;
            }

            using (activeGuidHandle)
            {
                return activeGuidHandle.ToStructure<Guid>();
            }
        }

        /// <summary>
        /// 检查是否开启了 Cpu Boost（根据当前供电来源读取对应值）
        /// </summary>
        /// <returns>true = 已启用，false = 已禁用，null = 读取失败（状态未知）</returns>
        public bool? IsCpuBoostEnabled()
        {
            lock (_configLock)
            {
                var activeGuid = GetActiveScheme();
                if (activeGuid is null) return null;

                // 检测当前供电来源（AC 或 DC）
                SYSTEM_POWER_STATUS powerStatus;
                var isOnAc = GetSystemPowerStatus(out powerStatus) &&
                             powerStatus.ACLineStatus == AC_STATUS.AC_ONLINE;

                var subGroup = GUID_PROCESSOR_SETTINGS_SUBGROUP;
                var setting = GUID_PROCESSOR_PERF_BOOST_MODE;
                uint value;

                Win32Error result;
                if (isOnAc)
                {
                    result = PowerReadACValueIndex(null, activeGuid.Value, subGroup, setting, out value);
                }
                else
                {
                    result = PowerReadDCValueIndex(null, activeGuid.Value, subGroup, setting, out value);
                }

                if (result != Win32Error.NO_ERROR)
                    return null;

                return value != BoostModeDisabled;
            }
        }

        /// <summary>
        /// 读取写前的原始 DC 和 AC 值，用于失败时的回滚
        /// 若读取失败，标记为无效，回滚时跳过写入
        /// </summary>
        private void ReadOriginalBoostValues(Guid activeScheme, out bool dcReadOk, out uint dcOriginalValue,
            out bool acReadOk, out uint acOriginalValue)
        {
            dcReadOk = PowerReadDCValueIndex(
                null,
                activeScheme,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                out dcOriginalValue
            ) == Win32Error.NO_ERROR;

            if (!dcReadOk)
            {
                AppContainer.Logger()?.Warning("PowerConfig",
                    "Failed to read original DC value for CPU boost, rollback will be skipped");
            }

            acReadOk = PowerReadACValueIndex(
                null,
                activeScheme,
                GUID_PROCESSOR_SETTINGS_SUBGROUP,
                GUID_PROCESSOR_PERF_BOOST_MODE,
                out acOriginalValue
            ) == Win32Error.NO_ERROR;

            if (!acReadOk)
            {
                AppContainer.Logger()?.Warning("PowerConfig",
                    "Failed to read original AC value for CPU boost, rollback will be skipped");
            }
        }

        /// <summary>
        /// 写入 AC 和 DC 的 Cpu Boost 值，并激活电源方案
        /// </summary>
        private bool SetCpuBoost(uint value)
        {
            lock (_configLock)
            {
                var activeGuid = GetActiveScheme();
                if (activeGuid is null) return false;

                // 读取写前的原始 DC 和 AC 值，用于失败时的回滚
                ReadOriginalBoostValues(activeGuid.Value, out var dcOriginalReadOk, out var originalDcValue,
                    out var acOriginalReadOk, out var originalAcValue);

                // 回滚 DC 到之前保存的原始值（仅在成功读取到原始值时执行）
                void RollbackDcToOriginal()
                {
                    if (!dcOriginalReadOk || originalDcValue == value)
                        return;

                    var rollbackResult = PowerWriteDCValueIndex(
                        null,
                        activeGuid.Value,
                        GUID_PROCESSOR_SETTINGS_SUBGROUP,
                        GUID_PROCESSOR_PERF_BOOST_MODE,
                        originalDcValue
                    );
                    if (rollbackResult != Win32Error.NO_ERROR)
                    {
                        AppContainer.Logger()?.Warning("PowerConfig",
                            $"DC rollback failed when restoring original value, error={rollbackResult}");
                    }
                }

                // 回滚 AC 到之前保存的原始值（仅在成功读取到原始值时执行）
                void RollbackAcToOriginal()
                {
                    if (!acOriginalReadOk || originalAcValue == value)
                        return;

                    var rollbackResult = PowerWriteACValueIndex(
                        null,
                        activeGuid.Value,
                        GUID_PROCESSOR_SETTINGS_SUBGROUP,
                        GUID_PROCESSOR_PERF_BOOST_MODE,
                        originalAcValue
                    );
                    if (rollbackResult != Win32Error.NO_ERROR)
                    {
                        AppContainer.Logger()?.Warning("PowerConfig",
                            $"AC rollback failed when restoring original value, error={rollbackResult}");
                    }
                }

                // 先写 DC（电池方案），后写 AC（电源方案），
                // 这样 AC 值在激活时是最新写入的，降低不一致窗口
                var writeDcResult = PowerWriteDCValueIndex(
                    null,
                    activeGuid.Value,
                    GUID_PROCESSOR_SETTINGS_SUBGROUP,
                    GUID_PROCESSOR_PERF_BOOST_MODE,
                    value
                );

                if (writeDcResult != Win32Error.NO_ERROR)
                {
                    AppContainer.Logger()?.Warning("PowerConfig",
                        $"PowerWriteDCValueIndex failed, error={writeDcResult}");
                    return false;
                }

                var writeAcResult = PowerWriteACValueIndex(
                    null,
                    activeGuid.Value,
                    GUID_PROCESSOR_SETTINGS_SUBGROUP,
                    GUID_PROCESSOR_PERF_BOOST_MODE,
                    value
                );

                if (writeAcResult != Win32Error.NO_ERROR)
                {
                    AppContainer.Logger()?.Warning("PowerConfig",
                        $"PowerWriteACValueIndex failed, error={writeAcResult}");
                    // 回滚 DC 到之前保存的原始值（仅在成功读取到原始值时执行）
                    RollbackDcToOriginal();
                    return false;
                }

                // 使用首次获取的 GUID 激活方案，消除 TOCTOU 窗口
                var activateResult = PowerSetActiveScheme(null, activeGuid.Value);
                if (activateResult != Win32Error.NO_ERROR)
                {
                    AppContainer.Logger()?.Warning("PowerConfig",
                        $"PowerSetActiveScheme failed, error={activateResult}");
                    // 激活失败时回滚 DC 和 AC 到原始值
                    RollbackDcToOriginal();
                    RollbackAcToOriginal();
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 启用 Cpu Boost / Enables CPU performance boost for both AC and DC power plans.
        /// </summary>
        /// <returns>true if the operation succeeded; false otherwise.</returns>
        public bool EnableCpuBoost()
        {
            return SetCpuBoost(BoostModeEnabled);
        }

        /// <summary>
        /// 关闭 Cpu Boost / Disables CPU performance boost for both AC and DC power plans.
        /// </summary>
        /// <returns>true if the operation succeeded; false otherwise.</returns>
        public bool DisableCpuBoost()
        {
            return SetCpuBoost(BoostModeDisabled);
        }
    }
}
