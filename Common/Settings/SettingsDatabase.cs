using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace RyzenTuner.Common.Settings
{
    /// <summary>
    /// 共享的数据库路径和连接字符串管理。
    /// 将所有 DB 相关常量集中到一处，消除跨文件的重复。
    /// </summary>
    internal static class SettingsDatabase
    {
        public const string DbFileName = "RyzenTuner.db";
        public const string SettingsTableName = "settings";

        /// <summary>
        /// 新路径：%LocalAppData%\RyzenTuner\RyzenTuner.db
        /// 符合 Windows 约定，卸载后保留，且用户隔离。
        /// </summary>
        public static string DefaultDbPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RyzenTuner",
                DbFileName);

        /// <summary>
        /// 旧路径：应用基目录（仅用于迁移）。
        /// </summary>
        private static string LegacyDbPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);

        /// <summary>
        /// 确保目录存在。
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// 获取实际的数据库路径，首次调用时自动从旧路径迁移（原子操作）。
        /// </summary>
        public static string GetDbPath()
        {
            var defaultPath = DefaultDbPath;
            var legacyPath = LegacyDbPath;

            // 新路径已存在 — 校验完整性
            if (File.Exists(defaultPath))
            {
                if (DbIntegrityCheck(defaultPath))
                    return defaultPath;

                // 文件损坏，删除后尝试从旧路径重新迁移
                Debug.WriteLine("[SettingsDatabase] DB corruption detected, re-migrating from legacy path");
                try { File.Delete(defaultPath); }
                catch { return defaultPath; } // 删除失败，只能使用损坏文件
            }

            // 旧路径存在而新路径不存在（或已删除）时，执行一次迁移
            if (File.Exists(legacyPath))
            {
                try
                {
                    EnsureDirectoryExists(defaultPath);

                    // 原子迁移：先复制到 .tmp 文件，再重命名
                    var tempPath = defaultPath + ".tmp";
                    File.Copy(legacyPath, tempPath, overwrite: true);

                    // 验证临时文件的完整性
                    if (!DbIntegrityCheck(tempPath))
                    {
                        File.Delete(tempPath);
                        return defaultPath; // 返回新路径（空 DB 比损坏的好）
                    }

                    // .NET Framework 4.8 的 File.Move 不支持 overwrite 参数
                    if (File.Exists(defaultPath))
                        File.Delete(defaultPath);
                    File.Move(tempPath, defaultPath);
                }
                catch
                {
                    // 迁移失败时回退到旧路径
                    return legacyPath;
                }
            }

            return defaultPath;
        }

        /// <summary>
        /// 使用 SQLite 的 integrity_check 验证数据库文件完整性。
        /// 返回 true 表示文件有效或文件尚不存在（首次运行）。
        /// </summary>
        private static bool DbIntegrityCheck(string dbPath)
        {
            if (!File.Exists(dbPath))
                return true;

            try
            {
                using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check";
                var result = (string)cmd.ExecuteScalar();
                return result == "ok";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 连接字符串，含 Busy Timeout 应对与日志组件的并发访问。
        /// </summary>
        public static string GetConnectionString()
        {
            var dbPath = GetDbPath();
            return $"Data Source={dbPath};Version=3;Journal Mode=WAL;Busy Timeout=3000;";
        }

        public const string CreateSettingsTableSql = @"
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            )";

        /// <summary>
        /// 架构版本控制密钥（用于检测是否需要重新迁移）。
        /// </summary>
        public const string SchemaVersionKey = "__schema_version";
        public const int CurrentSchemaVersion = 2; // v1: 初始 SQLite 架构
    }
}
