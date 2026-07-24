using System;
using LibreHardwareMonitor.Hardware;
using RyzenTuner.Common.Container;
using RyzenTuner.Utils;

namespace RyzenTuner.Common.Hardware
{
    public class UpdateVisitor : IVisitor
    {
        private const string LogTag = nameof(UpdateVisitor);

        /// <summary>
        /// Writes a warning message to the logger. Any logger failure is swallowed so the
        /// original exception context is never masked.
        /// </summary>
        private static void SafeLogWarning(string message)
        {
            try
            {
                AppContainer.Logger().Warning(LogTag, message);
            }
            catch (Exception ex) when (!CommonUtils.IsFatalException(ex))
            {
                // Logger unavailable (e.g., container disposed during shutdown).
                // Swallow to avoid masking the original exception.
            }
        }

        public void VisitComputer(IComputer computer)
        {
            if (computer == null)
                throw new ArgumentNullException(nameof(computer));

            // Triggers the "IElement.Accept" method with the given visitor for each device in each group.
            try
            {
                computer.Traverse(this);
            }
            catch (Exception ex) when (!CommonUtils.IsFatalException(ex))
            {
                SafeLogWarning($"computer.Traverse failed: {ex}");
            }
        }

        public void VisitHardware(IHardware hardware)
        {
            if (hardware == null)
                throw new ArgumentNullException(nameof(hardware));

            string hardwareName = "(unknown)";
            try
            {
                hardwareName = hardware.Name ?? "(null)";
                hardware.Update();
            }
            catch (Exception ex) when (!CommonUtils.IsFatalException(ex))
            {
                // Isolate a single device failure so it doesn't abort the entire sensor refresh cycle.
                // hardwareName is "(unknown)" only when hardware.Name threw before assignment.
                SafeLogWarning($"hardware.Update() failed for {hardwareName}: {ex}");
            }

            try
            {
                // SubHardware may be null per the IHardware interface contract.
                if (hardware.SubHardware is { } subHardware)
                {
                    foreach (var sub in subHardware)
                    {
                        if (sub == null)
                        {
                            continue;
                        }

                        var subName = sub.Name ?? "(null)";
                        try
                        {
                            sub.Accept(this);
                        }
                        catch (Exception ex) when (!CommonUtils.IsFatalException(ex))
                        {
                            SafeLogWarning($"sub-hardware Accept failed for {subName}: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex) when (!CommonUtils.IsFatalException(ex))
            {
                SafeLogWarning($"SubHardware enumeration failed for {hardwareName}: {ex}");
            }
        }

        /// <summary>
        /// No-op: sensor data is refreshed implicitly by hardware.Update().
        /// This method must exist to satisfy the IVisitor interface.
        /// </summary>
        public void VisitSensor(ISensor sensor)
        {
        }

        /// <summary>
        /// No-op: parameter data is refreshed implicitly by hardware.Update().
        /// This method must exist to satisfy the IVisitor interface.
        /// </summary>
        public void VisitParameter(IParameter parameter)
        {
        }
    }
}