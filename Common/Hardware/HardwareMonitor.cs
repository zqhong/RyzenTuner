using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Common.Hardware
{
    public class HardwareMonitor : IDisposable
    {
        private const string TcTtlDieSensorName = "Core (Tctl/Tdie)";
        private const string PackagePowerSensorName = "Package";
        private const float MaxReasonableTemperature = 150f;
        private const float MaxReasonablePower = 1000f;
        private const float MinReasonableFrequency = 100f;

        private float _cpuPackagePower;
        private float _cpuTemperature;
        private float _cpuFreq;

        private readonly Computer _computer;
        private readonly object _monitorLock = new();
        private volatile bool _disposed;

        public HardwareMonitor()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false
            };

            _computer.Open();
        }

        // Finalizers / destructor
        ~HardwareMonitor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            // Finalizers must not touch managed resources.
            // _computer.Close() may access managed child objects (IHardware, ISensor, etc.)
            // that may already have been finalized, so only call it during explicit disposal.
            if (disposing)
            {
                _computer.Close();
            }

            _disposed = true;
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

        public float CpuFreq
        {
            get
            {
                lock (_monitorLock)
                {
                    return _cpuFreq;
                }
            }
        }

        private void Update()
        {
            // Triggers the IVisitor.VisitComputer method for the given observer.
            _computer.Accept(new UpdateVisitor());
        }

        public void Monitor()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                lock (_monitorLock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    Update();

                    var cpuHardwareList = _computer
                        .Hardware
                        .Where(i => i.HardwareType == HardwareType.Cpu)
                        .ToList();

                    var cpuSensorList = cpuHardwareList.SelectMany(s => s.Sensors).ToList();
                    _cpuPackagePower = FetchCpuPackage(cpuSensorList);
                    _cpuTemperature = FetchCpuTemperature(cpuSensorList);
                    _cpuFreq = FetchCpuFreq(cpuSensorList);
                }
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
        }

        private static float FetchCpuTemperature(IEnumerable<ISensor> cpuEnumerable)
        {
            try
            {
                var sensorValue = cpuEnumerable
                    .Where(s => s.SensorType == SensorType.Temperature && s.Name == TcTtlDieSensorName && s.Value != null)
                    .Select(s => s.Value)
                    .FirstOrDefault();
                if (sensorValue is <= MaxReasonableTemperature)
                {
                    return sensorValue.Value;
                }
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }

            return float.NaN;
        }

        private static float FetchCpuPackage(IEnumerable<ISensor> cpuEnumerable)
        {
            try
            {
                var sensorValue = cpuEnumerable
                    .Where(s => s.SensorType == SensorType.Power && s.Name == PackagePowerSensorName && s.Value != null)
                    .Select(s => s.Value)
                    .FirstOrDefault();
                if (sensorValue is <= MaxReasonablePower)
                {
                    return sensorValue.Value;
                }
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }

            return float.NaN;
        }

        /// <summary>
        /// 获取 CPU 平均频率
        /// </summary>
        /// <param name="cpuEnumerable"></param>
        /// <returns></returns>
        private static float FetchCpuFreq(IEnumerable<ISensor> cpuEnumerable)
        {
            try
            {
                // Build a dictionary keyed by sensor name for O(1) per-core lookups,
                // avoiding repeated O(n) scans of the sensor list.
                var clockSensors = cpuEnumerable
                    .Where(s => s.SensorType == SensorType.Clock && s.Value != null)
                    .GroupBy(s => s.Name)
                    .ToDictionary(g => g.Key, g => g.First().Value!.Value);

                var cpuCount = Environment.ProcessorCount;

                float totalFreq = 0;
                var count = 0;

                for (var i = 1; i <= cpuCount; i++)
                {
                    if (clockSensors.TryGetValue($"Core #{i}", out var freq) &&
                        freq > MinReasonableFrequency)
                    {
                        totalFreq += freq;
                        count++;
                    }
                }

                if (count > 0)
                {
                    return totalFreq / count;
                }
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }

            return float.NaN;
        }
    }
}
