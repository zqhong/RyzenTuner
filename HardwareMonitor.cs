using System;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace RyzenTuner
{
    public class HardwareMonitor
    {
        private float _cpuUsage;
        private float _cpuPackagePower;

        private float _videoCard3DUsage;
        private float _videoCardDecodeUsage;

        class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
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

        public void Monitor()
        {
            var computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false
            };

            computer.Open();
            computer.Accept(new UpdateVisitor());

            // CPU
            var hardwareCpu = computer
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

            // 显卡
            var hardwareVideoCard = computer
                .Hardware
                .Where(i => i.Name.StartsWith(" Graphics"))
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

            var linqVideoCardDecode = videoCardList
                .Where(s => s.SensorType == SensorType.Load)
                .Where(s => s.Name == "D3D Video Decode")
                .Where(s => s.Value != null)
                .Select(s => s.Value)
                .First();
            if (linqVideoCardDecode != null)
            {
                _videoCardDecodeUsage = linqVideoCardDecode.Value;
            }

            // foreach (IHardware hardware in computer.Hardware)
            // {
            //     CommonUtils.LogInfo(String.Format("Hardware: {0}", hardware.Name));
            //
            //     foreach (ISensor sensor in hardware.Sensors)
            //     {
            //         CommonUtils.LogInfo(String.Format("\tSensor[{2}]: {0}, value: {1}", sensor.Name, sensor.Value,
            //             sensor.SensorType));
            //     }
            // }

            CommonUtils.LogInfo(String.Format(@"[Cpu] Usage: {0}, Power: {1}
[Video Card] 3D Usage: {2}, Decode Usage: {3}
",
                _cpuUsage,
                _cpuPackagePower,
                _videoCard3DUsage,
                _videoCardDecodeUsage
            ));

            computer.Close();
        }
    }
}