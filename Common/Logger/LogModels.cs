using System;

namespace RyzenTuner.Common.Logger
{
    /// <summary>
    /// Structured log entry for SQLite logging.
    /// </summary>
    public sealed class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = ""; // Trace/Debug/Info/Warning/Error/Fatal
        public string Action { get; set; } = ""; // 操作类别（Auto Switch, Power Limit, etc.）
        public string Details { get; set; } = ""; // 详细消息
        public long? ElapsedMilliseconds { get; set; } // 耗时（毫秒），可选
    }
}
