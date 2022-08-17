using System.Runtime.CompilerServices;
using RyzenTuner.Properties;

namespace RyzenTuner.Utils
{
    public static class AppUtils
    {
        public const string ModeDebug = "Debug";
        public const string ModeProduction = "Production";

        public static bool IsDebug()
        {
            return Settings.Default.RunMode == ModeDebug;
        }

        public static bool IsProduction()
        {
            return Settings.Default.RunMode == ModeProduction;
        }
    }
}