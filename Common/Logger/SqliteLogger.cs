using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;

using RyzenTuner.Common.Settings;

namespace RyzenTuner.Common.Logger
{
    /// <summary>
    /// SQLite 日志记录器 — 所有日志写入 SQLite 数据库。
    /// 数据库路径和连接字符串通过 SettingsDatabase 共享。
    /// </summary>
    public class SqliteLogger : IDisposable
    {
        public enum LogLevel
        {
            Trace = 0,
            Debug,
            Info,
            Warning,
            Error,
            Fatal
        }

        // 注意：_datetimeFormat 必须保持 "yyyy-MM-dd HH:mm:ss" 格式不变，
        // Cleanup() 中的 DELETE 查询使用字符串字典序比较 timestamp 列，
        // 任何格式变更（如省略前导零、12 小时制）都会导致时间比较失效。
        private readonly string _datetimeFormat;
        private readonly string _dbPath;
        private readonly object _dbLock = new();
        private bool _disposed;
        private bool _dbInitialized;

        public LogLevel DefaultLogLevel;

        public SqliteLogger()
        {
            _datetimeFormat = "yyyy-MM-dd HH:mm:ss";

            // #4, #14: 使用 SettingsDatabase 共享的路径和连接字符串
            _dbPath = SettingsDatabase.GetDbPath();

            DefaultLogLevel = LogLevel.Warning;

            // Initialize SQLite
            InitializeDatabase();
        }

        /// <summary>
        /// 获取 SQLite 数据库文件路径（供日志查看器使用）
        /// </summary>
        public string DbPath => _dbPath;

        /// <summary>
        /// 获取数据库连接字符串（含 Busy Timeout）
        /// </summary>
        private string ConnectionString =>
            SettingsDatabase.GetConnectionString();

        /// <summary>
        /// 初始化数据库和表结构
        /// </summary>
        private void InitializeDatabase()
        {
            try
            {
                // 先测试数据库能否正常打开
                using var testConn = new SQLiteConnection(ConnectionString);
                testConn.Open();

                // 创建表
                using var createCmd = testConn.CreateCommand();
                createCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS logs (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        timestamp TEXT NOT NULL,
                        level TEXT NOT NULL,
                        action TEXT NOT NULL DEFAULT '',
                        details TEXT NOT NULL DEFAULT '',
                        elapsed_ms INTEGER NULL
                    );
                    CREATE INDEX IF NOT EXISTS idx_logs_timestamp ON logs(timestamp);
                    CREATE INDEX IF NOT EXISTS idx_logs_level ON logs(level);
                ";
                createCmd.ExecuteNonQuery();

                testConn.Close();
                _dbInitialized = true;
            }
            catch
            {
                _dbInitialized = false;
            }
        }

        /// <summary>
        /// 创建并返回一个新的数据库连接
        /// </summary>
        private SQLiteConnection CreateConnection()
        {
            var conn = new SQLiteConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            // 释放 SQLite 连接池中的句柄，防止反复重启时句柄残留导致 SQLITE_BUSY
            SQLiteConnection.ClearAllPools();
        }

        public LogLevel ToLogLevel(string logLevel)
        {
            return logLevel switch
            {
                "Trace" => LogLevel.Trace,
                "Debug" => LogLevel.Debug,
                "Info" => LogLevel.Info,
                "Warning" => LogLevel.Warning,
                "Error" => LogLevel.Error,
                "Fatal" => LogLevel.Fatal,
                _ => throw new ArgumentException($"不正确的 log level 类型：{logLevel}")
            };
        }

        // ================================================================
        // 日志方法 — 单字符串重载（兼容现有调用，action 默认 "General"）
        // ================================================================

        public void Debug(string text)
        {
            WriteFormattedLog(LogLevel.Debug, "General", text, null);
        }

        public void Error(string text)
        {
            WriteFormattedLog(LogLevel.Error, "General", text, null);
        }

        public void Fatal(string text)
        {
            WriteFormattedLog(LogLevel.Fatal, "General", text, null);
        }

        public void Info(string text)
        {
            WriteFormattedLog(LogLevel.Info, "General", text, null);
        }

        public void Trace(string text)
        {
            WriteFormattedLog(LogLevel.Trace, "General", text, null);
        }

        public void Warning(string text)
        {
            WriteFormattedLog(LogLevel.Warning, "General", text, null);
        }

        public void LogException(Exception e)
        {
            var st = new StackTrace(e, true);
            var frame = st.GetFrame(0);
            var line = frame?.GetFileLineNumber() ?? 0;

            Warning($"Exception: {e.Message}\nLine: {line}\nStackTrace: {st}");
        }

        // ================================================================
        // 结构化日志重载（含 Action、Details、ElapsedMs）
        // ================================================================

        public void Debug(string action, string details, long? elapsedMs = null)
        {
            WriteFormattedLog(LogLevel.Debug, action, details, elapsedMs);
        }

        public void Error(string action, string details, long? elapsedMs = null)
        {
            WriteFormattedLog(LogLevel.Error, action, details, elapsedMs);
        }

        public void Fatal(string action, string details, long? elapsedMs = null)
        {
            WriteFormattedLog(LogLevel.Fatal, action, details, elapsedMs);
        }

        public void Info(string action, string details, long? elapsedMs = null)
        {
            WriteFormattedLog(LogLevel.Info, action, details, elapsedMs);
        }

        public void Trace(string action, string details, long? elapsedMs = null)
        {
            WriteFormattedLog(LogLevel.Trace, action, details, elapsedMs);
        }

        public void Warning(string action, string details, long? elapsedMs = null)
        {
            WriteFormattedLog(LogLevel.Warning, action, details, elapsedMs);
        }

        // ================================================================
        // 查询方法（供日志查看器使用）
        // ================================================================

        /// <summary>
        /// 查询日志记录
        /// </summary>
        /// <param name="levelFilter">日志级别筛选（null 表示所有级别）</param>
        /// <param name="searchText">搜索关键字（匹配 action 或 details）</param>
        /// <param name="limit">返回条数上限</param>
        public DataTable QueryLogs(string? levelFilter = null, string? searchText = null, int limit = 1000)
        {
            var table = new DataTable();
            table.Columns.Add("timestamp", typeof(string));
            table.Columns.Add("level", typeof(string));
            table.Columns.Add("action", typeof(string));
            table.Columns.Add("details", typeof(string));
            table.Columns.Add("elapsed_ms", typeof(string));

            if (!_dbInitialized)
                return table;

            try
            {
                using var conn = CreateConnection();
                using var cmd = conn.CreateCommand();

                var sql = "SELECT timestamp, level, action, details, elapsed_ms FROM logs WHERE 1=1";

                if (!string.IsNullOrEmpty(levelFilter))
                {
                    sql += " AND level = @level";
                    cmd.Parameters.AddWithValue("@level", levelFilter);
                }

                if (!string.IsNullOrEmpty(searchText))
                {
                    sql += " AND (action LIKE @search OR details LIKE @search)";
                    cmd.Parameters.AddWithValue("@search", $"%{searchText}%");
                }

                sql += " ORDER BY id DESC LIMIT @limit";
                cmd.Parameters.AddWithValue("@limit", limit);
                cmd.CommandText = sql;

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var row = table.NewRow();
                    row["timestamp"] = reader["timestamp"]?.ToString() ?? "";
                    row["level"] = reader["level"]?.ToString() ?? "";
                    row["action"] = reader["action"]?.ToString() ?? "";
                    row["details"] = reader["details"]?.ToString() ?? "";
                    var elapsedMs = reader["elapsed_ms"];
                    row["elapsed_ms"] = elapsedMs == DBNull.Value ? "-" : elapsedMs + " ms";
                    table.Rows.Add(row);
                }

                return table;
            }
            catch
            {
                return table;
            }
        }

        /// <summary>
        /// 获取可用的日志级别列表（用于筛选下拉框，不包含 "All"）
        /// </summary>
        public string[] GetAvailableLevels()
        {
            if (!_dbInitialized)
                return Array.Empty<string>();

            try
            {
                using var conn = CreateConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT DISTINCT level FROM logs ORDER BY level";
                using var reader = cmd.ExecuteReader();

                var levels = new System.Collections.Generic.List<string>();
                while (reader.Read())
                {
                    levels.Add(reader.GetString(0));
                }
                return levels.ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 获取日志总数
        /// </summary>
        public long GetLogCount()
        {
            if (!_dbInitialized)
                return 0;

            try
            {
                using var conn = CreateConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM logs";
                return (long)cmd.ExecuteScalar();
            }
            catch
            {
                return 0;
            }
        }

        // ================================================================
        // 日志清理
        // ================================================================

        /// <summary>
        /// 清空所有日志
        /// </summary>
        public void DeleteAll()
        {
            if (!_dbInitialized)
                return;

            try
            {
                using var conn = CreateConnection();
                lock (_dbLock)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM logs";
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // 删除失败时静默忽略
            }
        }

        /// <summary>
        /// 删除超过指定天数的日志
        /// </summary>
        public void Cleanup(int retentionDays)
        {
            if (!_dbInitialized || retentionDays <= 0)
                return;

            try
            {
                using var conn = CreateConnection();
                lock (_dbLock)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM logs WHERE timestamp < @cutoff";
                    cmd.Parameters.AddWithValue("@cutoff",
                        DateTime.UtcNow.AddDays(-retentionDays).ToString(_datetimeFormat));
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // 删除失败时静默忽略
            }
        }

        // ================================================================
        // 内部方法
        // ================================================================

        /// <summary>
        /// 写入格式化日志（仅 SQLite）
        /// </summary>
        private void WriteFormattedLog(LogLevel level, string action, string text, long? elapsedMs)
        {
            if (level < DefaultLogLevel)
                return;

            // 数据库未初始化时不分配 LogEntry，避免无谓的 GC 压力
            if (!_dbInitialized)
                return;

            // Write to SQLite (UTC for timezone-invariant comparison)
            WriteToDatabase(new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level.ToString(),
                Action = action,
                Details = text,
                ElapsedMs = elapsedMs,
            });
        }

        /// <summary>
        /// 写入 SQLite 数据库
        /// </summary>
        private void WriteToDatabase(LogEntry entry)
        {
            if (!_dbInitialized)
                return;

            try
            {
                lock (_dbLock)
                {
                    using var conn = CreateConnection();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO logs (timestamp, level, action, details, elapsed_ms)
                        VALUES (@timestamp, @level, @action, @details, @elapsed_ms)
                    ";
                    cmd.Parameters.AddWithValue("@timestamp",
                        entry.Timestamp.ToString(_datetimeFormat));
                    cmd.Parameters.AddWithValue("@level", entry.Level);
                    cmd.Parameters.AddWithValue("@action", entry.Action);
                    cmd.Parameters.AddWithValue("@details", entry.Details);
                    cmd.Parameters.AddWithValue("@elapsed_ms",
                        (object?)entry.ElapsedMs ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // SQLite 写入失败时静默忽略（不走文件回退）
            }
        }
    }
}
