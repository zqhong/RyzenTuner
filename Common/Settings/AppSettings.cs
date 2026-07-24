using System;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace RyzenTuner.Common.Settings
{
    /// <summary>
    /// 直接读写 SQLite 的设置存储（替代旧的 ApplicationSettingsBase + user.config 模式）。
    ///
    /// 所有设置以 key-value 形式存储在 RyzenTuner.db 的 settings 表中。
    /// Get() 回退到调用方提供的默认值，Set() 立即持久化到数据库。
    /// </summary>
    public static class AppSettings
    {
        private static string? _connectionString;
        private static SQLiteConnection? _connection;
        private static readonly object _connectionLock = new();
        private static readonly object _initLock = new();
        private static readonly ConcurrentDictionary<string, string?> _cache = new(StringComparer.Ordinal);
        private static volatile bool _initialized;

        /// <summary>
        /// 初始化数据库连接并加载所有已保存的设置到缓存。
        /// 需在首次访问任何设置之前调用（Program.Main）。
        /// </summary>
        public static void Initialize()
        {
            lock (_initLock)
            {
                if (_initialized)
                    return;

                var connectionString = SettingsDatabase.GetConnectionString();

                try
                {
                    SettingsDatabase.EnsureDirectoryExists(SettingsDatabase.DefaultDbPath);

                    using var conn = new SQLiteConnection(connectionString);
                    conn.Open();

                    // 确保 settings 表存在
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = SettingsDatabase.CreateSettingsTableSql;
                        cmd.ExecuteNonQuery();
                    }

                    // 清除缓存中可能存在的旧数据（支持重新初始化场景）
                    _cache.Clear();

                    // 加载所有已保存设置到缓存
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT key, value FROM settings";
                        using var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            // 防御性：key 列（TEXT PRIMARY KEY）在 SQLite 中仍允许 NULL，
                            // value 列尽管定义为 NOT NULL，手动 DB 编辑仍可能导致 NULL。
                            // 对两列都使用 IsDBNull 避免 InvalidCastException 使所有设置加载失败。
                            if (reader.IsDBNull(0))
                                continue;
                            if (reader.IsDBNull(1))
                            {
                                _cache[reader.GetString(0)] = null;
                                continue;
                            }

                            _cache[reader.GetString(0)] = reader.GetString(1);
                        }
                    }

                    // 所有初始化成功后，才标记连接字符串（允许后续重试）。
                    // 在 _connectionLock 下写入，确保与 Set()/Remove() 中的读取同步（内存屏障），
                    // 保证跨线程可见性（尤其 ARM 架构）。
                    lock (_connectionLock)
                    {
                        _connectionString = connectionString;
                    }

                    _initialized = true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[AppSettings] Initialize failed: {ex.Message}");
                    // 不设置 _connectionString 和 _initialized，让异常传播到调用方。
                    // 调用方（Program.Main）会处理启动异常并退出，下次启动时自动重试。
                    // 如果在此处部分初始化缓存并标记为成功，会导致部分设置数据丢失的静默故障。
                    throw;
                }
            }
        }

        /// <summary>
        /// 获取指定键的设置值，不存在时返回 null。
        /// </summary>
        public static string? Get(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null, empty, or whitespace.", nameof(key));

            if (!_initialized)
                throw new InvalidOperationException("AppSettings has not been initialized. Call Initialize() first.");

            return _cache.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// 获取指定键的设置值并转换为目标类型，不存在或转换失败时返回 defaultValue。
        /// </summary>
        public static T Get<T>(string key, T defaultValue)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null, empty, or whitespace.", nameof(key));

            if (!_initialized)
                throw new InvalidOperationException("AppSettings has not been initialized. Call Initialize() first.");

            if (_cache.TryGetValue(key, out var value) && value != null)
            {
                try
                {
                    var targetType = typeof(T);

                    // 处理可空值类型：拆包为底层类型
                    if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        targetType = Nullable.GetUnderlyingType(targetType)!;
                    }

                    // 处理枚举类型
                    if (targetType.IsEnum)
                    {
                        try
                        {
                            return (T)Enum.Parse(targetType, value, ignoreCase: true);
                        }
                        catch (ArgumentException)
                        {
                            // 非法的枚举值字符串，回退到 defaultValue
                            Trace.WriteLine(
                                $"[AppSettings] Get<{typeof(T).Name}>('{key}'): '{value}' is not a valid enum value, falling back to default.");
                            return defaultValue;
                        }
                        catch (OverflowException)
                        {
                            // 数值超出枚举底层类型范围，回退到 defaultValue
                            Trace.WriteLine(
                                $"[AppSettings] Get<{typeof(T).Name}>('{key}'): '{value}' overflows the underlying type of {targetType.Name}, falling back to default.");
                            return defaultValue;
                        }
                    }

                    return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                }
                catch (Exception ex) when (ex is InvalidCastException or FormatException or
                    OverflowException or NotSupportedException)
                {
                    Trace.WriteLine($"[AppSettings] Get<{typeof(T).Name}>('{key}') conversion failed: {ex.Message}");
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 获取布尔值设置，不存在时返回 false。
        /// </summary>
        public static bool GetBool(string key)
        {
            return Get(key, false);
        }

        /// <summary>
        /// 设置字符串值并立即持久化到 SQLite。
        /// </summary>
        public static void Set(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null, empty, or whitespace.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            lock (_connectionLock)
            {
                if (!_initialized)
                    throw new InvalidOperationException("AppSettings has not been initialized. Call Initialize() first.");

                // 先尝试持久化到 SQLite，成功后才更新内存缓存
                try
                {
                    var conn = EnsureConnection();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT OR REPLACE INTO settings (key, value) VALUES (@key, @value)";
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.ExecuteNonQuery();

                    // 数据库写入成功后更新缓存，确保与持久化状态一致
                    _cache[key] = value;
                }
                catch (Exception ex) when (ex is SQLiteException or InvalidOperationException or ObjectDisposedException)
                {
                    ResetConnection();
                    Trace.WriteLine($"[AppSettings] Set('{key}') 持久化失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 设置布尔值并立即持久化到 SQLite。
        /// </summary>
        public static void Set(string key, bool value)
        {
            Set(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 设置整数值并立即持久化到 SQLite。
        /// </summary>
        public static void Set(string key, int value)
        {
            Set(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 删除指定键的设置（从数据库和缓存中移除）。
        /// </summary>
        public static void Remove(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null, empty, or whitespace.", nameof(key));

            lock (_connectionLock)
            {
                if (!_initialized)
                    throw new InvalidOperationException("AppSettings has not been initialized. Call Initialize() first.");

                // 先尝试从 SQLite 删除，成功后才更新缓存
                try
                {
                    var conn = EnsureConnection();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM settings WHERE key = @key";
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.ExecuteNonQuery();

                    // 数据库删除成功后更新缓存，确保与持久化状态一致
                    _cache.TryRemove(key, out _);
                }
                catch (Exception ex) when (ex is SQLiteException or InvalidOperationException or ObjectDisposedException)
                {
                    ResetConnection();
                    Trace.WriteLine($"[AppSettings] Remove('{key}') 持久化删除失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 关闭持久连接（应用退出时调用）。
        /// 关闭后将 _initialized 置为 false，防止误操作（Set/Remove）在关闭后
        /// 通过 EnsureConnection 创建新连接导致资源泄漏。
        ///
        /// 注意：必须将 _initialized 赋值放在 _connectionLock 内部，与 Set()/Remove()
        /// 的检查（同样在 _connectionLock 内部）保持同步，避免并发 Set()/Remove()
        /// 读取到过时的 _initialized=true 后通过 EnsureConnection 读取已置为 null 的
        /// _connectionString 导致异常被静默吞噬。
        ///
        /// 必须先持有 _initLock 再持 _connectionLock，与 Initialize() 的
        /// 锁定顺序一致（_initLock → _connectionLock），避免 AB-BA 死锁。
        /// ResetConnection 的 Debug.Assert 要求持有 _connectionLock，因此
        /// ResetConnection 必须在嵌套锁中调用。
        /// </summary>
        public static void CloseConnection()
        {
            lock (_initLock)
            {
                lock (_connectionLock)
                {
                    ResetConnection();
                    _connectionString = null;
                    _initialized = false;
                }
            }
        }

        /// <summary>
        /// 确保持久连接已创建并打开，返回保证非 null 的连接引用。
        /// 注意：调用方必须已持有 _connectionLock。
        /// </summary>
        private static SQLiteConnection EnsureConnection()
        {
            Debug.Assert(
                Monitor.IsEntered(_connectionLock),
                "EnsureConnection must be called inside _connectionLock");

            if (_connectionString == null)
                throw new InvalidOperationException(
                    "AppSettings has not been initialized. Call Initialize() first.");

            if (_connection != null)
            {
                if (_connection.State == System.Data.ConnectionState.Open)
                    return _connection;

                // 连接已被外部关闭（如 SQLite WAL 检查点竞争），丢弃并重新创建
                try
                {
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[AppSettings] EnsureConnection dispose stale connection: {ex.Message}");
                }

                _connection = null;
            }

            var newConn = new SQLiteConnection(_connectionString);
            try
            {
                newConn.Open();
            }
            catch (Exception)
            {
                newConn.Dispose();
                throw;
            }

            _connection = newConn;

            return _connection;
        }

        /// <summary>
        /// 重置断开的连接，下次操作时自动创建新连接重试。
        /// 注意：调用方必须已持有 _connectionLock。
        /// </summary>
        private static void ResetConnection()
        {
            Debug.Assert(
                Monitor.IsEntered(_connectionLock),
                "ResetConnection must be called inside _connectionLock");

            if (_connection == null) return;

            try
            {
                _connection.Dispose();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[AppSettings] ResetConnection Dispose failed: {ex.Message}");
            }
            finally
            {
                _connection = null;
            }
        }
    }
}
