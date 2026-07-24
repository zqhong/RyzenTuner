using System;
using System.Threading;
using RyzenTuner.Common.EnergyStar;
using RyzenTuner.Common.Hardware;
using RyzenTuner.Common.Logger;
using RyzenTuner.Common.Processor;
using RyzenTuner.Common.Settings;


namespace RyzenTuner.Common.Container
{
    /// <summary>
    ///     Static service locator for application-wide singleton services.
    ///     Refer: https://csharpindepth.com/articles/singleton#cctor
    /// </summary>
    public static class AppContainer
    {
        // Not readonly; nulled out atomically in Dispose() via Interlocked.Exchange.
        // The null check in Resolve<T>() provides the thread-safe disposal guard.
        private static Container? _container;

        static AppContainer()
        {
            _container = new Container();

            _container.Register(() => new HardwareMonitor())
                .AsSingleton();

            _container.Register(() => new PowerConfig())
                .AsSingleton();

            _container.Register(() => new AmdProcessor())
                .AsSingleton();

            _container.Register(() => new EnergyManager())
                .AsSingleton();

            // Logger 注册：日志级别在 InitLogger() 中延迟初始化（需在 AppSettings.Initialize() 之后调用）
            _container.Register(() => new SqliteLogger())
                .AsSingleton();
        }

        /// <summary>
        /// 初始化 Logger 的日志级别。
        /// 必须在 AppSettings.Initialize() 之后调用。
        /// </summary>
        public static void InitLogger()
        {
            try
            {
                // 先读取设置（不依赖容器），实现 fail-fast：
                // 若 AppSettings 未初始化则立即抛出，避免无谓的容器 resolve。
                var logLevelStr = AppSettings.Get("LogLevel", "Warning");
                var logLevel = SqliteLogger.ToLogLevel(logLevelStr);

                // 再获取 logger 实例并设置级别。
                // 注意：Resolve<SqliteLogger>() 在 Dispose() 之后会抛出 ObjectDisposedException，
                // 但此方法仅在初始化时调用，此时容器尚未释放。外层的 catch 提供了兜底保护。
                // DefaultLogLevel 的 setter 仅写入 volatile 字段，不访问托管资源，
                // 因此即使日志器实例在其他场景下被引用，直接调用 setter 也无须 disposed 检查。
                var logger = Resolve<SqliteLogger>();
                logger.DefaultLogLevel = logLevel;
            }
            catch (Exception ex)
            {
                var msg = $"[AppContainer] InitLogger failed: {ex}";
                System.Diagnostics.Trace.WriteLine(msg);
            }
        }

        /// <summary>Returns the singleton HardwareMonitor instance.</summary>
        /// <exception cref="ObjectDisposedException">The container has been disposed.</exception>
        public static HardwareMonitor HardwareMonitor() => Resolve<HardwareMonitor>();

        /// <summary>Returns the singleton PowerConfig instance.</summary>
        /// <exception cref="ObjectDisposedException">The container has been disposed.</exception>
        public static PowerConfig PowerConfig() => Resolve<PowerConfig>();

        /// <summary>Returns the singleton AmdProcessor instance.</summary>
        /// <exception cref="ObjectDisposedException">The container has been disposed.</exception>
        public static AmdProcessor AmdProcessor() => Resolve<AmdProcessor>();

        /// <summary>Returns the singleton EnergyManager instance.</summary>
        /// <exception cref="ObjectDisposedException">The container has been disposed.</exception>
        public static EnergyManager EnergyManager() => Resolve<EnergyManager>();

        /// <summary>Returns the singleton SqliteLogger instance.</summary>
        /// <exception cref="ObjectDisposedException">The container has been disposed.</exception>
        public static SqliteLogger Logger() => Resolve<SqliteLogger>();

        /// <summary>
        ///     Disposes all singleton services registered in the container.
        ///     Safe to call multiple times from any thread — subsequent calls are no-ops.
        /// </summary>
        public static void Dispose()
        {
            // Atomically take ownership of the container and null the field.
            // After this point, any concurrent Resolve<T>() sees null and
            // throws ObjectDisposedException immediately — no TOCTOU gap.
            var container = Interlocked.Exchange(ref _container, null);
            if (container == null)
                return;

            // 显式控制释放顺序：先释放 Logger，使后续服务在自身 Dispose 中仍可日志记录
            try
            {
                var logger = container.Resolve<SqliteLogger>();
                logger.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[AppContainer] Logger disposal failed: {ex}");
            }

            // 然后释放容器（非确定顺序释放剩余单例）
            container.Dispose();
        }

        /// <summary>
        ///     Resolves a registered singleton service from the container.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The singleton instance of <typeparamref name="T"/>.</returns>
        /// <exception cref="ObjectDisposedException">The container has been disposed.</exception>
        private static T Resolve<T>() where T : class
        {
            // Volatile.Read pairs with the Interlocked.Exchange release-store in
            // Dispose() to provide acquire semantics, guaranteeing this thread
            // sees the null once Dispose() commits it.  Without it, on weakly-
            // ordered hardware the read could return a stale non-null value.
            // The single read also eliminates any TOCTOU window.
            var container = Volatile.Read(ref _container);
            if (container == null)
            {
                throw new ObjectDisposedException(nameof(AppContainer),
                    $"Cannot resolve {typeof(T).Name}: {nameof(AppContainer)} has been disposed.");
            }

            return container.Resolve<T>();
        }

    }
}
