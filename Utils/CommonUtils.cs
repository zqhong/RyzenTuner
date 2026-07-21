using System;
using System.Drawing;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Utils
{
    public static class CommonUtils
    {
        /// <summary>
        /// 检查字体是否存在
        /// </summary>
        public static bool FontExists(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
            {
                return false;
            }

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
            catch (ArgumentException ex)
            {
                AppContainer.Logger().Warning("CommonUtils", $"FontExists check failed for '{fontName}': {ex.Message}");
                return false;
            }
        }
    }
}
