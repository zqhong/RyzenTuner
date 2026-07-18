using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RyzenTuner.Common.SettingsStore
{
    /// <summary>
    /// 从旧的 user.config 文件向 SQLite settings 表迁移设置项。
    /// 仅在首次运行且 SQLite 表中无数据时执行。
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
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RyzenTuner.db");
                var connString = $"Data Source={dbPath};Version=3;Journal Mode=WAL;";

                using var conn = new SQLiteConnection(connString);
                conn.Open();

                // 确保 settings 表存在
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS settings (
                            key TEXT PRIMARY KEY,
                            value TEXT NOT NULL
                        )
                    ";
                    cmd.ExecuteNonQuery();
                }

                // 检查是否已有数据（已迁移或已有用户设置）
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM settings";
                    var count = (long)cmd.ExecuteScalar();
                    if (count > 0)
                        return;
                }

                // 查找旧的 user.config 文件
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var ryzenTunerDir = Path.Combine(localAppData, "RyzenTuner");

                if (!Directory.Exists(ryzenTunerDir))
                    return;

                var configFiles = Directory.GetFiles(ryzenTunerDir, "user.config", SearchOption.AllDirectories);
                if (configFiles.Length == 0)
                    return;

                // 使用最新修改的配置文件
                var latestConfig = configFiles
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .First();

                var doc = XDocument.Load(latestConfig.FullName);

                // 尝试无命名空间解析，再尝试有命名空间
                var settingElements = doc.Descendants("setting").ToList();
                if (settingElements.Count == 0)
                {
                    // .NET Framework user.config 通常无命名空间，但部分版本有
                    XNamespace ns = "urn:schemas-microsoft-com:netfx70settings";
                    settingElements = doc.Descendants(ns + "setting").ToList();
                }

                if (settingElements.Count == 0)
                    return;

                using var tx = conn.BeginTransaction();
                try
                {
                    foreach (var setting in settingElements)
                    {
                        var name = setting.Attribute("name")?.Value;
                        if (string.IsNullOrEmpty(name))
                            continue;

                        var valueElement = setting.Element("value");
                        var value = valueElement?.Value ?? string.Empty;

                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT OR REPLACE INTO settings (key, value) VALUES (@key, @value)";
                        cmd.Parameters.AddWithValue("@key", name);
                        cmd.Parameters.AddWithValue("@value", value);
                        cmd.ExecuteNonQuery();
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
                // 迁移失败时静默忽略，后续使用默认设置
            }
        }
    }
}
