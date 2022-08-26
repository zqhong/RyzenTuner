using System;
using System.Diagnostics;
using System.Globalization;
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

            AutoSelectLang();

            if (Process.GetProcessesByName("RyzenTuner").Length > 1)
            {
                throw new Exception(Properties.Strings.TextExceptionOnlyOneProgramIsAllowedToRun);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        /// <summary>
        /// 根据用户系统语言，自动选择合适的语言
        /// </summary>
        private static void AutoSelectLang()
        {
            var currentCulture = Thread.CurrentThread.CurrentCulture;
            
            // 非中文环境全部切换为英文
            if (!currentCulture.ToString().StartsWith("zh-"))
            {
                var culture = new CultureInfo("en-US");
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
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
            MessageBox.Show(ex.Message, Properties.Strings.TextExceptionTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            AppContainer.Logger().LogException(ex);

            Application.Exit();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}