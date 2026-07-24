using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Utils
{
    public static class CommonUtils
    {
        /// <summary>
        /// Determines whether the given exception is a fatal system exception
        /// that should not be caught and swallowed.
        /// </summary>
        public static bool IsFatalException(Exception e) =>
            e is AccessViolationException or StackOverflowException or OutOfMemoryException
                or ThreadAbortException;

        /// <summary>
        /// 检查字体是否存在
        /// </summary>
        public static bool FontExists(string? fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
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

                return string.Equals(fontTester.Name, fontName, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or ExternalException)
            {
                // Logger 可能在容器释放后不可用，隔离日志异常防止传播
                // 参考: Program.cs 中的日志调用保护模式
                try
                {
                    AppContainer.Logger()?.Warning("CommonUtils",
                        $"FontExists check failed for '{fontName}': {ex.Message}");
                }
                catch (Exception innerException) when (
                    innerException is ObjectDisposedException or InvalidOperationException)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"[CommonUtils] FontExists check failed for '{fontName}': " +
                        $"{ex.Message} (logger error: {innerException.Message})");
                }

                return false;
            }
        }
    }
}
