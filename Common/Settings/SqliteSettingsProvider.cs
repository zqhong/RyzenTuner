using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SQLite;
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

            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RyzenTuner.db");
            _connectionString = $"Data Source={dbPath};Version=3;Journal Mode=WAL;";

            try
            {
                using var conn = new SQLiteConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS settings (
                        key TEXT PRIMARY KEY,
                        value TEXT NOT NULL
                    )
                ";
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // 表创建失败时静默处理 — 后续 Get/Set 会再次尝试
            }
        }

        public override SettingsPropertyValueCollection GetPropertyValues(
            SettingsContext context, SettingsPropertyCollection collection)
        {
            var values = new SettingsPropertyValueCollection();

            try
            {
                using var conn = new SQLiteConnection(_connectionString);
                conn.Open();

                foreach (SettingsProperty property in collection)
                {
                    var value = new SettingsPropertyValue(property);

                    try
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT value FROM settings WHERE key = @key";
                        cmd.Parameters.AddWithValue("@key", property.Name);
                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            value.SerializedValue = result.ToString();
                        }
                        // 表中无记录时保留 SerializedValue = null，由
                        // ApplicationSettingsBase 回退到 DefaultSettingValue 属性值
                    }
                    catch
                    {
                        // 单条查询失败时使用默认值
                    }

                    value.IsDirty = false;
                    values.Add(value);
                }
            }
            catch
            {
                // 整个连接失败时，所有属性使用默认值
                foreach (SettingsProperty property in collection)
                {
                    var value = new SettingsPropertyValue(property) { IsDirty = false };
                    values.Add(value);
                }
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

                        var serialized = value.SerializedValue?.ToString() ?? string.Empty;

                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            INSERT OR REPLACE INTO settings (key, value)
                            VALUES (@key, @value)
                        ";
                        cmd.Parameters.AddWithValue("@key", value.Name);
                        cmd.Parameters.AddWithValue("@value", serialized);
                        cmd.ExecuteNonQuery();

                        value.IsDirty = false;
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                }
            }
            catch
            {
                // 写入失败时静默忽略
            }
        }
    }
}
