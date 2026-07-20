using System;
using System.Drawing;

namespace RyzenTuner.Utils
{
    public static class CommonUtils
    {
        /// <summary>
        /// 检查字体是否存在
        /// </summary>
        public static bool IsFontExists(string fontName)
        {
            try
            {
                const float fontSize = 12;

                using var fontTester = new Font(
                    fontName,
                    fontSize,
                    FontStyle.Regular,
                    GraphicsUnit.Pixel);

                return fontTester.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
