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
        private static bool _ownsInstanceMutex;

        // 同步锁，保护 _instanceMutex 和 _ownsInstanceMutex 的并发访问
        private static readonly object _mutexGuard = new();

        /// <summary>
        /// 释放单例 Mutex，供重启时调用（新进程启动前释放，避免冲突）
        /// </summary>
        public static void ReleaseInstanceMutex()
        {
            Mutex? mutex;
            bool owns;
            lock (_mutexGuard)
            {
                mutex = _instanceMutex;
                owns = _ownsInstanceMutex;
                _instanceMutex = null;
                _ownsInstanceMutex = false;
            }

            if (mutex == null)
                return;

            if (owns)
            {
                try
                {
                    mutex.ReleaseMutex();
                }
                catch (ObjectDisposedException ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"[Program.ReleaseInstanceMutex] Mutex already disposed (ReleaseMutex): {ex.Message}");
                }
                catch (Exception ex)
                {
                    // ReleaseMutex 要求调用线程拥有 Mutex 所有权。
                    // 从异常处理线程调用时可能不拥有所有权，跳过释放，由 OS 自动清理。
                    System.Diagnostics.Trace.WriteLine(
                        $"[Program.ReleaseInstanceMutex] ReleaseMutex failed: {ex.Message}");
                }
            }

            try
            {
                mutex.Dispose();
            }
            catch (ObjectDisposedException ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[Program.ReleaseInstanceMutex] Mutex already disposed (Dispose): {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[Program.ReleaseInstanceMutex] Mutex.Dispose failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新获取 Mutex（Process.Start 失败后恢复单例保护）
        /// </summary>
        public static bool TryReacquireInstanceMutex()
        {
            try
            {
                var mutex = new Mutex(true, InstanceMutexName, out var isFirst);
                lock (_mutexGuard)
                {
                    _instanceMutex = mutex;
                    _ownsInstanceMutex = isFirst;
                }
                return isFirst;
            }
            catch (Exception ex)
            {
                TryLogError($"重新获取单例 Mutex 失败: {ex.Message}");
                return false;
            }
        }

        [STAThread]
        private static void Main()
        {
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // 使用 Mutex 进行单例检测，支持重启场景
                // 注意：当前一个进程异常退出时，Mutex 会处于废弃状态，
                // 此时构造函数可能抛出 AbandonedMutexException。
                var checkMutex = new Mutex(true, InstanceMutexName, out var isFirstInstance);
                if (!isFirstInstance)
                {
                    checkMutex.Dispose();
                    throw new InvalidOperationException(
                        Properties.Strings.TextExceptionOnlyOneProgramIsAllowedToRun);
                }

                lock (_mutexGuard)
                {
                    _instanceMutex = checkMutex;
                    _ownsInstanceMutex = true;
                }

                StartApplication();
            }
            catch (AbandonedMutexException ame)
            {
                // 前一个进程异常退出导致 Mutex 废弃，尝试恢复
                if (TryHandleAbandonedMutex(ame))
                {
                    StartApplication();
                }
            }
            catch (DllNotFoundException ex)
            {
                HandleStartupException(ex);
            }
            catch (EntryPointNotFoundException ex)
            {
                HandleStartupException(ex);
            }
            catch (InvalidOperationException ex)
            {
                HandleStartupException(ex);
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
                HandleStartupException(ex);
            }
            finally
            {
                CleanupResources();
            }
        }

        /// <summary>
        /// 处理废弃的 Mutex（前一个进程未正常释放时发生），尝试重新获取所有权。
        /// </summary>
        private static bool TryHandleAbandonedMutex(AbandonedMutexException ame)
        {
            var abandonedMutex = ame.Mutex;
            if (abandonedMutex != null)
            {
                try
                {
                    abandonedMutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"[Program.TryHandleAbandonedMutex] ReleaseMutex failed: {ex.Message}");
                }

                try
                {
                    abandonedMutex.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"[Program.TryHandleAbandonedMutex] Dispose failed: {ex.Message}");
                }
            }

            // 此时线程不再拥有 Mutex，标记 _ownsInstanceMutex = false。
            lock (_mutexGuard)
            {
                _ownsInstanceMutex = false;
            }

            const int maxRetries = 3;
            for (var retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    var firstMutex = AcquireInstanceMutex(out var isFirstInstance);
                    if (isFirstInstance)
                    {
                        return true;
                    }

                    // 另一个进程在我们释放废弃 mutex 和创建新 mutex
                    // 之间抢先获取了所有权。此时尝试通过 WaitOne(0) 获取非阻塞所有权。
                    try
                    {
                        if (!firstMutex.WaitOne(0))
                        {
                            // 无法获取所有权 — 另一个实例已在运行
                            lock (_mutexGuard)
                            {
                                _instanceMutex = null;
                                _ownsInstanceMutex = false;
                            }
                            firstMutex.Dispose();
                            HandleStartupException(new InvalidOperationException(
                                Properties.Strings.TextExceptionOnlyOneProgramIsAllowedToRun));
                            // NotReached: HandleStartupException 内部调用 ShowErrorAndExit -> Environment.Exit(1)，
                            // 不会返回。保留 return false 以防未来行为变更。
                            return false;
                        }

                        // WaitOne(0) 成功，线程已拥有 Mutex 所有权
                        lock (_mutexGuard)
                        {
                            _ownsInstanceMutex = true;
                        }
                        return true;
                    }
                    catch (AbandonedMutexException)
                    {
                        // WaitOne(0) 在废弃 mutex 上抛出 AbandonedMutexException，
                        // 但同时也授予了所有权，因此视为成功获取
                        lock (_mutexGuard)
                        {
                            _ownsInstanceMutex = true;
                        }
                        // 线程已通过 WaitOne(0) 继承 Mutex 所有权，无需释放或关闭。
                        // 进程正常退出时 ReleaseInstanceMutex 会处理所有权释放。
                        return true;
                    }
                }
                catch (AbandonedMutexException ame2)
                {
                    // 双重废弃竞争：AcquireInstanceMutex 再次遇到废弃 mutex。
                    // 释放 Mutex 后重试。
                    var retryMutex = ame2.Mutex;
                    if (retryMutex != null)
                    {
                        try
                        {
                            retryMutex.ReleaseMutex();
                        }
                        catch (Exception releaseEx)
                        {
                            System.Diagnostics.Trace.WriteLine(
                                $"[Program.TryHandleAbandonedMutex] ReleaseMutex failed on retry: {releaseEx.Message}");
                        }

                        try
                        {
                            retryMutex.Dispose();
                        }
                        catch (Exception releaseEx)
                        {
                            System.Diagnostics.Trace.WriteLine(
                                $"[Program.TryHandleAbandonedMutex] Dispose failed on retry: {releaseEx.Message}");
                        }
                    }

                    lock (_mutexGuard)
                    {
                        _instanceMutex = null;
                        _ownsInstanceMutex = false;
                    }

                    System.Diagnostics.Trace.WriteLine(
                        $"[Program.TryHandleAbandonedMutex] Retry {retry + 1}/{maxRetries}: abandoned mutex on acquire");
                }
            }

            // 所有重试均已耗尽，视为单例冲突
            HandleStartupException(new InvalidOperationException(
                Properties.Strings.TextExceptionOnlyOneProgramIsAllowedToRun));
            return false;
        }

        /// <summary>
        /// 执行应用程序初始化并启动主窗口。
        /// </summary>
        private static void StartApplication()
        {
            AppSettings.Initialize();
            AutoSelectLanguage();
            AppContainer.InitLogger();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new MainForm())
            {
                Application.Run(form);
            }
        }

        /// <summary>
        /// 清理实例互斥锁和容器资源。
        /// </summary>
        private static void CleanupResources()
        {
            ReleaseInstanceMutex();
            try
            {
                AppContainer.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[Program.CleanupResources] AppContainer.Dispose failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理启动过程中的一般异常。
        /// 注意：ShowErrorAndExit 内部调用 Environment.Exit(1) 终止进程，
        /// 因此 Main() 的 finally 块可能不会执行。此处显式释放资源。
        /// </summary>
        private static void HandleStartupException(Exception ex)
        {
            // Environment.Exit() 可能会跳过 Main() 的 finally 块，
            // 因此在显示错误消息前主动释放互斥锁和容器资源。
            CleanupResources();

            if (HasException<DllNotFoundException>(ex))
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

        /// <summary>
        /// 创建单例检测 Mutex 并判断是否为第一个实例。
        /// </summary>
        private static Mutex AcquireInstanceMutex(out bool isFirstInstance)
        {
            var mutex = new Mutex(true, InstanceMutexName, out isFirstInstance);
            lock (_mutexGuard)
            {
                _instanceMutex = mutex;
                _ownsInstanceMutex = isFirstInstance;
            }
            return mutex;
        }

        /// <summary>
        /// 显示错误消息后退出进程。
        /// Environment.Exit() 会跳过 Main() 的 finally 块，因此调用方
        /// 必须在调用此方法前自行释放资源。
        /// </summary>
        private static void ShowErrorAndExit(string title, string message)
        {
            MessageBox.Show(message, title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            Environment.Exit(1);
        }

        /// <summary>
        /// 选择 UI 语言：优先使用用户设置，否则根据系统语言自动选择
        /// </summary>
        private static void AutoSelectLanguage()
        {
            // 优先使用用户设置的语言
            var langCode = AppSettings.Get("Language", "");
            if (string.IsNullOrEmpty(langCode))
            {
                langCode = Utils.RyzenTunerUtils.DetectDefaultLanguageCode();
            }

            // 防御：如果检测结果仍为空，默认使用英语
            if (string.IsNullOrEmpty(langCode))
            {
                langCode = "en";
            }

            CultureInfo culture;
            try
            {
                culture = new CultureInfo(langCode);
            }
            catch (CultureNotFoundException)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[Program.AutoSelectLanguage] Invalid language code '{langCode}', falling back to 'en'");
                culture = new CultureInfo("en");
            }

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
            if (arg.ExceptionObject is Exception e)
            {
                HandleUnhandledException(e);
            }
        }

        private static void HandleUnhandledException(Exception ex)
        {
            try
            {
                AppContainer.Logger()?.LogException(ex);
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[Program.HandleUnhandledException] LogException failed: {logEx.Message}");
            }

            try
            {
                MessageBox.Show(ex.Message, Properties.Strings.TextExceptionTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception msgEx)
            {
                // UI 线程外 MessageBox 可能抛出 InvalidOperationException
                System.Diagnostics.Trace.WriteLine(
                    $"[Program.HandleUnhandledException] MessageBox failed: {msgEx.Message}");
            }

            // 异常处理路径：Main 的 finally 块可能不会执行（Application.Exit 后进程退出），
            // 此处显式清理以避免资源泄漏。
            CleanupResources();

            // 注意：在 CurrentDomain_UnhandledException 中 Application.Exit() 不可靠，
            // 直接使用 Environment.Exit() 终止进程。
            Environment.Exit(1);
        }

        private static void TryLogError(string message)
        {
            try
            {
                AppContainer.Logger()?.Error("System", message);
            }
            catch (Exception ex)
            {
                // 日志异常不影响主流程
                System.Diagnostics.Trace.WriteLine($"[Program.TryLogError] Logger call failed: {ex.Message}");
            }
        }

        private static bool HasException<T>(Exception ex) where T : Exception
        {
            const int maxDepth = 100;
            var current = ex;
            var depth = 0;
            while (current != null && depth < maxDepth)
            {
                if (current is T)
                {
                    return true;
                }

                current = current.InnerException;
                depth++;
            }

            return false;
        }

        /// <summary>
        /// 判断异常是否为不可恢复的严重异常，此类异常应传播而非被吞没。
        /// </summary>
        private static bool IsCriticalException(Exception ex)
        {
            return ex is OutOfMemoryException or AccessViolationException;
        }

    }
}
