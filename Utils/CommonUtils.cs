using System.Drawing;

namespace RyzenTuner.Utils
{
    public static class CommonUtils
    {
        /**
         * 检查字体是否存在
         */
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

                return fontTester.Name == fontName;
            }
            catch
            {
                return false;
            }
        }
    }
}
