using System;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Common
{
    public class HardwareMonitor
    {
        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                // Triggers the "IElement.Accept" method with the given visitor for each device in each group.
                computer.Traverse(this);
            }

            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (var subHardware in hardware.SubHardware) subHardware.Accept(this);
            }

            public void VisitSensor(ISensor sensor)
            {
            }

            public void VisitParameter(IParameter parameter)
            {
            }
        }

        private float _cpuUsage;
        private float _cpuPackagePower;
        private float _cpuTemperature;

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

        public float VideoCard3DUsage => _videoCard3DUsage;

        public void Monitor()
        {
            try
            {
                // Triggers the IVisitor.VisitComputer method for the given observer.
                _computer.Accept(new UpdateVisitor());

                // CPU
                // 示例：
                //      AMD Ryzen 7 PRO 6850HS with Radeon Graphics
                //      AMD Ryzen 7 4800H with Radeon Graphics
                // 备注：如果有多个 CPU，可能会有问题。可忽略，普通电脑一般只有一个 CPU 插槽
                var hardwareCpu = _computer
                    .Hardware
                    .Where(i => i.HardwareType == HardwareType.Cpu)
                    .SelectMany(s => s.Sensors);
                var cpuEnumerable = hardwareCpu.ToList();

                var linqCpuUsage = cpuEnumerable
                    .Where(s => s.SensorType == SensorType.Load)
                    .Where(s => s.Name == "CPU Total")
                    .Where(s => s.Value != null)
                    .Select(s => s.Value)
                    .First();
                if (linqCpuUsage is <= 100)
                {
                    _cpuUsage = linqCpuUsage.Value;
                }

                var linqCpuPackage = cpuEnumerable
                    .Where(s => s.SensorType == SensorType.Power)
                    .Where(s => s.Name == "Package")
                    .Where(s => s.Value != null)
                    .Select(s => s.Value)
                    .First();
                if (linqCpuPackage is <= 1000)
                {
                    _cpuPackagePower = linqCpuPackage.Value;
                }

                var linqCpuTemperature = cpuEnumerable
                    .Where(s => s.SensorType == SensorType.Temperature)
                    .Where(s => s.Name == "Core (Tctl/Tdie)")
                    .Where(s => s.Value != null)
                    .Select(s => s.Value)
                    .First();
                if (linqCpuTemperature is <= 150)
                {
                    _cpuTemperature = linqCpuTemperature.Value;
                }

                // 核心显卡
                // 示例：AMD Radeon(TM) Graphics
                // 备注：如果是 AMD 核心显卡 + AMD 独立显卡，可能会有问题
                var hardwareVideoCard = _computer
                    .Hardware
                    .Where(i => i.HardwareType == HardwareType.GpuAmd)
                    .SelectMany(s => s.Sensors);
                var videoCardList = hardwareVideoCard.ToList();

                var linqVideoCard3D = videoCardList
                    .Where(s => s.SensorType == SensorType.Load)
                    .Where(s => s.Name == "D3D 3D")
                    .Where(s => s.Value != null)
                    .Select(s => s.Value)
                    .First();
                if (linqVideoCard3D is <= 100)
                {
                    _videoCard3DUsage = linqVideoCard3D.Value;
                }
            }
            catch (Exception e)
            {
                AppContainer.Logger().Warning(e.Message);
            }
        }
    }
}