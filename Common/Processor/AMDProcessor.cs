using System;
using System.Threading;

namespace RyzenTuner.Common.Processor
{
    public class AmdProcessor : IDisposable
    {
        private enum PowerType
        {
            /// <summary>Slow PPT — 持续/平均功率限制 (average/sustained)</summary>
            Slow = 0,
            /// <summary>STAPM Limit — 智能热平均功率限制</summary>
            Stapm = 1,
            /// <summary>Fast PPT — 短时突发功率限制 (burst/short duration)</summary>
            Fast = 2,
        }

        private const int WattsToMilliwatts = 1000;
        /// <summary>
        /// PPT 功率限制最小值（单位：瓦特），与 UI 范围和 RyzenAdjUtils 一致。
        /// </summary>
        private const double PowerLimitWattsMin = 1.0;
        /// <summary>
        /// PPT 功率限制最大值（单位：瓦特），与 UI 范围和 RyzenAdjUtils 一致。
        /// </summary>
        private const double PowerLimitWattsMax = 100.0;
        /// <summary>
        /// APU 皮肤温度 SMU 寄存器使用 Q8.8 定比格式，native setter 内部乘以 256，
        /// native getter 返回原始值，此处还原为摄氏度。
        /// </summary>
        private const int ApuSkinTempScale = 256;

        private volatile IntPtr _ry;
        private volatile int _disposed;
        private readonly object _lock = new();

        public bool CanChangeTdp { get; private set; }
        public RyzenFamily CpuFamily { get; private set; }

        ~AmdProcessor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            CanChangeTdp = false;
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            if (disposing)
            {
                // Dispose 路径：可以加锁、访问托管资源
                lock (_lock)
                {
                    CleanupRy();
                }
            }
            else
            {
                // 终结器路径：不可访问托管资源，不可抛出。
                // 使用 Interlocked.Exchange 原子性地提取并清空 _ry 句柄，
                // 避免在终结器线程中获取锁（阻塞终结器队列会导致进程不稳定）。
                // CleanupRy 中的 lock 在 Dispose 路径下使用，终结器路径不走 CleanupRy。
                try
                {
                    var handle = Interlocked.Exchange(ref _ry, IntPtr.Zero);
                    if (handle != IntPtr.Zero)
                    {
                        RyzenAdj.cleanup_ryzenadj(handle);
                    }
                }
                catch
                {
                    // 终结器异常在 .NET Framework 中会终止进程，必须全部抑制。
                }
            }
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

                // Warm-up call: triggers internal SMU table population; return value not needed
                RyzenAdj.get_table_ver(_ry);

                CpuFamily = RyzenAdj.get_cpu_family(_ry);
                CanChangeTdp = CpuFamily > RyzenFamily.FamUnknown;
            }
            catch (Exception ex)
            {
                CleanupRy();
                throw new InvalidOperationException(GetInitErrorMessage(ex), ex);
            }
        }

        private static string GetInitErrorMessage(Exception ex)
        {
            return ex switch
            {
                DllNotFoundException e => Properties.Strings.TextLibRyzenAdjLoadFailed
                    .Replace("{message}", e.Message),
                BadImageFormatException => Properties.Strings.TextLibRyzenAdjArchitectureMismatch,
                EntryPointNotFoundException => Properties.Strings.TextLibRyzenAdjTooOld,
                InvalidOperationException => Properties.Strings.TextRyzenAdjInitFailed,
                _ => Properties.Strings.TextRyzenAdjInitFailedWithMessage
                    .Replace("{message}", ex.Message),
            };
        }

        /// <summary>
        /// 释放原生 ryzenadj 句柄并将 _ry 置零。
        /// 调用方应确保在合适的同步上下文中调用（Dispose 路径有锁保护，构造函数路径无竞争）。
        /// 此方法始终吞异常：因为调用方是 Dispose（不应抛出）或构造函数失败回滚（应保留原始异常）。
        /// </summary>
        private void CleanupRy()
        {
            if (_ry == IntPtr.Zero)
                return;

            try
            {
                RyzenAdj.cleanup_ryzenadj(_ry);
            }
            catch
            {
                // 抑制清理异常。Dispose 不应抛出（否则可能导致进程终止），
                // 构造函数 catch 块需要保留原始异常上下文。
            }
            finally
            {
                // Always zero the pointer, even if the native cleanup throws.
                // This prevents use-after-free if any method is called after a partial cleanup.
                _ry = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 返回 STAPM Limit（读取 SMU 寄存器的设定值），单位：瓦特 (W)。
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetStapmLimit()
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return float.NaN;
                // 原生 API 返回毫瓦 (mW)，需转换为瓦特 (W)
                var raw = RyzenAdj.get_stapm_limit(handle);
                GC.KeepAlive(this);
                return float.IsNaN(raw) ? raw : raw / WattsToMilliwatts;
            }
        }

        /// <summary>
        /// 返回 Fast PPT 限制值（读取 SMU 寄存器的设定值），单位：瓦特 (W)。
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetFastLimit()
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return float.NaN;
                var raw = RyzenAdj.get_fast_limit(handle);
                GC.KeepAlive(this);
                return float.IsNaN(raw) ? raw : raw / WattsToMilliwatts;
            }
        }

        /// <summary>
        /// 返回 Slow PPT 限制值（读取 SMU 寄存器的设定值），单位：瓦特 (W)。
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetSlowLimit()
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return float.NaN;
                var raw = RyzenAdj.get_slow_limit(handle);
                GC.KeepAlive(this);
                return float.IsNaN(raw) ? raw : raw / WattsToMilliwatts;
            }
        }

        /// <summary>
        /// 返回 Tctl/Tdie 当前温度（读取 SMU 寄存器的测量值）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetTctlTemperature()
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return float.NaN;
                var temp = RyzenAdj.get_tctl_temp(handle);
                GC.KeepAlive(this);
                return temp;
            }
        }

        /// <summary>
        /// 刷新 SMU 内部表数据，使后续 get_* 调用读取到最新值。
        /// 等效于 ryzenadj --info 中的 refresh_table 步骤。
        /// </summary>
        public bool RefreshTable()
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return false;
                var result = RyzenAdj.refresh_table(handle);
                GC.KeepAlive(this);
                return result == ErrCode.AdjErrNone;
            }
        }

        /// <summary>
        /// 返回 APU 皮肤温度限制值（SMU 寄存器设定值），单位：摄氏度。
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        /// <remarks>
        /// native setter 内部将摄氏度乘以 ApuSkinTempScale（Q8.8 格式）再写入 SMU，
        /// native getter 返回原始 SMU 值，此方法还原为摄氏度。
        /// </remarks>
        public float GetApuSkinTempLimit()
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return float.NaN;
                var raw = RyzenAdj.get_apu_skin_temp_limit(handle);
                GC.KeepAlive(this);
                return float.IsNaN(raw) ? raw : raw / ApuSkinTempScale;
            }
        }

        /// <summary>
        /// 返回 APU 皮肤温度当前值（SMU 测量值），单位：摄氏度。
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetApuSkinTempValue()
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return float.NaN;
                var raw = RyzenAdj.get_apu_skin_temp_value(handle);
                GC.KeepAlive(this);
                return float.IsNaN(raw) ? raw : raw / ApuSkinTempScale;
            }
        }

        /// <summary>
        /// 设置 Tctl/Tdie 温度上限（单位：摄氏度）。
        /// </summary>
        /// <param name="temp">温度上限值（1–115°C）。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        public bool SetTctlTemp(uint temp)
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            if (!CanChangeTdp)
                return false;

            if (temp < 1 || temp > 115)
                return false;

            ErrCode result;
            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return false;

                result = RyzenAdj.set_tctl_temp(handle, temp);
                GC.KeepAlive(this);
            }

            return result == ErrCode.AdjErrNone;
        }

        private bool SetTdpLimit(PowerType type, double limit)
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            if (!CanChangeTdp)
            {
                return false;
            }

            // 例如：15W : 15000 mW
            // 保护：拒绝负数、零值、NaN 或 Infinity，避免 (uint) 转换后环绕为超大值
            if (double.IsNaN(limit) || double.IsInfinity(limit) || limit <= 0)
                return false;

            // 保护：强制限制在文档声明的有效范围内（与 RyzenAdjUtils 一致）
            if (limit < PowerLimitWattsMin || limit > PowerLimitWattsMax)
                return false;

            var limitMilliwatts = limit * WattsToMilliwatts;

            // 保护：防止 double→uint 溢出（uint.MaxValue = 4294967295）
            if (limitMilliwatts > uint.MaxValue)
                return false;

            // 保护：防止 Math.Round 结果为零（极小正值向下取整为 0 mW）
            var limitMilliwattsRounded = (uint)Math.Round(limitMilliwatts, MidpointRounding.AwayFromZero);
            if (limitMilliwattsRounded == 0)
                return false;

            ErrCode result;
            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return false;

                result = type switch
                {
                    PowerType.Fast => RyzenAdj.set_fast_limit(handle, limitMilliwattsRounded),
                    PowerType.Slow => RyzenAdj.set_slow_limit(handle, limitMilliwattsRounded),
                    PowerType.Stapm => RyzenAdj.set_stapm_limit(handle, limitMilliwattsRounded),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"Unknown power type: {type}")
                };
                GC.KeepAlive(this);
            }

            return result == ErrCode.AdjErrNone;
        }

        /// <summary>
        /// 设置 Fast PPT（快速功率限制），单位：瓦特。
        /// </summary>
        /// <param name="limit">功率限制值（1–100W 有效范围）。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        public bool SetFastPpt(double limit) => SetTdpLimit(PowerType.Fast, limit);

        /// <summary>
        /// 设置 Slow PPT（持续功率限制），单位：瓦特。
        /// </summary>
        /// <param name="limit">功率限制值（1–100W 有效范围）。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        public bool SetSlowPpt(double limit) => SetTdpLimit(PowerType.Slow, limit);

        /// <summary>
        /// 设置 STAPM Limit（STAPM 功率限制），单位：瓦特。
        /// </summary>
        /// <param name="limit">功率限制值（1–100W 有效范围）。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        public bool SetStapmPpt(double limit) => SetTdpLimit(PowerType.Stapm, limit);

        /// <summary>
        /// 设置 APU 皮肤温度上限（单位：摄氏度）。
        /// 仅部分 CPU 家族支持此参数。
        /// </summary>
        /// <param name="temp">温度上限值。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        public bool SetApuSkinTemp(uint temp)
        {
            if (_disposed == 1)
                throw new ObjectDisposedException(GetType().Name);

            if (!CanChangeTdp)
                return false;

            if (temp < 1 || temp > 100)
                return false;

            ErrCode result;
            lock (_lock)
            {
                var handle = _ry;
                if (handle == IntPtr.Zero)
                    return false;

                result = RyzenAdj.set_apu_skin_temp_limit(handle, temp);
                GC.KeepAlive(this);
            }

            return result == ErrCode.AdjErrNone;
        }
    }
}
