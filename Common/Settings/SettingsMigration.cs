using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RyzenTuner.Common.SettingsStore
{
    /// <summary>
    /// 从旧的 user.config 文件向 SQLite settings 表迁移设置项。
    /// 仅在首次运行或架构版本变化时执行。
    /// </summary>
    internal static class SettingsMigration
    {
        /// <summary>
        /// 尝试从旧版 user.config 迁移设置到 SQLite。
        /// 需在首次访问 Settings.Default 之前调用。
        /// </summary>
        public static void Migrate()
        {
            try
            {
                var connString = SettingsDatabase.GetConnectionString();
                var currentDbPath = SettingsDatabase.GetDbPath();

                // 确保数据库目录存在
                var dir = Path.GetDirectoryName(currentDbPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using var conn = new SQLiteConnection(connString);
                conn.Open();

                // 确保 settings 表存在
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SettingsDatabase.CreateSettingsTableSql;
                    cmd.ExecuteNonQuery();
                }

                // #3: 检查架构版本 — 版本不匹配时更新版本标记，保留已有数据
                var currentVersion = ReadSchemaVersion(conn);
                if (currentVersion.HasValue && currentVersion.Value >= SettingsDatabase.CurrentSchemaVersion)
                {
                    // 已有最新版本数据，跳过迁移
                    Debug.WriteLine("[SettingsMigration] Already migrated, skipping");
                    return;
                }

                if (currentVersion.HasValue && currentVersion.Value < SettingsDatabase.CurrentSchemaVersion)
                {
                    Debug.WriteLine(
                        $"[SettingsMigration] Schema version {currentVersion.Value} < " +
                        $"{SettingsDatabase.CurrentSchemaVersion}, updating version and preserving existing settings");

                    // 仅更新版本标记，不删除已有用户数据
                    WriteSchemaVersion(conn);
                    return;
                }

                // 检查是否已有非版本标记的数据（首次运行但有人手动设置过）
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT COUNT(*) FROM settings WHERE key != @versionKey";
                    cmd.Parameters.AddWithValue("@versionKey", SettingsDatabase.SchemaVersionKey);
                    var count = (long)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        // 有数据但无版本标记 — 写入当前版本后跳过
                        WriteSchemaVersion(conn);
                        return;
                    }
                }

                // 查找旧的 user.config 文件
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var ryzenTunerDir = Path.Combine(localAppData, "RyzenTuner");

                if (!Directory.Exists(ryzenTunerDir))
                {
                    // 无旧配置文件，写入版本标记后退出
                    WriteSchemaVersion(conn);
                    return;
                }

                // #13: 限制搜索范围（备注：实际结构为嵌套子目录，使用 AllDirectories 但
                // 最多只有若干版本子目录，不会造成严重性能问题）
                var configFiles = FindUserConfigFiles(ryzenTunerDir);
                if (configFiles.Length == 0)
                {
                    WriteSchemaVersion(conn);
                    return;
                }

                // 使用最新修改的配置文件
                var latestConfig = configFiles
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .First();

                Debug.WriteLine($"[SettingsMigration] Migrating from: {latestConfig.FullName}");

                var doc = XDocument.Load(latestConfig.FullName);

                // 尝试无命名空间解析，再尝试有命名空间
                var settingElements = doc.Descendants("setting").ToList();
                XNamespace? ns = null;
                if (settingElements.Count == 0)
                {
                    // .NET Framework user.config 通常无命名空间，但部分版本有
                    ns = "urn:schemas-microsoft-com:netfx70settings";
                    settingElements = doc.Descendants(ns + "setting").ToList();
                }

                if (settingElements.Count == 0)
                {
                    WriteSchemaVersion(conn);
                    return;
                }

                using var tx = conn.BeginTransaction();
                try
                {
                    foreach (var setting in settingElements)
                    {
                        var name = setting.Attribute("name")?.Value;
                        if (string.IsNullOrEmpty(name))
                            continue;

                        // #11: 根据 serializeAs 属性提取正确值
                        var serializeAs = setting.Attribute("serializeAs")?.Value ?? "String";
                        string value;

                        if (serializeAs.Equals("Xml", StringComparison.OrdinalIgnoreCase))
                        {
                            // Xml 序列化 — 保留完整子元素结构
                            var valueElement = ns != null
                                ? setting.Element(ns + "value")
                                : setting.Element("value");

                            if (valueElement != null)
                            {
                                // 获取完整 inner XML（含子元素），确保反序列化时结构完整
                                value = string.Concat(valueElement.Nodes().Select(n => n.ToString()));
                            }
                            else
                            {
                                Debug.WriteLine($"[SettingsMigration] Skipping Xml-serialized '{name}': missing <value>");
                                continue;
                            }
                        }
                        else
                        {
                            // String 序列化（默认和常见情况）— 直接取文本内容
                            var valueElement = ns != null
                                ? setting.Element(ns + "value")
                                : setting.Element("value");

                            value = valueElement?.Value ?? string.Empty;
                        }

                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT OR REPLACE INTO settings (key, value) VALUES (@key, @value)";
                        cmd.Parameters.AddWithValue("@key", name);
                        cmd.Parameters.AddWithValue("@value", value);
                        cmd.ExecuteNonQuery();
                    }

                    // 写入当前架构版本标记
                    WriteSchemaVersion(conn, tx);

                    tx.Commit();
                    Debug.WriteLine($"[SettingsMigration] Migration completed: {settingElements.Count} settings migrated");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SettingsMigration] Transaction failed: {ex.Message}");
                    try { tx.Rollback(); }
                    catch (Exception rollbackEx)
                    {
                        Debug.WriteLine($"[SettingsMigration] Rollback also failed: {rollbackEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsMigration] Migration failed: {ex.Message}");
                // 迁移失败时静默忽略，后续使用默认设置
            }
        }

        /// <summary>
        /// 安全查找 user.config 文件。
        /// </summary>
        private static string[] FindUserConfigFiles(string rootDir)
        {
            try
            {
                return Directory.GetFiles(rootDir, "user.config", SearchOption.AllDirectories);
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
            catch (DirectoryNotFoundException)
            {
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 读取 settings 表中的架构版本号。
        /// </summary>
        private static int? ReadSchemaVersion(SQLiteConnection conn)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT value FROM settings WHERE key = @key";
                cmd.Parameters.AddWithValue("@key", SettingsDatabase.SchemaVersionKey);
                var result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value &&
                    int.TryParse(result.ToString(), out var version))
                {
                    return version;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 写入当前架构版本号到 settings 表。
        /// </summary>
        private static void WriteSchemaVersion(SQLiteConnection conn, SQLiteTransaction? tx = null)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                if (tx != null)
                    cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO settings (key, value)
                    VALUES (@key, @value)
                ";
                cmd.Parameters.AddWithValue("@key", SettingsDatabase.SchemaVersionKey);
                cmd.Parameters.AddWithValue("@value", SettingsDatabase.CurrentSchemaVersion.ToString());
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsMigration] Failed to write schema version: {ex.Message}");
            }
        }
    }
}
