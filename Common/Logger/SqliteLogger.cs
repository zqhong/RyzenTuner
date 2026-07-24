using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;

using RyzenTuner.Common.Settings;

namespace RyzenTuner.Common.Logger
{
    /// <summary>
    /// SQLite 日志记录器 — 所有日志写入 SQLite 数据库。
    /// 数据库路径和连接字符串通过 SettingsDatabase 共享。
    /// </summary>
    public sealed class SqliteLogger : IDisposable
    {
        // ISO 8601 兼容格式，作为常量以防止误改。
        // Cleanup() 中的 DELETE 查询使用字符串字典序比较 timestamp 列，
        // 任何格式变更（如省略前导零、12 小时制）都会导致时间比较失效。
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string DefaultAction = "General";
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly object _dbLock = new();
        private SQLiteConnection? _persistentConn;
        private volatile bool _disposed;
        private volatile bool _dbInitialized;

        private volatile LogLevel _defaultLogLevel = LogLevel.Warning;

        public LogLevel DefaultLogLevel
        {
            get => _defaultLogLevel;
            set => _defaultLogLevel = value;
        }

        public SqliteLogger()
        {
            // 使用 SettingsDatabase 共享的路径和连接字符串
            _dbPath = SettingsDatabase.GetDbPath();
            _connectionString = SettingsDatabase.GetConnectionString();

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
        private string ConnectionString => _connectionString;

        /// <summary>
        /// 初始化数据库和表结构，并建立持久化连接供后续复用。
        /// </summary>
        private void InitializeDatabase()
        {
            try
            {
                // 直接创建持久化连接，避免后续 GetConnection() 再次打开新连接
                var conn = new SQLiteConnection(ConnectionString);
                conn.Open();
                _persistentConn = conn;

                // 创建表
                using var createCmd = conn.CreateCommand();
                createCmd.CommandText = @"
                    PRAGMA auto_vacuum = 1;  /* INCREMENTAL mode — free pages are reclaimable */
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

                _dbInitialized = true;
            }
            catch (Exception ex)
            {
                // 清理因创建或 Open 失败可能残留的连接
                try { _persistentConn?.Dispose(); } catch { }
                _persistentConn = null;
                System.Diagnostics.Trace.WriteLine($"[SqliteLogger] Database initialization failed: {ex.Message}");
                _dbInitialized = false;
            }
        }

        /// <summary>
        /// 获取持久化数据库连接，按需重连。
        /// 注意：调用方必须已持有 _dbLock。
        /// </summary>
        private SQLiteConnection GetConnection()
        {
            System.Diagnostics.Debug.Assert(
                System.Threading.Monitor.IsEntered(_dbLock),
                "GetConnection must be called inside _dbLock");

            // 检查现有连接是否可复用（状态为 Open 的非损坏连接）
            if (_persistentConn != null)
            {
                try
                {
                    if (_persistentConn.State == ConnectionState.Open)
                        return _persistentConn;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[SqliteLogger] Persistent connection corrupted, recreating: {ex.Message}");
                }

                try { _persistentConn?.Dispose(); } catch { /* 抑制 Dispose 异常 */ }
                _persistentConn = null;
            }

            // 先创建再赋值：确保 Open() 失败时 _persistentConn 不会遗留僵尸连接
            var newConn = new SQLiteConnection(ConnectionString);
            try
            {
                newConn.Open();
            }
            catch (Exception)
            {
                newConn.Dispose();
                throw;
            }

            _persistentConn = newConn;
            return _persistentConn;
        }

        /// <summary>
        /// 创建并返回一个新的数据库连接（用于查询路径）
        /// </summary>
        private SQLiteConnection CreateConnection()
        {
            var conn = new SQLiteConnection(ConnectionString);
            try
            {
                conn.Open();
            }
            catch
            {
                conn.Dispose();
                throw;
            }

            return conn;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            // 等待所有进行中的数据库操作完成后释放持久化连接，
            // 避免与 WriteToDatabase/Cleanup/DeleteAll 中正在使用的连接发生竞态。
            lock (_dbLock)
            {
                if (_disposed)
                    return;

                _disposed = true;

                // 释放持久化连接
                try { _persistentConn?.Dispose(); } catch { /* 抑制 Dispose 异常 */ }
                _persistentConn = null;
            }
        }

        public static LogLevel ToLogLevel(string logLevel)
        {
            if (logLevel == null)
                throw new ArgumentNullException(nameof(logLevel));

            if (string.IsNullOrWhiteSpace(logLevel))
                throw new ArgumentException($"Log level cannot be empty or whitespace.", nameof(logLevel));

            if (Enum.TryParse(logLevel, ignoreCase: true, out LogLevel result)
                && Enum.IsDefined(typeof(LogLevel), result))
            {
                return result;
            }

            throw new ArgumentException($"Unknown log level: '{logLevel}'", nameof(logLevel));
        }

        // ================================================================
        // 日志方法 — 单字符串重载（兼容现有调用，action 默认 "General"）
        // ================================================================

        public void Debug(string text)
        {
            WriteFormattedLog(LogLevel.Debug, DefaultAction, text, null);
        }

        public void Error(string text)
        {
            WriteFormattedLog(LogLevel.Error, DefaultAction, text, null);
        }

        public void Fatal(string text)
        {
            WriteFormattedLog(LogLevel.Fatal, DefaultAction, text, null);
        }

        public void Info(string text)
        {
            WriteFormattedLog(LogLevel.Info, DefaultAction, text, null);
        }

        public void Trace(string text)
        {
            WriteFormattedLog(LogLevel.Trace, DefaultAction, text, null);
        }

        public void Warning(string text)
        {
            WriteFormattedLog(LogLevel.Warning, DefaultAction, text, null);
        }

        public void LogException(Exception? e)
        {
            if (e == null)
                return;

            Error("Exception", e.ToString());
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
        /// <remarks>
        /// 此方法不获取 _dbLock，依赖 SQLite 内部锁和 BusyTimeout 来保证并发安全。
        /// 与 Dispose() 之间存在 TOCTOU 竞态：如果 Dispose() 在 _disposed 检查之后执行，
        /// 新连接可能在 _persistentConn 释放后创建。
        /// 为缓解此问题，在 CreateConnection() 成功后再次检查 _disposed。
        /// </remarks>
        /// <param name="levelFilter">日志级别筛选（null 表示所有级别）</param>
        /// <param name="searchText">搜索关键字（匹配 action 或 details）</param>
        /// <param name="limit">返回条数上限</param>
        public DataTable QueryLogs(string? levelFilter = null, string? searchText = null, int limit = 1000)
        {
            var table = new DataTable();

            if (!_dbInitialized || _disposed)
                return table;

            try
            {
                using var conn = CreateConnection();

                // 再次检查 _disposed：缩小 TOCTOU 窗口，确保 Dispose() 完成后不创建新连接。
                if (_disposed)
                    return table;

                using var cmd = conn.CreateCommand();

                var sql = "SELECT timestamp, level, action, details, elapsed_ms FROM logs WHERE 1=1";

                if (!string.IsNullOrEmpty(levelFilter))
                {
                    sql += " AND level = @level";
                    cmd.Parameters.AddWithValue("@level", levelFilter);
                }

                if (!string.IsNullOrEmpty(searchText))
                {
                    // 转义 SQL LIKE 通配符，防止用户输入的 %/_ 匹配到非预期行
                    var escaped = searchText!
                        .Replace("\\", "\\\\")
                        .Replace("%", "\\%")
                        .Replace("_", "\\_");
                    sql += " AND (action LIKE @search ESCAPE '\\' OR details LIKE @search ESCAPE '\\')";
                    cmd.Parameters.AddWithValue("@search", $"%{escaped}%");
                }

                sql += " ORDER BY id DESC LIMIT @limit";
                cmd.Parameters.AddWithValue("@limit", limit);
                cmd.CommandText = sql;

                using var reader = cmd.ExecuteReader();
                // 预先获取 ordinal，避免在循环中重复调用 GetOrdinal
                var elapsedMsOrdinal = reader.GetOrdinal("elapsed_ms");

                // 先定义字符串类型的列，确保 elapsed_ms 可存储格式化文本
                table.Columns.AddRange(new[]
                {
                    new DataColumn("timestamp", typeof(string)),
                    new DataColumn("level", typeof(string)),
                    new DataColumn("action", typeof(string)),
                    new DataColumn("details", typeof(string)),
                    new DataColumn("elapsed_ms", typeof(string)),
                });

                while (reader.Read())
                {
                    var row = table.NewRow();
                    row["timestamp"] = reader["timestamp"]?.ToString() ?? "";
                    row["level"] = reader["level"]?.ToString() ?? "";
                    row["action"] = reader["action"]?.ToString() ?? "";
                    row["details"] = reader["details"]?.ToString() ?? "";
                    row["elapsed_ms"] = reader.IsDBNull(elapsedMsOrdinal) ? "-" : reader.GetInt64(elapsedMsOrdinal) + " ms";
                    table.Rows.Add(row);
                }

                return table;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[SqliteLogger] QueryLogs failed: {ex.Message}");
                return table;
            }
        }

        /// <summary>
        /// 获取可用的日志级别列表（用于筛选下拉框，不包含 "All"）
        /// </summary>
        /// <remarks>
        /// 线程安全说明：同 <see cref="QueryLogs"/> — 不获取 _dbLock，依赖 SQLite 内部锁。
        /// 同样存在 TOCTOU 竞态，在 CreateConnection() 成功后再次检查 _disposed。
        /// </remarks>
        public string[] GetAvailableLevels()
        {
            if (!_dbInitialized || _disposed)
                return Array.Empty<string>();

            try
            {
                using var conn = CreateConnection();

                // 再次检查 _disposed：缩小 TOCTOU 窗口
                if (_disposed)
                    return Array.Empty<string>();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT DISTINCT level FROM logs ORDER BY level";
                using var reader = cmd.ExecuteReader();

                var levels = new List<string>();
                while (reader.Read())
                {
                    levels.Add(reader.GetString(0));
                }
                return levels.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[SqliteLogger] GetAvailableLevels failed: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 获取日志总数
        /// </summary>
        /// <remarks>
        /// 线程安全说明：同 <see cref="QueryLogs"/> — 不获取 _dbLock，依赖 SQLite 内部锁。
        /// 同样存在 TOCTOU 竞态，在 CreateConnection() 成功后再次检查 _disposed。
        /// </remarks>
        public long GetLogCount()
        {
            if (!_dbInitialized || _disposed)
                return 0;

            try
            {
                using var conn = CreateConnection();

                // 再次检查 _disposed：缩小 TOCTOU 窗口
                if (_disposed)
                    return 0;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM logs";
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[SqliteLogger] GetLogCount failed: {ex.Message}");
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
            if (!_dbInitialized || _disposed)
                return;

            lock (_dbLock)
            {
                if (_disposed)
                    return;

                try
                {
                    using var cmd = GetConnection().CreateCommand();
                    cmd.CommandText = "DELETE FROM logs";
                    cmd.ExecuteNonQuery();

                    // 重建数据库文件，回收空闲页面占用的磁盘空间
                    // VACUUM 需要独占锁；由于在 _dbLock 下执行，此时无其他连接在使用持久化连接。
                    // 若文件较大或并发读取中执行，VACUUM 可能失败 — 非关键错误，静默处理。
                    try
                    {
                        cmd.CommandText = "VACUUM";
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception vacuumEx)
                    {
                        System.Diagnostics.Trace.WriteLine(
                            $"[SqliteLogger] VACUUM after DeleteAll failed (non-critical): {vacuumEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    try { _persistentConn?.Dispose(); } catch { }
                    _persistentConn = null;
                    System.Diagnostics.Trace.WriteLine($"[SqliteLogger] DeleteAll failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除超过指定天数的日志
        /// </summary>
        public void Cleanup(int retentionDays)
        {
            if (!_dbInitialized || _disposed || retentionDays <= 0)
                return;

            lock (_dbLock)
            {
                if (_disposed)
                    return;

                try
                {
                    using var cmd = GetConnection().CreateCommand();
                    cmd.CommandText = "DELETE FROM logs WHERE timestamp < @cutoff";
                    cmd.Parameters.AddWithValue("@cutoff",
                        DateTime.UtcNow.AddDays(-retentionDays).ToString(DateTimeFormat, CultureInfo.InvariantCulture));
                    cmd.ExecuteNonQuery();

                    // 回收部分空闲页面（最多 50 页），避免数据库文件无限膨胀。
                    // VACUUM 需要独占锁不适合在此处使用；incremental_vacuum 在 auto_vacuum=1 模式下有效。
                    try
                    {
                        cmd.CommandText = "PRAGMA incremental_vacuum(50)";
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ivEx)
                    {
                        System.Diagnostics.Trace.WriteLine(
                            $"[SqliteLogger] incremental_vacuum after Cleanup failed (non-critical): {ivEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    try { _persistentConn?.Dispose(); } catch { }
                    _persistentConn = null;
                    System.Diagnostics.Trace.WriteLine($"[SqliteLogger] Cleanup failed: {ex.Message}");
                }
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

            // 数据库未初始化或已释放则跳过写入（WriteToDatabase 内部还有一次检查，
            // 但此处提前返回可减少在无锁路径下访问 volatile 标志的警告）
            if (!_dbInitialized || _disposed)
                return;

            // 防御性空值处理：LogEntry 的属性 setter 拒绝 null，确保日志方法不会因 null 输入而抛出异常。
            // 即使 Nullable 启用时编译器已保证非空，仍添加此保护以应对反射 / 动态调用场景。
            WriteToDatabase(new LogEntry
            {
                Level = level,
                Action = action ?? string.Empty,
                Details = text ?? string.Empty,
                ElapsedMilliseconds = elapsedMs,
            });
        }

        /// <summary>
        /// 写入 SQLite 数据库
        /// </summary>
        private void WriteToDatabase(LogEntry entry)
        {
            if (!_dbInitialized)
                return;

            lock (_dbLock)
            {
                if (_disposed || !_dbInitialized)
                    return;

                try
                {
                    using var cmd = GetConnection().CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO logs (timestamp, level, action, details, elapsed_ms)
                        VALUES (@timestamp, @level, @action, @details, @elapsed_ms)
                    ";
                    cmd.Parameters.AddWithValue("@timestamp",
                        entry.Timestamp.ToString(DateTimeFormat, CultureInfo.InvariantCulture));
                    cmd.Parameters.AddWithValue("@level", entry.Level.ToString());
                    cmd.Parameters.AddWithValue("@action", entry.Action);
                    cmd.Parameters.AddWithValue("@details", entry.Details);
                    cmd.Parameters.AddWithValue("@elapsed_ms",
                        entry.ElapsedMilliseconds.HasValue ? (object)entry.ElapsedMilliseconds.Value : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    // 重置持久化连接，强制下次重连，避免因连接损坏导致重复失败
                    try { _persistentConn?.Dispose(); } catch { }
                    _persistentConn = null;
                    System.Diagnostics.Trace.WriteLine($"[SqliteLogger] WriteToDatabase failed: {ex.Message}");
                }
            }
        }
    }
}
