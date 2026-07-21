using System;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Common.Processor
{
    public class AmdProcessor : IDisposable
    {
        private enum PowerType
        {
            // long
            Slow = 0,
            Stapm = 1,
            Fast = 2,
        }

        private IntPtr _ry;
        private readonly object _lock = new();

        public bool CanChangeTdp { get; private set; }
        public RyzenFamily CpuFamily { get; private set; }

        public void Dispose()
        {
            CleanupRy();
        }

        public AmdProcessor()
        {
            try
            {
                _ry = RyzenAdj.init_ryzenadj();
                if (_ry == IntPtr.Zero)
                {
                    throw new InvalidOperationException(Properties.Strings.TextRyzenAdjInitFailed);
                }

                _ = RyzenAdj.get_table_ver(_ry);

                CpuFamily = RyzenAdj.get_cpu_family(_ry);
                CanChangeTdp = CpuFamily != RyzenFamily.FamUnknown;
            }
            catch (DllNotFoundException ex)
            {
                CleanupRy();
                throw new InvalidOperationException(
                    Properties.Strings.TextLibRyzenAdjLoadFailed.Replace("{message}", ex.Message), ex);
            }
            catch (BadImageFormatException ex)
            {
                CleanupRy();
                throw new InvalidOperationException(Properties.Strings.TextLibRyzenAdjArchitectureMismatch, ex);
            }
            catch (EntryPointNotFoundException ex)
            {
                CleanupRy();
                throw new InvalidOperationException(Properties.Strings.TextLibRyzenAdjTooOld, ex);
            }
            catch (Exception ex)
            {
                CleanupRy();
                throw new InvalidOperationException(
                    Properties.Strings.TextRyzenAdjInitFailedWithMessage.Replace("{message}", ex.Message), ex);
            }
        }

        private void CleanupRy()
        {
            if (_ry == IntPtr.Zero)
                return;

            RyzenAdj.cleanup_ryzenadj(_ry);
            _ry = IntPtr.Zero;
        }
        
        public float GetStapmLimit() { lock (_lock) return RyzenAdj.get_stapm_limit(_ry); }

        /// <summary>
        /// 返回 Fast Limit（读取 SMU 寄存器的设定值）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetFastLimit() { lock (_lock) return RyzenAdj.get_fast_limit(_ry); }

        /// <summary>
        /// 返回 Slow Limit（读取 SMU 寄存器的设定值）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetSlowLimit() { lock (_lock) return RyzenAdj.get_slow_limit(_ry); }

        /// <summary>
        /// 返回 Tctl Temp 限制（读取 SMU 寄存器的设定值）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetTctlTempLimit() { lock (_lock) return RyzenAdj.get_tctl_temp(_ry); }

        /// <summary>
        /// 刷新 SMU 内部表数据，使后续 get_* 调用读取到最新值。
        /// 等效于 ryzenadj --info 中的 refresh_table 步骤。
        /// </summary>
        public void RefreshTable() { lock (_lock) RyzenAdj.refresh_table(_ry); }

        /// <summary>
        /// 返回 APU 皮肤温度限制值（SMU 寄存器设定值）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetApuSkinTempLimit() { lock (_lock) return RyzenAdj.get_apu_skin_temp_limit(_ry); }

        /// <summary>
        /// 返回 APU 皮肤温度当前值（SMU 测量值）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetApuSkinTempValue() { lock (_lock) return RyzenAdj.get_apu_skin_temp_value(_ry); }

        public bool SetTctlTemp(uint temp)
        {
            lock (_lock)
            {
                var result = RyzenAdj.set_tctl_temp(_ry, temp);
                return result == (int)ErrCode.AdjErrNone;
            }
        }

        private bool SetTdpLimit(PowerType type, double limit)
        {
            if (!CanChangeTdp)
            {
                return false;
            }

            // 例如：15W : 15000 mW
            // 保护：拒绝负数、零值或 NaN，避免 (uint) 转换后环绕为超大值
            if (limit <= 0 || double.IsNaN(limit))
                return false;

            limit *= 1000;

            // 保护：防止 double→uint 溢出（uint.MaxValue = 4294967295）
            if (limit > uint.MaxValue)
                return false;

            int result;
            lock (_lock)
            {
                result = type switch
                {
                    PowerType.Fast => RyzenAdj.set_fast_limit(_ry, (uint)limit),
                    PowerType.Slow => RyzenAdj.set_slow_limit(_ry, (uint)limit),
                    PowerType.Stapm => RyzenAdj.set_stapm_limit(_ry, (uint)limit),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
            }

            return result == (int)ErrCode.AdjErrNone;
        }

        public bool SetFastPpt(double limit) => SetTdpLimit(PowerType.Fast, limit);

        public bool SetSlowPpt(double limit) => SetTdpLimit(PowerType.Slow, limit);

        public bool SetStapmPpt(double limit) => SetTdpLimit(PowerType.Stapm, limit);

        public bool SetApuSkinTemp(uint temp)
        {
            if (!CanChangeTdp)
                return false;

            int result;
            lock (_lock)
            {
                result = RyzenAdj.set_apu_skin_temp_limit(_ry, temp);
            }

            AppContainer.Logger().Debug("Call ryzenadj", $"AMDProcessor.SetApuSkinTemp: {temp}, result: {result}");
            return result == (int)ErrCode.AdjErrNone;
        }
    }
}
