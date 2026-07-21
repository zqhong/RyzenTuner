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
        private bool _disposed;

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
            if (_disposed)
            {
                return;
            }

            _computer.Close();
            _disposed = true;
        }

        public void Dispose()
        {
            lock (_monitorLock)
            {
                if (_disposed)
                {
                    return;
                }

                _computer.Close();
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        public float CpuPackagePower => _cpuPackagePower;

        public float CpuTemperature => _cpuTemperature;
        public float CpuFreq => _cpuFreq;

        private void Update()
        {
            // Triggers the IVisitor.VisitComputer method for the given observer.
            _computer.Accept(new UpdateVisitor());
        }

        public void Monitor()
        {
            try
            {
                lock (_monitorLock)
                {
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
                var cpuCount = Environment.ProcessorCount;

                float totalFreq = 0;
                var count = 0;

                for (var i = 1; i <= cpuCount; i++)
                {
                    var index = i;
                    var freq = cpuEnumerable
                        .Where(s => s.SensorType == SensorType.Clock && s.Name == $"Core #{index}" && s.Value != null)
                        .Select(s => s.Value)
                        .FirstOrDefault();
                    if (freq is > MinReasonableFrequency)
                    {
                        totalFreq += freq.Value;
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
