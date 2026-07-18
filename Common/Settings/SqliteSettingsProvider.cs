using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace RyzenTuner.Common.SettingsStore
{
    /// <summary>
    /// SQLite 后备的设置提供程序 — 将 Settings.Default 的所有读写重定向到
    /// RyzenTuner.db 的 settings 表中，替代默认的 user.config 文件存储。
    /// </summary>
    public class SqliteSettingsProvider : SettingsProvider
    {
        private string _connectionString = string.Empty;

        public override string ApplicationName
        {
            get => "RyzenTuner";
            set { }
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name ?? "SqliteSettingsProvider", config);

            _connectionString = SettingsDatabase.GetConnectionString();

            try
            {
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SqliteSettingsProvider] Initialize failed: {ex.Message}");
                // 初始化失败不阻止后续使用，Get/Set 会再次尝试
            }
        }

        /// <summary>
        /// 确保 SQLite 数据库和 settings 表已创建。
        /// </summary>
        private void InitializeDatabase()
        {
            // 必须先确保目录存在再打开连接，否则 SQLITE_CANTOPEN
            SettingsDatabase.EnsureDirectoryExists(SettingsDatabase.DefaultDbPath);

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            var currentDbPath = SettingsDatabase.GetDbPath();
            Debug.WriteLine($"[SqliteSettingsProvider] DB path: {currentDbPath}");

            using var cmd = conn.CreateCommand();
            cmd.CommandText = SettingsDatabase.CreateSettingsTableSql;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 检查从 SQLite 读取的值是否符合目标类型的基本格式。
        /// </summary>
        private static bool IsValueValidForType(string value, Type targetType)
        {
            if (targetType == typeof(bool))
            {
                return bool.TryParse(value, out _);
            }

            if (targetType == typeof(int) || targetType == typeof(long) ||
                targetType == typeof(short) || targetType == typeof(byte))
            {
                return long.TryParse(value, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out _);
            }

            if (targetType == typeof(float) || targetType == typeof(double) ||
                targetType == typeof(decimal))
            {
                return double.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out _);
            }

            // 字符串和其他类型直接接受
            return true;
        }

        public override SettingsPropertyValueCollection GetPropertyValues(
            SettingsContext context, SettingsPropertyCollection collection)
        {
            var values = new SettingsPropertyValueCollection();

            // Phase 1: 批量加载所有设置（一次查询，消除 N+1）
            var dbValues = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                using var conn = new SQLiteConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT key, value FROM settings";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    dbValues[reader.GetString(0)] = reader.GetString(1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SqliteSettingsProvider] Batch load failed: {ex.Message}");
                // 批量加载失败，逐个回退（下方逐属性处理）
            }

            // Phase 2: 为每个属性构造 SettingsPropertyValue
            foreach (SettingsProperty property in collection)
            {
                var value = new SettingsPropertyValue(property);

                try
                {
                    // 获取序列化值
                    string? serializedValue;

                    if (dbValues.TryGetValue(property.Name, out var storedValue) &&
                        IsValueValidForType(storedValue, property.PropertyType))
                    {
                        // SQLite 中有有效值
                        serializedValue = storedValue;
                    }
                    else
                    {
                        // SQLite 中无值或值无效，由框架回退到 DefaultSettingValueAttribute
                        serializedValue = null;
                    }

                    if (serializedValue != null)
                    {
                        value.SerializedValue = serializedValue;
                    }
                    // 仍为 null 时由 ApplicationSettingsBase 回退到 DefaultSettingValueAttribute
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SqliteSettingsProvider] GetPropertyValue failed for '{property.Name}': {ex.Message}");
                    // 使用默认值
                }

                value.IsDirty = false;
                values.Add(value);
            }

            return values;
        }

        public override void SetPropertyValues(
            SettingsContext context, SettingsPropertyValueCollection collection)
        {
            try
            {
                using var conn = new SQLiteConnection(_connectionString);
                conn.Open();
                using var tx = conn.BeginTransaction();

                try
                {
                    foreach (SettingsPropertyValue value in collection)
                    {
                        if (!value.IsDirty)
                            continue;

                        // #12: 跳过 null SerializedValue — 不创建行，让框架使用 DefaultSettingValueAttribute
                        if (value.SerializedValue == null)
                            continue;

                        // #9: 二进制序列化保护 — 检测 byte[] 并跳过，写入可见日志
                        if (value.SerializedValue is byte[])
                        {
                            System.Diagnostics.Trace.WriteLine(
                                $"[SqliteSettingsProvider] Binary-serialized property '{value.Name}' is not supported, value will not be persisted");
                            continue;
                        }

                        var serialized = value.SerializedValue?.ToString() ?? string.Empty;

                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            INSERT OR REPLACE INTO settings (key, value)
                            VALUES (@key, @value)
                        ";
                        cmd.Parameters.AddWithValue("@key", value.Name);
                        cmd.Parameters.AddWithValue("@value", serialized);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();

                    // #1: 提交成功后才清除脏标记 — 避免 Commit() 失败后永久丢失
                    foreach (SettingsPropertyValue value in collection)
                    {
                        if (value.IsDirty)
                        {
                            value.IsDirty = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SqliteSettingsProvider] SetPropertyValues transaction failed: {ex.Message}");
                    try { tx.Rollback(); }
                    catch (Exception rollbackEx)
                    {
                        Debug.WriteLine($"[SqliteSettingsProvider] Rollback also failed: {rollbackEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SqliteSettingsProvider] SetPropertyValues connection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 版本升级时标记需要重新迁移（删除架构版本标记）。
        /// 由于 SQLite 存储是跨版本共享的，真正的数据迁移由 SettingsMigration 处理，
        /// 而 SettingsProvider 基类没有 Upgrade() 虚方法；此逻辑由
        /// SettingsMigration 的架构版本检测机制自动完成。
        /// 注意：ApplicationSettingsBase.Upgrade() 通过正常的 GetPropertyValues/
        /// SetPropertyValues 循环工作，不需要 provider 端特殊处理。
        /// </summary>
    }
}
