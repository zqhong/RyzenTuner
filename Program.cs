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
        // 用于单例检测的 Mutex（替代 Process.GetProcessesByName，支持 Application.Restart）
        private static Mutex? _instanceMutex;

        /// <summary>
        /// 释放单例 Mutex，供重启时调用（新进程启动前释放，避免冲突）
        /// </summary>
        public static void ReleaseInstanceMutex()
        {
            if (_instanceMutex != null)
            {
                _instanceMutex.ReleaseMutex();
                _instanceMutex.Close();
                _instanceMutex = null;
            }
        }

        /// <summary>
        /// 重新获取 Mutex（Process.Start 失败后恢复单例保护）
        /// </summary>
        public static bool TryReacquireInstanceMutex()
        {
            try
            {
                bool isFirst;
                _instanceMutex = new Mutex(true, "RyzenTuner-InstanceMutex", out isFirst);
                return isFirst;
            }
            catch
            {
                return false;
            }
        }

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

                // 使用 Mutex 进行单例检测，支持重启场景
                bool isFirstInstance;
                _instanceMutex = new Mutex(true, "RyzenTuner-InstanceMutex", out isFirstInstance);
                if (!isFirstInstance)
                {
                    throw new Exception(Properties.Strings.TextExceptionOnlyOneProgramIsAllowedToRun);
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                ReleaseInstanceMutex();
                AppContainer.Dispose();
            }
            catch (Exception ex)
            {
                // 检查是否是初始化失败导致的异常
                if (HasException<DllNotFoundException>(ex) ||
                    ex.Message.Contains("libryzenadj.dll"))
                {
                    ShowErrorAndExit(Properties.Strings.TextCriticalError,
                        Properties.Strings.TextLibRyzenAdjMissing);
                }
                else if (HasException<EntryPointNotFoundException>(ex))
                {
                    ShowErrorAndExit(Properties.Strings.TextCriticalError,
                        Properties.Strings.TextLibRyzenAdjTooOld);
                }
                else
                {
                    ShowErrorAndExit(Properties.Strings.TextFatalError,
                        Properties.Strings.TextUnhandledException.Replace("{message}", ex.Message));
                }
            }
        }
        
        // 显示错误并退出的辅助方法
        static void ShowErrorAndExit(string title, string message)
        {
            ReleaseInstanceMutex();
            MessageBox.Show(message, title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppContainer.Dispose();
            Environment.Exit(1);
        }

        /// <summary>
        /// 选择 UI 语言：优先使用用户设置，否则根据系统语言自动选择
        /// </summary>
        private static void AutoSelectLang()
        {
            string langCode;

            // 优先使用用户设置的语言
            var userLang = Properties.Settings.Default.Language;
            if (!string.IsNullOrEmpty(userLang))
            {
                langCode = userLang;
            }
            else
            {
                langCode = RyzenTuner.Utils.RyzenTunerUtils.DetectDefaultLanguageCode();
            }

            var culture = new CultureInfo(langCode);
            // 设置默认语言（新线程生效）
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            // 设置当前线程语言（立即生效，确保 ResourceManager 使用正确语言）
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
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
            ReleaseInstanceMutex();
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
