using RyzenTuner.Common.Container;

namespace RyzenTuner.Utils
{
    public class DebugUtils
    {
        public void Test()
        {
            AppContainer.HardwareMonitor().Monitor();
            
            for (var i = 1; i <= 30; i++)
            {
                
            }
        }
    }
}