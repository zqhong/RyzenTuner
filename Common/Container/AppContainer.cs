using System;
using System.Threading;
using RyzenTuner.Common.EnergyStar;
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
        private static readonly Container Container;
        private static volatile int _disposed;

        static AppContainer()
        {
            Container = new Container();

            Container.Register(() => new Hardware.HardwareMonitor())
                .AsSingleton();

            Container.Register(() => new PowerConfig())
                .AsSingleton();

            Container.Register(() => new AmdProcessor())
                .AsSingleton();

            Container.Register(() => new EnergyManager())
                .AsSingleton();

            Container.Register(() =>
                {
                    var logger = new SqliteLogger();

                    // 安全解析日志级别：若设置值无效（例如人为篡改）则静默回退到 Warning，
                    // 防止 ArgumentOutOfRangeException 在静态构造函数中传播导致 TypeInitializationException。
                    try
                    {
                        var logLevelStr = AppSettings.Get("LogLevel", "Warning");
                        logger.DefaultLogLevel = SqliteLogger.ToLogLevel(logLevelStr);
                    }
                    catch
                    {
                        // SqliteLogger 构造函数已将 DefaultLogLevel 初始化为 LogLevel.Warning，
                        // 此处无需额外赋值。
                    }

                    return logger;
                })
                .AsSingleton();
        }

        /// <summary>Returns the singleton HardwareMonitor instance.</summary>
        /// <exception cref="ObjectDisposedException">Thrown if the container has been disposed.</exception>
        public static Hardware.HardwareMonitor HardwareMonitor()
        {
            ThrowIfDisposed();
            return Container.Resolve<Hardware.HardwareMonitor>();
        }

        /// <summary>Returns the singleton PowerConfig instance.</summary>
        /// <exception cref="ObjectDisposedException">Thrown if the container has been disposed.</exception>
        public static PowerConfig PowerConfig()
        {
            ThrowIfDisposed();
            return Container.Resolve<PowerConfig>();
        }

        /// <summary>Returns the singleton AmdProcessor instance.</summary>
        /// <exception cref="ObjectDisposedException">Thrown if the container has been disposed.</exception>
        public static AmdProcessor AmdProcessor()
        {
            ThrowIfDisposed();
            return Container.Resolve<AmdProcessor>();
        }

        /// <summary>Returns the singleton EnergyManager instance.</summary>
        /// <exception cref="ObjectDisposedException">Thrown if the container has been disposed.</exception>
        public static EnergyManager EnergyManager()
        {
            ThrowIfDisposed();
            return Container.Resolve<EnergyManager>();
        }

        /// <summary>Returns the singleton SqliteLogger instance.</summary>
        /// <exception cref="ObjectDisposedException">Thrown if the container has been disposed.</exception>
        public static SqliteLogger Logger()
        {
            ThrowIfDisposed();
            return Container.Resolve<SqliteLogger>();
        }

        /// <summary>
        ///     Disposes all singleton services registered in the container.
        ///     Safe to call multiple times from any thread — subsequent calls are no-ops.
        /// </summary>
        public static void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            Container.Dispose();
        }

        private static void ThrowIfDisposed()
        {
            if (_disposed != 0)
            {
                throw new ObjectDisposedException(nameof(AppContainer));
            }
        }
    }
}
