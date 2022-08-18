using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Common.Hardware
{
    public class HardwareMonitor
    {
        private float _cpuUsage;
        private float _cpuPackagePower;
        private float _cpuTemperature;
        private float _cpuFreq;

        private float _videoCard3DUsage;

        private readonly Computer _computer;

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
            _computer.Close();
        }

        public float CpuUsage => _cpuUsage;

        public float CpuPackagePower => _cpuPackagePower;

        public float CpuTemperature => _cpuTemperature;
        public float CpuFreq => _cpuFreq;

        public float VideoCard3DUsage => _videoCard3DUsage;

        private void Update()
        {
            // Triggers the IVisitor.VisitComputer method for the given observer.
            _computer.Accept(new UpdateVisitor());
        }

        /// <summary>
        /// 获取 CPU 传感器信息
        ///
        /// CPU 名称示例：
        ///     AMD Ryzen 7 PRO 6850HS with Radeon Graphics
        ///     AMD Ryzen 7 4800H with Radeon Graphics
        /// 
        /// 备注：如果有多个 CPU，可能会有问题。可忽略，普通电脑一般只有一个 CPU 插槽
        /// </summary>
        /// <returns></returns>
        private List<ISensor> FetchHardwareCpu()
        {
            var hardwareCpu = _computer
                .Hardware
                .Where(i => i.HardwareType == HardwareType.Cpu)
                .SelectMany(s => s.Sensors);
            var cpuEnumerable = hardwareCpu.ToList();

            return cpuEnumerable;
        }

        /// <summary>
        /// 获取核心显卡的传感器信息
        ///
        /// 显卡名称示例：AMD Radeon(TM) Graphics
        /// 备注：如果是 AMD 核心显卡 + AMD 独立显卡，可能会有问题
        /// </summary>
        /// <returns></returns>
        private List<ISensor> FetchHardwareVideoCard()
        {
            var hardwareVideoCard = _computer
                .Hardware
                .Where(i => i.HardwareType == HardwareType.GpuAmd)
                .SelectMany(s => s.Sensors);
            var videoCardList = hardwareVideoCard.ToList();

            return videoCardList;
        }

        public void Monitor()
        {
            try
            {
                Update();

                var cpuSensorList = FetchHardwareCpu();
                _cpuUsage = FetchCpuUsage(cpuSensorList);
                _cpuPackagePower = FetchCpuPackage(cpuSensorList);
                _cpuTemperature = FetchCpuTemperature(cpuSensorList);
                _cpuFreq = FetchCpuFreq(cpuSensorList);

                var videoCardSensorList = FetchHardwareVideoCard();
                _videoCard3DUsage = FetchVideoCard3DUsage(videoCardSensorList);
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
        }

        private float FetchVideoCard3DUsage(IEnumerable<ISensor> videoCardSensorList)
        {
            var linqVideoCard3D = videoCardSensorList
                .Where(s => s.SensorType == SensorType.Load)
                .Where(s => s.Name == "D3D 3D")
                .Where(s => s.Value != null)
                .Select(s => s.Value)
                .FirstOrDefault();
            if (linqVideoCard3D is <= 100)
            {
                return linqVideoCard3D.Value;
            }

            return 0;
        }

        private float FetchCpuTemperature(IEnumerable<ISensor> cpuEnumerable)
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
        private float FetchCpuPackage(IEnumerable<ISensor> cpuEnumerable)
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
        /// 获取 CPU 占用率
        /// </summary>
        /// <param name="cpuEnumerable"></param>
        /// <returns></returns>
        private static float FetchCpuUsage(IEnumerable<ISensor> cpuEnumerable)
        {
            var linqCpuUsage = cpuEnumerable
                .Where(s => s.SensorType == SensorType.Load)
                .Where(s => s.Name == "CPU Total")
                .Where(s => s.Value != null)
                .Select(s => s.Value)
                .FirstOrDefault();

            if (linqCpuUsage is <= 100)
            {
                return linqCpuUsage.Value;
            }

            return 0;
        }

        /// <summary>
        /// 获取 CPU 平均频率
        /// </summary>
        /// <param name="cpuEnumerable"></param>
        /// <returns></returns>
        private float FetchCpuFreq(IEnumerable<ISensor> cpuEnumerable)
        {
            var cpuCount = Environment.ProcessorCount;

            float tmpTotal = 0;
            var tmpCount = 0;

            for (var i = 1; i <= cpuCount; i++)
            {
                var linqCpuFreq = cpuEnumerable
                    .Where(s => s.SensorType == SensorType.Clock)
                    .Where(s => s.Name == $"Core #{i}")
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