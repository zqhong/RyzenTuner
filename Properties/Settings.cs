using System.Configuration;
using RyzenTuner.Common.SettingsStore;

namespace RyzenTuner.Properties
{
    /// <summary>
    /// 将 Settings.Default 的后备存储切换到 SQLite（完全替代 user.config / App.config）。
    /// </summary>
    [SettingsProvider(typeof(SqliteSettingsProvider))]
    internal sealed partial class Settings
    {
    }
}
