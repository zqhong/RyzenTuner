using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace RyzenTuner
{
    public class HardwareMonitor
    {
        class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                // Triggers the "IElement.Accept" method with the given visitor for each device in each group.
                computer.Traverse(this);
            }

            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
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
            // Triggers the IVisitor.VisitComputer method for the given observer.
            _computer.Accept(new UpdateVisitor());

            // CPU
            var hardwareCpu = _computer
                .Hardware
                .Where(i => i.Name.StartsWith("AMD Ryzen"))
                .SelectMany(s => s.Sensors);
            var cpuEnumerable = hardwareCpu.ToList();

            var linqCpuUsage = cpuEnumerable
                .Where(s => s.SensorType == SensorType.Load)
                .Where(s => s.Name == "CPU Total")
                .Where(s => s.Value != null)
                .Select(s => s.Value)
                .First();
            if (linqCpuUsage != null)
            {
                _cpuUsage = linqCpuUsage.Value;
            }

            var linqCpuPackage = cpuEnumerable
                .Where(s => s.SensorType == SensorType.Power)
                .Where(s => s.Name == "Package")
                .Where(s => s.Value != null)
                .Select(s => s.Value)
                .First();
            if (linqCpuPackage != null)
            {
                _cpuPackagePower = linqCpuPackage.Value;
            }
            
            var linqCpuTemperature = cpuEnumerable
                .Where(s => s.SensorType == SensorType.Temperature)
                .Where(s => s.Name == "Core (Tctl/Tdie)")
                .Where(s => s.Value != null)
                .Select(s => s.Value)
                .First();
            if (linqCpuTemperature != null)
            {
                _cpuTemperature = linqCpuTemperature.Value;
            }

            // 显卡
            var hardwareVideoCard = _computer
                .Hardware
                .Where(i => i.Name.EndsWith(" Graphics"))
                .SelectMany(s => s.Sensors);
            var videoCardList = hardwareVideoCard.ToList();

            var linqVideoCard3D = videoCardList
                .Where(s => s.SensorType == SensorType.Load)
                .Where(s => s.Name == "D3D 3D")
                .Where(s => s.Value != null)
                .Select(s => s.Value)
                .First();
            if (linqVideoCard3D != null)
            {
                _videoCard3DUsage = linqVideoCard3D.Value;
            }
        }
    }
}