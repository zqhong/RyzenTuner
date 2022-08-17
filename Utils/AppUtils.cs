using System.Runtime.CompilerServices;
using RyzenTuner.Properties;

namespace RyzenTuner.Utils
{
    public static class AppUtils
    {
        public const string ModeDebug = "Debug";

        public static bool IsDebug()
        {
            return Settings.Default.RunMode == ModeDebug;
        }
    }
}