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
                // Logger 可能在容器释放后不可用，隔离日志异常防止传播
                // 参考: Program.cs 中的日志调用保护模式
                try
                {
                    AppContainer.Logger().Warning("CommonUtils",
                        $"FontExists check failed for '{fontName}': {ex.Message}");
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[CommonUtils] FontExists check failed for '{fontName}': {ex.Message}");
                }

                return false;
            }
        }
    }
}
