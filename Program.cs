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
            try
            {
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDPIAware();
                }

                AutoSelectLang();

                var runningProcesses = Process.GetProcessesByName("RyzenTuner");
                try
                {
                    if (runningProcesses.Length > 1)
                    {
                        throw new Exception(Properties.Strings.TextExceptionOnlyOneProgramIsAllowedToRun);
                    }
                }
                finally
                {
                    foreach (var process in runningProcesses)
                    {
                        process.Dispose();
                    }
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                AppContainer.Dispose();
            }
            catch (Exception ex)
            {
                // 检查是否是初始化失败导致的异常
                if (HasException<DllNotFoundException>(ex) ||
                    ex.Message.Contains("libryzenadj.dll"))
                {
                    ShowErrorAndExit("Critical Error", 
                        "libryzenadj.dll not found! This component is required.\n\n" +
                        "Please download it from the official repository: " +
                        "https://github.com/FlyGoat/RyzenAdj");
                }
                else if (HasException<EntryPointNotFoundException>(ex))
                {
                    ShowErrorAndExit("Critical Error",
                        "libryzenadj.dll is too old for this version of RyzenTuner.\n\n" +
                        "Please update libryzenadj.dll to v0.18.0 or newer.");
                }
                else
                {
                    ShowErrorAndExit("Fatal Error", $"Unhandled exception: {ex.Message}");
                }
            }
        }
        
        // 显示错误并退出的辅助方法
        static void ShowErrorAndExit(string title, string message)
        {
            MessageBox.Show(message, title, 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppContainer.Dispose();
            Environment.Exit(1);
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
            AppContainer.Dispose();

            Application.Exit();
        }

        private static bool HasException<T>(Exception ex) where T : Exception
        {
            while (ex != null)
            {
                if (ex is T)
                {
                    return true;
                }

                ex = ex.InnerException;
            }

            return false;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}
