using System;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace RyzenTuner.Common.Hardware
{
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            // Triggers the "IElement.Accept" method with the given visitor for each device in each group.
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            try
            {
                hardware.Update();
            }
            catch (Exception ex)
            {
                // Isolate a single device failure so it doesn't abort the entire sensor refresh cycle.
                System.Diagnostics.Debug.WriteLine(
                    $"[UpdateVisitor] hardware.Update() failed for {hardware.Name}: {ex.Message}");
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            // SubHardware may be null per the IHardware interface contract.
            if (hardware.SubHardware != null)
            {
                foreach (var subHardware in hardware.SubHardware)
                {
                    try
                    {
                        subHardware.Accept(this);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[UpdateVisitor] sub-hardware Accept failed for {subHardware.Name}: {ex.Message}");
                    }
                }
            }
        }

        // No-op: sensor/parameter data is refreshed implicitly by hardware.Update().
        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}