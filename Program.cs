using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using RyzenTuner.Common.Container;
using RyzenTuner.Common.Settings;
using RyzenTuner.UI;

namespace RyzenTuner
{
    internal static class Program
    {
        private const string InstanceMutexName = "RyzenTuner-InstanceMutex";

        // 用于单例检测的 Mutex（替代 Process.GetProcessesByName，支持 Application.Restart）
        private static Mutex? _instanceMutex;

        // 跟踪当前线程是否拥有 Mutex，避免对未拥有的 Mutex 调用 ReleaseMutex 崩溃
        private static volatile bool _ownsInstanceMutex;

        /// <summary>
        /// 释放单例 Mutex，供重启时调用（新进程启动前释放，避免冲突）
        /// </summary>
        public static void ReleaseInstanceMutex()
        {
            if (!_ownsInstanceMutex)
            {
                return;
            }

            var mutex = Interlocked.Exchange(ref _instanceMutex, null);
            if (mutex == null)
            {
                return;
            }

            try
            {
                mutex.ReleaseMutex();
            }
            catch
            {
                // ReleaseMutex 是线程关联的：只有创建线程（Main 线程）才能释放。
                // 从异常处理线程调用时跳过释放，由进程退出自动清理。
            }
            finally
            {
                mutex.Close();
                _ownsInstanceMutex = false;
            }
        }

        /// <summary>
        /// 重新获取 Mutex（Process.Start 失败后恢复单例保护）
        /// </summary>
        public static bool TryReacquireInstanceMutex()
        {
            try
            {
                _instanceMutex = new Mutex(true, InstanceMutexName, out var isFirst);
                _ownsInstanceMutex = isFirst;
                return isFirst;
            }
            catch (Exception ex)
            {
                try { AppContainer.Logger()?.Error("System", $"重新获取单例 Mutex 失败: {ex.Message}"); } catch { /* 日志异常不影响流程 */ }
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

                // 使用 Mutex 进行单例检测，支持重启场景
                _instanceMutex = new Mutex(true, InstanceMutexName, out var isFirstInstance);
                _ownsInstanceMutex = isFirstInstance;
                if (!isFirstInstance)
                {
                    throw new InvalidOperationException(Properties.Strings.TextExceptionOnlyOneProgramIsAllowedToRun);
                }

                // 在 Mutex 保护下初始化数据库和设置（确保不会与并发实例冲突）
                AppSettings.Initialize();
                AutoSelectLang();

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
        private static void ShowErrorAndExit(string title, string message)
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
            var userLang = AppSettings.Get("Language", "");
            if (!string.IsNullOrEmpty(userLang))
            {
                langCode = userLang;
            }
            else
            {
                langCode = Utils.RyzenTunerUtils.DetectDefaultLanguageCode();
            }

            var culture = new CultureInfo(langCode);
            // 设置当前线程语言（立即生效，确保 ResourceManager 使用正确语言）
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            // 设置默认语言（新线程生效）
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs arg)
        {
            var e = (Exception)arg.ExceptionObject;
            HandleUnhandledException(e);
        }

        private static void HandleUnhandledException(Exception ex)
        {
            ReleaseInstanceMutex();
            MessageBox.Show(ex.Message, Properties.Strings.TextExceptionTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            try { AppContainer.Logger()?.LogException(ex); } catch { /* 日志异常不掩盖原始错误 */ }
            AppContainer.Dispose();

            Application.Exit();
        }

        private static bool HasException<T>(Exception ex) where T : Exception
        {
            var current = ex;
            while (current != null)
            {
                if (current is T)
                {
                    return true;
                }

                current = current.InnerException;
            }

            return false;
        }

    }
}
