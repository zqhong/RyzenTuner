using System;
using System.IO;
using RyzenTuner.Utils;

namespace RyzenTuner.Common
{
    public static class Logger
    {
        public static void Debug(string content)
        {
            if (AppUtils.IsDebug())
            {
                LogToFile(AppUtils.ModeDebug, content);
            }
        }

        private static void LogToFile(string mode, string content)
        {
            mode = mode.ToUpper();

            var filePath = AppDomain.CurrentDomain.BaseDirectory + "\\runtime\\ryzen-tuner.log.txt";
            File.AppendAllText(filePath,
                $@"[{mode}]{DateTime.Now:yyyy-MM-dd hh:mm:ss} {content}{Environment.NewLine}");
        }
    }
}