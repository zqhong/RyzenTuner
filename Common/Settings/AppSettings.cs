using System;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;

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

        /// <summary>
        /// 初始化数据库连接并加载所有已保存的设置到缓存。
        /// 需在首次访问任何设置之前调用（Program.Main）。
        /// </summary>
        public static void Initialize()
        {
            lock (_initLock)
            {
                if (_connectionString != null)
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

                    // 加载所有已保存设置到缓存
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT key, value FROM settings";
                        using var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            _cache[reader.GetString(0)] = reader.GetString(1);
                        }
                    }

                    // 所有初始化成功后，才标记连接字符串（允许后续重试）
                    _connectionString = connectionString;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AppSettings] Initialize failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取指定键的设置值，不存在时返回 null。
        /// </summary>
        public static string? Get(string key)
        {
            return _cache.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// 获取指定键的设置值并转换为目标类型，不存在或转换失败时返回 defaultValue。
        /// </summary>
        public static T Get<T>(string key, T defaultValue) where T : IConvertible
        {
            if (_cache.TryGetValue(key, out var value) && value != null)
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AppSettings] Get<{typeof(T).Name}>('{key}') conversion failed: {ex.Message}");
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
            try
            {
                lock (_connectionLock)
                {
                    var conn = EnsureConnection();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT OR REPLACE INTO settings (key, value) VALUES (@key, @value)";
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.ExecuteNonQuery();

                    _cache[key] = value;
                }
            }
            catch (Exception ex)
            {
                lock (_connectionLock)
                {
                    ResetConnection();
                }

                Debug.WriteLine($"[AppSettings] Set('{key}') failed: {ex.Message}");
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
            try
            {
                lock (_connectionLock)
                {
                    var conn = EnsureConnection();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM settings WHERE key = @key";
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.ExecuteNonQuery();

                    _cache.TryRemove(key, out _);
                }
            }
            catch (Exception ex)
            {
                lock (_connectionLock)
                {
                    ResetConnection();
                }

                Debug.WriteLine($"[AppSettings] Remove('{key}') failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭持久连接（应用退出时调用）。
        /// </summary>
        public static void CloseConnection()
        {
            lock (_connectionLock)
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
            }
        }

        /// <summary>
        /// 确保持久连接已创建并打开，返回保证非 null 的连接引用。
        /// </summary>
        private static SQLiteConnection EnsureConnection()
        {
            if (_connection == null)
            {
                _connection = new SQLiteConnection(_connectionString);
                _connection.Open();
            }

            return _connection;
        }

        /// <summary>
        /// 重置断开的连接，下次操作时自动创建新连接重试。
        /// </summary>
        private static void ResetConnection()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
