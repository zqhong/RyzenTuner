using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using RyzenTuner.Common.Container;
using RyzenTuner.Utils;

namespace RyzenTuner.Common.Hardware
{
    public sealed class HardwareMonitor : IDisposable
    {
        private const string TctlTdieSensorName = "Core (Tctl/Tdie)";
        private const string PackagePowerSensorName = "Package";
        private const float MaxReasonableTemperature = 150f;
        private const float MaxReasonablePower = 1000f;
        private const float MinReasonableFrequency = 100f;

        private float _cpuPackagePower = float.NaN;
        private float _cpuTemperature = float.NaN;
        private float _cpuFrequency = float.NaN;

        private readonly IVisitor _visitor = new UpdateVisitor();
        private readonly Computer _computer;
        private readonly object _monitorLock = new();
        private readonly List<ISensor> _sensorsBuffer = new();
        private readonly List<(SensorType, string)> _sensorWarningsBuffer = new();
        private volatile bool _disposed;

        /// <summary>
        /// Dispose 期间后备日志记录：在 AppContainer 可能已被释放时使用。
        /// 静态且不捕获实例状态，因此声明为 static readonly。
        /// </summary>
        private static readonly Action<Exception> _logExceptionFallback = ex =>
            System.Diagnostics.Trace.WriteLine(
                $"[HardwareMonitor] Exception: {ex.Message}");

        /// <summary>
        /// Safely logs a warning via AppContainer.Logger(), falling back to Trace
        /// if the logger is unavailable (e.g. container already disposed).
        /// </summary>
        private static void SafeLogWarning(string message)
        {
            try
            {
                AppContainer.Logger().Warning(nameof(HardwareMonitor), message);
            }
            catch (Exception ex) when (!CommonUtils.IsFatalException(ex))
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[HardwareMonitor] Warning: {message} (logger: {ex.Message})");
            }
        }

        /// <summary>
        /// Safely logs an exception via AppContainer.Logger(), falling back to Trace
        /// if the logger is unavailable (e.g. container already disposed).
        /// </summary>
        private static void SafeLogException(Exception ex)
        {
            try
            {
                AppContainer.Logger().LogException(ex);
            }
            catch (Exception inner) when (!CommonUtils.IsFatalException(inner))
            {
                _logExceptionFallback(inner);
            }
        }

        public HardwareMonitor()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = false,
                IsMemoryEnabled = false,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false
            };

            try
            {
                _computer.Open();
            }
            catch (Exception openEx) when (!CommonUtils.IsFatalException(openEx))
            {
                try { _computer.Close(); }
                catch (Exception closeEx) when (!CommonUtils.IsFatalException(closeEx))
                {
                    AppContainer.Logger().LogException(closeEx);
                }
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_monitorLock)
            {
                if (_disposed)
                    return;

                _disposed = true;
            }

            // Close the Computer outside the lock to avoid blocking sensor readers
            // during potentially long-running hardware I/O teardown.
            try { _computer.Close(); }
            catch (Exception ex) when (!CommonUtils.IsFatalException(ex))
            {
                // Dispose 期间回避 AppContainer 服务检索（可能已被释放），使用后备日志
                _logExceptionFallback(ex);
            }
        }

        public float CpuPackagePower
        {
            get
            {
                lock (_monitorLock)
                {
                    return _cpuPackagePower;
                }
            }
        }

        public float CpuTemperature
        {
            get
            {
                lock (_monitorLock)
                {
                    return _cpuTemperature;
                }
            }
        }

        public float CpuFrequency
        {
            get
            {
                lock (_monitorLock)
                {
                    return _cpuFrequency;
                }
            }
        }

        /// <summary>
        /// 在一次加锁中读取所有传感器快照，减少锁获取次数
        /// </summary>
        public (float Power, float Temp, float Frequency) GetSnapshot()
        {
            lock (_monitorLock)
            {
                return (_cpuPackagePower, _cpuTemperature, _cpuFrequency);
            }
        }

        private void Update()
        {
            // Triggers the IVisitor.VisitComputer method for the given observer.
            _computer.Accept(_visitor);
        }

        public void Monitor()
        {
            if (_disposed)
                return;

            var (updateEx, readEx, sensorWarnings, generalWarning) = ReadSensorsUnderLock();

            // 在锁外记录异常和传感器警告，避免持有锁期间调用外部日志组件导致死锁
            if (updateEx != null)
                SafeLogException(updateEx);
            if (readEx != null)
                SafeLogException(readEx);

            if (generalWarning != null)
                SafeLogWarning(generalWarning);

            LogSensorWarnings(sensorWarnings);
        }

        private (Exception? UpdateEx, Exception? ReadEx, List<(SensorType, string)> SensorWarnings, string? GeneralWarning) ReadSensorsUnderLock()
        {
            Exception? updateEx = null;
            Exception? readEx = null;
            string? generalWarning = null;

            lock (_monitorLock)
            {
                _sensorWarningsBuffer.Clear();

                if (_disposed)
                {
                    return (null, null, _emptyWarningsList, null);
                }

                try
                {
                    Update();
                }
                catch (Exception e) when (!CommonUtils.IsFatalException(e))
                {
                    // Update 失败（如硬件 I/O 错误）不会阻塞后续读取，保留上次有效传感器值
                    updateEx = e;
                }

                try
                {
                    // Clear and reuse the buffer list to avoid per-tick allocation
                    _sensorsBuffer.Clear();
                    var hardwareList = _computer.Hardware;
                    if (hardwareList == null)
                    {
                        generalWarning =
                            "_computer.Hardware returned null, sensor fields not updated";
                    }
                    else
                    {
                        foreach (var hardware in hardwareList)
                        {
                            if (hardware == null)
                                continue;

                            if (hardware.HardwareType != HardwareType.Cpu)
                                continue;

                            // Guard against IHardware implementations that may return null Sensors
                            if (hardware.Sensors != null)
                            {
                                foreach (var sensor in hardware.Sensors)
                                {
                                    if (sensor == null)
                                        continue;
                                    _sensorsBuffer.Add(sensor);
                                }
                            }
                        }

                        // 先读取所有传感器值到局部变量，再一次性赋值，避免部分异常导致字段状态不一致
                        var power = FetchCpuPackagePower(_sensorsBuffer, _sensorWarningsBuffer);
                        var temp = FetchCpuTemperature(_sensorsBuffer, _sensorWarningsBuffer);
                        var frequency = FetchCpuFrequency(_sensorsBuffer, _sensorWarningsBuffer);

                        _cpuPackagePower = power;
                        _cpuTemperature = temp;
                        _cpuFrequency = frequency;
                    }
                }
                catch (Exception e) when (!CommonUtils.IsFatalException(e))
                {
                    readEx = e;
                }
            }

            // Return a snapshot copy so the internal buffer is never iterated outside the lock.
            // The common case (0 warnings) produces a near-zero-cost allocation.
            var warningsSnapshot = new List<(SensorType, string)>(_sensorWarningsBuffer);
            return (updateEx, readEx, warningsSnapshot, generalWarning);
        }

        private static void LogSensorWarnings(List<(SensorType, string)> sensorWarnings)
        {
            if (sensorWarnings.Count <= 0)
                return;

            var now = DateTime.UtcNow;
            foreach (var (type, name) in sensorWarnings)
            {
                var key = (type, name);
                if (_lastSensorWarnings.TryGetValue(key, out var lastTime) &&
                    (now - lastTime) < _sensorWarningInterval)
                {
                    continue;
                }

                _lastSensorWarnings[key] = now;
                SafeLogWarning($"Sensor warning: {type}/{name}");
            }
        }

        // Rate-limiting for sensor warnings: log at most once per 5 minutes per sensor.
        private static readonly ConcurrentDictionary<(SensorType, string), DateTime> _lastSensorWarnings = new();
        private static readonly TimeSpan _sensorWarningInterval = TimeSpan.FromMinutes(5);
        private static readonly List<(SensorType, string)> _emptyWarningsList = new(0);

        /// <summary>
        /// 通用的传感器值读取方法：按类型、名称查找，并验证是否在合理范围内。
        /// 当传感器完全未找到时，将条目添加到 warnings 列表（由调用方在锁外记录）。
        /// </summary>
        private static float FetchSensorValue(List<ISensor> sensors, SensorType sensorType, string sensorName, float maxReasonable, List<(SensorType, string)> warnings)
        {
            foreach (var s in sensors)
            {
                if (s.SensorType != sensorType || s.Name != sensorName)
                    continue;

                if (s.Value.HasValue)
                {
                    if (s.Value.Value <= maxReasonable)
                    {
                        return s.Value.Value;
                    }

                    // Sensor exists but value exceeds the reasonable range
                    warnings.Add((sensorType,
                        $"{sensorName} (value={s.Value.Value:F1}, max={maxReasonable})"));
                    return float.NaN;
                }

                // Sensor exists but has no value (e.g. transient read failure)
                warnings.Add((sensorType, $"{sensorName} (no value)"));
                return float.NaN;
            }

            // Sensor not found in collection at all
            warnings.Add((sensorType, sensorName));
            return float.NaN;
        }

        private static float FetchCpuTemperature(List<ISensor> sensors, List<(SensorType, string)> warnings)
            => FetchSensorValue(sensors, SensorType.Temperature, TctlTdieSensorName, MaxReasonableTemperature, warnings);

        private static float FetchCpuPackagePower(List<ISensor> sensors, List<(SensorType, string)> warnings)
            => FetchSensorValue(sensors, SensorType.Power, PackagePowerSensorName, MaxReasonablePower, warnings);

        /// <summary>
        /// 获取 CPU 平均频率
        /// </summary>
        /// <param name="cpuSensors">CPU core clock sensor collection</param>
        /// <returns>Average frequency across all cores, or NaN if unavailable</returns>
        private static float FetchCpuFrequency(List<ISensor> cpuSensors, List<(SensorType, string)> warnings)
        {
            float totalFreq = 0;
            var count = 0;
            var hasCoreSensor = false;

            // 遍历所有传感器，直接计算匹配 "Core #" 前缀的时钟传感器的平均频率。
            // 避免了 LINQ 链（Where + Select + GroupBy + ToDictionary）的每 tick 分配开销。
            foreach (var sensor in cpuSensors)
            {
                if (sensor.SensorType != SensorType.Clock || !sensor.Value.HasValue)
                    continue;

                var name = sensor.Name;
                if (name == null)
                    continue;

                if (!name.StartsWith("Core #", StringComparison.Ordinal))
                    continue;

                hasCoreSensor = true;

                if (sensor.Value.Value > MinReasonableFrequency)
                {
                    totalFreq += sensor.Value.Value;
                    count++;
                }
            }

            if (count > 0)
            {
                return totalFreq / count;
            }

            if (!hasCoreSensor)
            {
                warnings.Add((SensorType.Clock, "Core #*"));
            }

            return float.NaN;
        }
    }
}
