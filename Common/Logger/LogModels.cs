using System;

namespace RyzenTuner.Common.Logger
{
    /// <summary>
    /// Structured log entry for SQLite logging.
    /// Timestamp must be UTC; the cleanup query relies on string-ordinal
    /// comparison of ISO 8601 UTC timestamps and would misbehave with local time.
    /// </summary>
    public sealed class LogEntry
    {
        private DateTime _timestamp;
        private string _action = "";
        private string _details = "";
        private const string ActionNullError = nameof(Action) + " cannot be null";
        private const string DetailsNullError = nameof(Details) + " cannot be null";

        /// <summary>UTC timestamp of the log entry. Normalized to UTC on assignment; Unspecified kind is treated as UTC.</summary>
        public DateTime Timestamp
        {
            get => _timestamp;
            set => _timestamp = value.Kind switch
                {
                    DateTimeKind.Utc => value,
                    DateTimeKind.Local => value.ToUniversalTime(),
                    _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                };
        }

        /// <summary>Trace/Debug/Info/Warning/Error/Fatal</summary>
        public LogLevel Level { get; set; } = LogLevel.Info;

        /// <summary>操作类别（Auto Switch, Power Limit, etc.）</summary>
        public string Action
        {
            get => _action;
            set => _action = value ?? throw new ArgumentNullException(nameof(value), ActionNullError);
        }

        /// <summary>详细消息</summary>
        public string Details
        {
            get => _details;
            set => _details = value ?? throw new ArgumentNullException(nameof(value), DetailsNullError);
        }

        /// <summary>耗时（毫秒），可选</summary>
        public long? ElapsedMilliseconds { get; set; }

        public LogEntry()
        {
            _timestamp = DateTime.UtcNow;
        }
    }
}
