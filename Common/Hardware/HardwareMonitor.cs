using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Common.Hardware
{
    public class HardwareMonitor : IDisposable
    {
        private float _cpuPackagePower;
        private float _cpuTemperature;
        private float _cpuFreq;

        private readonly Computer _computer;
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
            DisposeCore();
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        private void DisposeCore()
        {
            if (_disposed)
            {
                return;
            }

            _computer.Close();
            _disposed = true;
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
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
        }

        private static float FetchCpuTemperature(IEnumerable<ISensor> cpuEnumerable)
        {
            //  CPU 温度
            var linqCpuTemperature = cpuEnumerable
                .Where(s => s.SensorType == SensorType.Temperature)
                .Where(s => s.Name == "Core (Tctl/Tdie)")
                .Where(s => s.Value != null)
                .Select(s => s.Value)
                .FirstOrDefault();
            if (linqCpuTemperature is <= 150)
            {
                return linqCpuTemperature.Value;
            }

            return 0;
        }

        /// <summary>
        /// 获取 CPU 功耗
        /// </summary>
        /// <param name="cpuEnumerable"></param>
        /// <returns></returns>
        private static float FetchCpuPackage(IEnumerable<ISensor> cpuEnumerable)
        {
            //  
            var linqCpuPackage = cpuEnumerable
                .Where(s => s.SensorType == SensorType.Power)
                .Where(s => s.Name == "Package")
                .Where(s => s.Value != null)
                .Select(s => s.Value)
                .FirstOrDefault();
            if (linqCpuPackage is <= 1000)
            {
                return linqCpuPackage.Value;
            }

            return 0;
        }


        /// <summary>
        /// 获取 CPU 平均频率
        /// </summary>
        /// <param name="cpuEnumerable"></param>
        /// <returns></returns>
        private static float FetchCpuFreq(IEnumerable<ISensor> cpuEnumerable)
        {
            var cpuSensorList = cpuEnumerable.ToList();
            var cpuCount = Environment.ProcessorCount;

            float tmpTotal = 0;
            var tmpCount = 0;

            for (var i = 1; i <= cpuCount; i++)
            {
                var index = i;
                var linqCpuFreq = cpuSensorList
                    .Where(s => s.SensorType == SensorType.Clock)
                    .Where(s => s.Name == $"Core #{index}")
                    .Where(s => s.Value != null)
                    .Select(s => s.Value)
                    .FirstOrDefault();
                if (linqCpuFreq is > 100)
                {
                    tmpTotal += linqCpuFreq.Value;
                    tmpCount++;
                }
            }

            if (tmpCount <= 0)
            {
                return 0;
            }

            return tmpTotal / tmpCount;
        }
    }
}
