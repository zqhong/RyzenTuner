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
        private static int _disposed;

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
                    logger.DefaultLogLevel = logger.ToLogLevel(AppSettings.Get("LogLevel", "Warning"));
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
