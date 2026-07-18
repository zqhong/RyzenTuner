using System.Configuration;
using RyzenTuner.Common.SettingsStore;

namespace RyzenTuner.Properties
{
    /// <summary>
    /// 将 Settings.Default 的后备存储从 user.config 文件切换到 SQLite。
    /// </summary>
    [SettingsProvider(typeof(SqliteSettingsProvider))]
    internal sealed partial class Settings
    {
    }
}
