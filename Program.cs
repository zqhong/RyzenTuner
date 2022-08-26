using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using RyzenTuner.Common.Container;
using RyzenTuner.UI;

namespace RyzenTuner
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }
            
            if (Process.GetProcessesByName("RyzenTuner").Length > 1)
            {
                throw new Exception("同一时间内，只允许运行一个 RyzenTuner 程序");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            _handleUnhandledException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs arg)
        {
            var e = (Exception)arg.ExceptionObject;
            _handleUnhandledException(e);
        }

        private static void _handleUnhandledException(Exception ex)
        {
            MessageBox.Show(ex.Message, "RyzenTuner 出现错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            AppContainer.Logger().LogException(ex);

            Application.Exit();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}