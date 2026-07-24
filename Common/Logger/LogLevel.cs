namespace RyzenTuner.Common.Logger
{
    /// <summary>
    /// Shared log severity levels used by all loggers in the application.
    /// Severity increases with the numeric value:
    /// <c>Trace</c> (0) is the most verbose / least severe;
    /// <c>Fatal</c> (5) is the least verbose / most severe;
    /// <c>None</c> (6) disables all logging.
    /// Levels below the configured <c>DefaultLogLevel</c> are filtered out.
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug,
        Info,
        Warning,
        Error,
        Fatal,

        /// <summary>
        /// Disables all logging. Must have the highest value so that
        /// <c>level &lt; DefaultLogLevel</c> filters out all entries.
        /// </summary>
        None = 6
    }
}
