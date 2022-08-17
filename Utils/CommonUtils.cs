using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace RyzenTuner.Utils
{
    public static class CommonUtils
    {
        /**
         * 检查系统是否处于锁屏状态
         */
        public static bool IsSystemLocked()
        {
            // logonui，即 Windows Logon User Interface Host，翻译为【登录用户界面】
            return Process.GetProcessesByName("logonui").Length > 0;
        }

        /**
         * 检查字体是否存在
         */
        public static bool IsFontExists(string fontName)
        {
            const float fontSize = 12;

            using var fontTester = new Font(
                fontName,
                fontSize,
                FontStyle.Regular,
                GraphicsUnit.Pixel);

            return fontTester.Name == fontName;
        }

        /**
         * 检查提供的日期是否处于晚上
         */
        public static bool IsNight(DateTime now)
        {
            TimeSpan nightShiftStart = new TimeSpan(23, 59, 0); // 23:59pm 
            TimeSpan nightShiftEnd = new TimeSpan(7, 0, 0); // 7:00am

            if (now.TimeOfDay > nightShiftStart || now.TimeOfDay < nightShiftEnd)
            {
                return true;
            }

            return false;
        }
    }
}