using System;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RyzenTuner.Common.Settings
{
    /// <summary>
    /// 共享的数据库路径和连接字符串管理。
    /// 将所有 DB 相关常量集中到一处，消除跨文件的重复。
    /// </summary>
    internal static class SettingsDatabase
    {
        private const string DbFileName = "RyzenTuner.db";
        private const string CorruptedSuffix = ".corrupted";
        private const string TempFileSuffix = ".tmp";
        private const int BusyTimeoutMs = 3000;
        private static readonly object _syncRoot = new object();
        private static volatile string? _resolvedDbPath;
        private static readonly Lazy<string> _connectionString = new Lazy<string>(() =>
        {
            var dbPath = GetDbPath();
            return BuildConnectionString(dbPath);
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 新路径：%LocalAppData%\RyzenTuner\RyzenTuner.db
        /// 符合 Windows 约定，卸载后保留，且用户隔离。
        /// </summary>
        public static readonly string DefaultDbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RyzenTuner",
            DbFileName);

        /// <summary>
        /// 旧路径：应用基目录（仅用于迁移）。
        /// </summary>
        private static string LegacyDbPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);

        /// <summary>
        /// 确保指定文件路径的父目录存在。
        /// 参数为文件路径（而非目录路径），方法自动提取目录部分。
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir); // CreateDirectory is idempotent
        }

        /// <summary>
        /// 获取实际的数据库路径，首次调用时自动从旧路径迁移（线程安全，幂等）。
        /// </summary>
        public static string GetDbPath()
        {
            if (_resolvedDbPath != null)
                return _resolvedDbPath;

            lock (_syncRoot)
            {
                if (_resolvedDbPath != null)
                    return _resolvedDbPath;

                var defaultPath = DefaultDbPath;
                var legacyPath = LegacyDbPath;

                // 新路径已存在 — 校验完整性
                if (File.Exists(defaultPath))
                {
                    if (CheckDbIntegrity(defaultPath))
                    {
                        _resolvedDbPath = defaultPath;
                        return defaultPath;
                    }

                    // 文件损坏，删除后尝试从旧路径重新迁移
                    Trace.WriteLine("[SettingsDatabase] DB corruption detected, re-migrating from legacy path");
                    try { File.Delete(defaultPath); }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"[SettingsDatabase] Failed to delete corrupted DB: {ex}");
                        // 删除失败，尝试从旧路径迁移覆盖（若旧路径存在）
                        if (File.Exists(legacyPath))
                        {
                            return MigrateFromLegacy(legacyPath, defaultPath);
                        }

                        // 无法删除损坏文件，尝试重命名后创建新库
                        try
                        {
                            var corruptedPath = defaultPath + CorruptedSuffix;
                            var actualCorruptedPath = TryReplaceFile(defaultPath, corruptedPath);
                            // 标记进程退出时清理敏感数据（使用实际重命名后的路径）
                            ScheduleCorruptedFileCleanup(actualCorruptedPath);
                            EnsureDirectoryExists(defaultPath);
                            _resolvedDbPath = defaultPath;
                            return defaultPath;
                        }
                        catch (Exception ex2)
                        {
                            Trace.WriteLine($"[SettingsDatabase] Failed to rename corrupted DB: {ex2}");
                            _resolvedDbPath = defaultPath;
                            return defaultPath; // 所有恢复尝试均失败，返回当前路径（SQLite 会创建空库或使用已有文件）
                        }
                    }
                }

                // 旧路径存在而新路径不存在（或已删除）时，执行一次迁移
                if (File.Exists(legacyPath))
                {
                    return MigrateFromLegacy(legacyPath, defaultPath);
                }

                // 确保目录存在（首次运行时，defaultPath 目录还未创建）
                try
                {
                    EnsureDirectoryExists(defaultPath);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[SettingsDatabase] Failed to create directory for {defaultPath}: {ex}");
                    // 回退到应用基目录（该目录始终存在）
                    _resolvedDbPath = LegacyDbPath;
                    return _resolvedDbPath;
                }

                _resolvedDbPath = defaultPath;
                return defaultPath;
            }
        }

        /// <summary>
        /// 从旧路径迁移数据库文件到新路径。
        /// </summary>
        private static string MigrateFromLegacy(string legacyPath, string defaultPath)
        {
            var tempPath = defaultPath + TempFileSuffix;
            try
            {
                EnsureDirectoryExists(defaultPath);

                // 原子迁移：先复制到 .tmp 文件，再重命名
                File.Copy(legacyPath, tempPath, overwrite: true);

                // 验证临时文件的完整性
                if (!CheckDbIntegrity(tempPath))
                {
                    // 尝试回退到旧路径
                    if (CheckDbIntegrity(legacyPath))
                    {
                        _resolvedDbPath = legacyPath;
                        return legacyPath;
                    }

                    _resolvedDbPath = defaultPath;
                    return defaultPath; // 返回新路径（空 DB 比损坏的好）
                }

                // 使用 File.Replace 进行原子替换（调用 Win32 ReplaceFile API）
                // 当目标文件存在时：ReplaceFile 是事务性操作，崩溃时原文件保持完整
                // 当目标文件不存在时：File.Move 同卷重命名也是原子操作
                if (File.Exists(defaultPath))
                    File.Replace(tempPath, defaultPath, null);
                else
                    File.Move(tempPath, defaultPath);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[SettingsDatabase] Migration failed: {ex}");
                // 迁移失败时回退到旧路径
                _resolvedDbPath = legacyPath;
                return legacyPath;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[SettingsDatabase] Failed to clean up temp file: {ex}");
                }
            }

            _resolvedDbPath = defaultPath;
            return defaultPath;
        }

        /// <summary>
        /// 使用 SQLite 的 integrity_check 验证数据库文件完整性。
        /// 返回 true 表示文件有效或文件尚不存在（首次运行）。
        /// </summary>
        private static bool CheckDbIntegrity(string dbPath)
        {
            if (!File.Exists(dbPath))
                return true;

            try
            {
                using var conn = new SQLiteConnection(BuildConnectionString(dbPath));
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check";
                var result = cmd.ExecuteScalar() as string;
                return result == "ok";
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[SettingsDatabase] integrity_check failed for {dbPath}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 构造 SQLite 连接字符串。
        /// </summary>
        private static string BuildConnectionString(string dbPath) =>
            $"Data Source=\"{dbPath}\";Version=3;Journal Mode=WAL;Busy Timeout={BusyTimeoutMs};Connect Timeout=5;";

        /// <summary>
        /// 连接字符串，含 Busy Timeout 应对与日志组件的并发访问。
        /// 使用 Lazy&lt;string&gt; 保证线程安全并避免重复计算。
        /// </summary>
        public static string GetConnectionString() => _connectionString.Value;

        /// <summary>
        /// 替换文件：使用 File.Replace（Win32 ReplaceFile API）或 File.Move 实现。
        /// 当目标文件存在时使用 File.Replace，该操作是事务性的（崩溃时原文件保持完整）。
        /// 当目标文件不存在时使用 File.Move（同卷重命名是原子操作）。
        /// 最后的重试回退使用唯一文件名（含 GUID）以避免冲突。
        /// 采用 File.Move 直接重命名，同卷内为原子操作，不存在崩溃窗口。
        /// </summary>
        /// <returns>实际使用的目标路径（与 targetPath 相同，或在回退时包含 GUID）</returns>
        private static string TryReplaceFile(string sourcePath, string targetPath)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 100;

            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    if (File.Exists(targetPath))
                    {
                        // File.Replace 调用 Win32 ReplaceFile API，是事务性操作
                        File.Replace(sourcePath, targetPath, null);
                    }
                    else
                    {
                        // 同卷重命名是原子操作
                        File.Move(sourcePath, targetPath);
                    }

                    return targetPath;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // 目标被占用或权限不足，短暂等待后重试
                    Thread.Sleep(retryDelayMs);
                }
            }

            // 最后一次尝试：使用 GUID 唯一文件名避免冲突
            // 此处不采用 "先删除再移动" 的做法（存在崩溃窗口），而是直接移动到不冲突的唯一路径。
            if (!File.Exists(sourcePath))
            {
                // 源文件已被其他进程删除，直接返回目标路径（无操作）
                return targetPath;
            }

            var guid = Guid.NewGuid().ToString("N");
            var uniquePath = $"{targetPath}.{guid}";
            try
            {
                File.Move(sourcePath, uniquePath);
                return uniquePath;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[SettingsDatabase] Failed to move file in fallback: {ex}");
                // 所有尝试均失败，返回目标路径（无操作，调用方会处理异常情况）
                return targetPath;
            }
        }

        private static readonly ConcurrentDictionary<string, bool> _cleanupScheduled =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static int _cleanupHandlerRegistered;

        /// <summary>
        /// 安排进程退出时清理残留的 .corrupted 文件（含敏感设置数据）。
        /// 使用单个 ProcessExit 处理器和 ConcurrentDictionary 避免重复订阅。
        /// </summary>
        private static void ScheduleCorruptedFileCleanup(string path)
        {
            if (!_cleanupScheduled.TryAdd(path, true))
                return; // already scheduled

            // 使用 Interlocked.CompareExchange 确保 ProcessExit 只注册一次
            if (Interlocked.CompareExchange(ref _cleanupHandlerRegistered, 1, 0) == 0)
            {
                AppDomain.CurrentDomain.ProcessExit += OnProcessExitCleanup;
            }
        }

        /// <summary>
        /// 进程退出时清理所有已注册的 .corrupted 残留文件。
        /// 使用 single-pass 模式避免多次订阅 ProcessExit。
        /// </summary>
        private static void OnProcessExitCleanup(object? sender, EventArgs e)
        {
            foreach (var path in _cleanupScheduled.Keys)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        $"[SettingsDatabase] Failed to clean corrupted file {path}: {ex.Message}");
                }
            }
        }

        public const string CreateSettingsTableSql = @"
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            )";
    }
}
