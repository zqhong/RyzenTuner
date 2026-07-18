using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using RyzenTuner.Common.SettingsStore;

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
        private static readonly Dictionary<string, string?> _cache = new(StringComparer.Ordinal);

        /// <summary>
        /// 初始化数据库连接并加载所有已保存的设置到缓存。
        /// 需在首次访问任何设置之前调用（Program.Main）。
        /// </summary>
        public static void Initialize()
        {
            if (_connectionString != null)
                return;

            _connectionString = SettingsDatabase.GetConnectionString();
            SettingsDatabase.EnsureDirectoryExists(SettingsDatabase.DefaultDbPath);

            try
            {
                using var conn = new SQLiteConnection(_connectionString);
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] Initialize failed: {ex.Message}");
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
                catch
                {
                    // 转换失败，使用默认值
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
            _cache[key] = value;

            try
            {
                using var conn = new SQLiteConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT OR REPLACE INTO settings (key, value) VALUES (@key, @value)";
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@value", value);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettings] Set('{key}') failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置布尔值并立即持久化到 SQLite。
        /// </summary>
        public static void Set(string key, bool value)
        {
            Set(key, value ? "True" : "False");
        }

        /// <summary>
        /// 设置整数值并立即持久化到 SQLite。
        /// </summary>
        public static void Set(string key, int value)
        {
            Set(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 检查指定键是否存在缓存中（即是否曾在 SQLite 中保存过值）。
        /// </summary>
        public static bool Has(string key)
        {
            return _cache.ContainsKey(key);
        }

        /// <summary>
        /// 兼容旧版 Save() 调用 — 设置已立即持久化，此方法为无操作。
        /// </summary>
        [Obsolete("设置已自动持久化，无需调用 Save()")]
        public static void Save()
        {
        }
    }
}
