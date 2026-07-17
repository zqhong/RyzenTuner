using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Data.SQLite;

namespace RyzenTuner.Common.Logger
{
    /// <summary>
    /// SQLite 日志记录器 — 替代 SimpleLogger 作为主日志。
    /// 同时写入 SQLite 数据库和 .log 纯文本文件。
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

        private const string FileExt = ".log";
        private const string DbDirName = "logs";
        private const string DbFileName = "RyzenTuner.db";

        private readonly string _datetimeFormat;
        private readonly string _logFilename;
        private readonly string _dbPath;
        private readonly object _fileLock = new();
        private readonly object _dbLock = new();
        private StreamWriter? _writer;
        private bool _disposed;
        private SQLiteConnection? _connection;
        private bool _dbInitialized;

        public LogLevel DefaultLogLevel;

        public SqliteLogger()
        {
            _datetimeFormat = "yyyy-MM-dd HH:mm:ss";
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // .log file path (same as SimpleLogger)
            _logFilename = Path.Combine(baseDir,
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + FileExt);

            // SQLite database path in logs/ subdirectory
            var dbDir = Path.Combine(baseDir, DbDirName);
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
            _dbPath = Path.Combine(dbDir, DbFileName);

            DefaultLogLevel = LogLevel.Warning;

            // Open log file for appending (same as SimpleLogger)
            _writer = new StreamWriter(_logFilename, true, Encoding.UTF8)
            {
                AutoFlush = true,
            };

            // Initialize SQLite
            InitializeDatabase();
        }

        /// <summary>
        /// 获取 SQLite 数据库文件路径（供日志查看器使用）
        /// </summary>
        public string DbPath => _dbPath;

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        private string ConnectionString =>
            $"Data Source={_dbPath};Version=3;Journal Mode=WAL;";

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

                WriteLine("[SQLite] 数据库初始化完成: " + _dbPath);
            }
            catch (Exception ex)
            {
                _dbInitialized = false;
                WriteLine($"[SQLite] 数据库初始化失败: {ex.Message}");
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

            lock (_fileLock)
            {
                _writer?.Dispose();
                _writer = null;
            }

            lock (_dbLock)
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
            }
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
                _ => throw new Exception($"不正确的 log level 类型：{logLevel}")
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
            if (e == null)
                return;

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
        /// <param name="limit">返回条数上限</param>
        public DataTable QueryLogs(string? levelFilter = null, int limit = 1000)
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

                if (!string.IsNullOrEmpty(levelFilter) && levelFilter != "All")
                {
                    sql += " AND level = @level";
                    cmd.Parameters.AddWithValue("@level", levelFilter);
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
                    row["elapsed_ms"] = elapsedMs == DBNull.Value ? "-" : elapsedMs.ToString() + " ms";
                    table.Rows.Add(row);
                }

                return table;
            }
            catch (Exception ex)
            {
                WriteLine($"[SQLite] 查询日志失败: {ex.Message}");
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
        /// 删除超过指定天数的日志
        /// </summary>
        public void Cleanup(int retentionDays)
        {
            if (!_dbInitialized || retentionDays <= 0)
                return;

            try
            {
                using var conn = CreateConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM logs WHERE timestamp < @cutoff";
                cmd.Parameters.AddWithValue("@cutoff",
                    DateTime.Now.AddDays(-retentionDays).ToString(_datetimeFormat));
                var deleted = cmd.ExecuteNonQuery();

                // VACUUM only if we deleted a substantial number of rows (>1000)
                if (deleted > 1000)
                {
                    using var vacuumCmd = conn.CreateCommand();
                    vacuumCmd.CommandText = "VACUUM";
                    vacuumCmd.ExecuteNonQuery();
                }

                WriteLine($"[SQLite] 已清理 {deleted} 条过期日志（保留 {retentionDays} 天）");
            }
            catch (Exception ex)
            {
                WriteLine($"[SQLite] 日志清理失败: {ex.Message}");
            }
        }

        // ================================================================
        // 内部方法
        // ================================================================

        /// <summary>
        /// 写入格式化日志
        /// </summary>
        private void WriteFormattedLog(LogLevel level, string action, string text, long? elapsedMs)
        {
            if (level < DefaultLogLevel)
            {
                return;
            }

            var now = DateTime.Now;
            var pretext = level switch
            {
                LogLevel.Trace => now.ToString(_datetimeFormat) + " [TRACE]   ",
                LogLevel.Info => now.ToString(_datetimeFormat) + " [INFO]    ",
                LogLevel.Debug => now.ToString(_datetimeFormat) + " [DEBUG]   ",
                LogLevel.Warning => now.ToString(_datetimeFormat) + " [WARNING] ",
                LogLevel.Error => now.ToString(_datetimeFormat) + " [ERROR]   ",
                LogLevel.Fatal => now.ToString(_datetimeFormat) + " [FATAL]   ",
                _ => ""
            };

            // Write to .log file
            var logLine = pretext + text;
            if (elapsedMs.HasValue)
            {
                logLine += $" (耗时: {elapsedMs.Value}ms)";
            }
            WriteLine(logLine);

            // Write to SQLite
            WriteToDatabase(new LogEntry
            {
                Timestamp = now,
                Level = level.ToString(),
                Action = action,
                Details = text,
                ElapsedMs = elapsedMs,
            });
        }

        /// <summary>
        /// 写入纯文本 .log 文件
        /// </summary>
        private void WriteLine(string text, bool append = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            lock (_fileLock)
            {
                if (_writer == null)
                    return;

                _writer.WriteLine(text);
            }
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
                    cmd.Parameters.AddWithValue("@action", entry.Action ?? "");
                    cmd.Parameters.AddWithValue("@details", entry.Details ?? "");
                    cmd.Parameters.AddWithValue("@elapsed_ms",
                        (object?)entry.ElapsedMs ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Fallback to file-only logging if SQLite fails
                lock (_fileLock)
                {
                    _writer?.WriteLine(
                        $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} [SQLITE-ERR] 写入数据库失败: {ex.Message}");
                }
            }
        }
    }
}
