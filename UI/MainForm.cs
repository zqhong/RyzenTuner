using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using RyzenTuner.Common;
using RyzenTuner.Common.Benchmark;
using RyzenTuner.Common.Container;
using RyzenTuner.Common.Settings;
using RyzenTuner.Properties;
using RyzenTuner.Utils;

namespace RyzenTuner.UI
{
    public partial class MainForm : Form
    {
        private Int64 _tickCount;
        private DateTime _lastLogCleanupTime = DateTime.MinValue;
        private string _lastPowerLimitApplyError = string.Empty;
        private bool? _lastCpuBoostEnabled;
        private DateTime _lastPowerLimitErrorShownAt = DateTime.MinValue;
        private DateTime _lastPowerLimitErrorTime = DateTime.MinValue;
        private DateTime _lastSuccessfulApplyTime = DateTime.MinValue;
        private string _preErrorMode = MODE_BALANCED;
        private bool _isErrorRecoveryPending;
        private bool _isApplyingPowerLimit;
        private bool _isChangingMode;
        private bool _isInitializingOptions;
        private DateTime _lastPowerLimitRunTime = DateTime.MinValue;
        private bool _isBenchmarkRunning;
        private bool _aboutInfoLoaded;

        // --- 跑分引擎字段 ---
        private BenchmarkEngine? _engine;
        private readonly List<BenchmarkTestPoint> _allResults = new();
        private BenchmarkTestType _benchmarkTestType;
        private int _benchmarkVersion;
        // 是否需要运行 BoostAllUserBackgroundProcesses 任务
        private bool _needRunBoostAllBgProcesses;

        // ================================================================
        // 全局快捷键
        // ================================================================

        private const int WM_HOTKEY = 0x0312;

        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;
        private const int MOD_NOREPEAT = 0x4000;

        private const int HOTKEY_ID_POWERSAVE = 1;
        private const int HOTKEY_ID_BALANCED = 2;
        private const int HOTKEY_ID_PERFORMANCE = 3;

        // ================================================================
        // 模式名称常量（避免魔法字符串）
        // ================================================================

        private const string MODE_POWER_SAVE = "PowerSaveMode";
        private const string MODE_BALANCED = "BalancedMode";
        private const string MODE_PERFORMANCE = "PerformanceMode";

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        /// <summary>
        /// 检测当前 Win 键是否处于按下状态（弥补 KeyEventArgs.Modifiers 不报告 Win 键的缺陷）
        /// </summary>
        private static bool IsWinKeyPressed()
        {
            return (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;
        }

        /// <summary>
        /// 判断指定 Keys 值是否为修饰键
        /// </summary>
        private static bool IsModifierKey(Keys key)
        {
            return key == Keys.ControlKey ||
                   key == Keys.LControlKey ||
                   key == Keys.RControlKey ||
                   key == Keys.Menu ||
                   key == Keys.LMenu ||
                   key == Keys.RMenu ||
                   key == Keys.ShiftKey ||
                   key == Keys.LShiftKey ||
                   key == Keys.RShiftKey ||
                   key == Keys.LWin ||
                   key == Keys.RWin;
        }

#if DEBUG
        private static string GetDebugBuildSuffix()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var buildTime = File.GetLastWriteTime(assembly.Location);
                return $" [Debug Build: {buildTime:yyyy-MM-dd HH:mm}]";
            }
            catch
            {
                return " [Debug Build]";
            }
        }
#endif

        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 返回图标（在 Designer.cs 外部实现，避免 Rider CodeDom 解析器卡死）
        /// </summary>
        private static Icon GetIcon()
        {
            if (_cachedIcon != null)
                return _cachedIcon;
            try
            {
                _cachedIcon = Icon.ExtractAssociatedIcon(
                    Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
                // 忽略图标提取失败
            }

            _cachedIcon ??= SystemIcons.Application;
            return _cachedIcon;
        }

        // ================================================================
        // 导航图标
        // ================================================================

        /// <summary>
        /// 初始化导航按钮图标（程序绘制 24x24 图标）
        /// </summary>
        private void InitializeNavIcons()
        {
            imageListNavIcons.Images.Clear();

            imageListNavIcons.Images.Add("navHome", CreateNavIcon("navHome"));
            imageListNavIcons.Images.Add("navSettings", CreateNavIcon("navSettings"));
            imageListNavIcons.Images.Add("navBenchmark", CreateNavIcon("navBenchmark"));
            imageListNavIcons.Images.Add("navLogs", CreateNavIcon("navLogs"));
            imageListNavIcons.Images.Add("navAbout", CreateNavIcon("navAbout"));

            navHome.ImageKey = "navHome";
            navSettings.ImageKey = "navSettings";
            navBenchmark.ImageKey = "navBenchmark";
            navLogs.ImageKey = "navLogs";
            navAbout.ImageKey = "navAbout";
        }

        /// <summary>
        /// 绘制单个导航图标（纯 GDI+ 绘制，无外部资源依赖）
        /// </summary>
        private static Bitmap CreateNavIcon(string iconName)
        {
            var bmp = new Bitmap(24, 24);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var color = Color.FromArgb(80, 80, 80);
            using var brush = new SolidBrush(color);
            using var bgBrush = new SolidBrush(Color.FromArgb(215, 215, 215));

            switch (iconName)
            {
                case "navHome":
                    // 房屋：屋顶 + 墙体 + 门
                    g.FillPolygon(brush, new[] { new Point(12, 2), new Point(2, 9), new Point(22, 9) });
                    g.FillRectangle(brush, 4, 9, 16, 13);
                    g.FillRectangle(bgBrush, 10, 14, 4, 8);
                    break;

                case "navSettings":
                    // 齿轮：外圈 + 内孔 + 4 个齿
                    g.FillEllipse(brush, 5, 5, 14, 14);
                    g.FillEllipse(bgBrush, 8, 8, 8, 8);
                    g.FillRectangle(brush, 10, 1, 4, 5);
                    g.FillRectangle(brush, 10, 18, 4, 5);
                    g.FillRectangle(brush, 1, 10, 5, 4);
                    g.FillRectangle(brush, 18, 10, 5, 4);
                    break;

                case "navBenchmark":
                    // 柱状图：3 根递增柱
                    g.FillRectangle(brush, 4, 11, 4, 9);
                    g.FillRectangle(brush, 10, 6, 4, 14);
                    g.FillRectangle(brush, 16, 2, 4, 18);
                    break;

                case "navLogs":
                    // 文档列表：矩形 + 横线
                    g.FillRectangle(brush, 3, 2, 18, 20);
                    g.FillRectangle(bgBrush, 6, 7, 12, 2);
                    g.FillRectangle(bgBrush, 6, 12, 12, 2);
                    g.FillRectangle(bgBrush, 6, 17, 8, 2);
                    break;

                case "navAbout":
                    // 信息圆圈：圆圈 + "i" 点 + 竖线
                    g.FillEllipse(brush, 2, 2, 20, 20);
                    g.FillEllipse(bgBrush, 10, 6, 4, 4);
                    g.FillRectangle(bgBrush, 11, 12, 2, 6);
                    break;
            }

            return bmp;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

#if DEBUG
            Text += GetDebugBuildSuffix();
#endif

            // 修复设计器遗漏：pageLog.SuspendLayout() 后从未调用 ResumeLayout，
            // 导致其内部控件的 Anchor/Dock 布局从未计算，表格始终为固定宽度。
            // 必须在 Load 阶段立即恢复，延迟到 Resize 或页切换后再做已来不及。
            pageLog.ResumeLayout(true);

            // 同样修复：dataGridViewLogs.BeginInit() 后缺少对应的 EndInit()
            ((ISupportInitialize)dataGridViewLogs).EndInit();

            // 修复设计器遗漏：groupBoxLogSettings.SuspendLayout() 后缺少 ResumeLayout()
            groupBoxLogSettings.ResumeLayout(false);

            // 修复设计器遗漏：numericUpDownLogSaveDays.BeginInit() 后缺少 EndInit()
            ((ISupportInitialize)numericUpDownLogSaveDays).EndInit();

            // 初始化导航按钮图标
            InitializeNavIcons();

            // 运行时启动定时器
            mainFormTimer.Enabled = true;

            _isInitializingOptions = true;
            checkBoxEnergyStar.Checked = AppSettings.GetBool("EnergyStar");
            keepAwakeCheckBox.Checked = AppSettings.GetBool("KeepAwake");
            launchAtLogonCheckBox.Checked = AppSettings.GetBool("LaunchAtLogon");
            cpuBoostCheckBox.Checked = AppSettings.GetBool("CpuBoostEnabled");
            SyncLaunchAtLogonSetting();
            SyncCpuBoostSetting();
            SyncEnergyModeSelection();

            // 初始化语言选择（在 _isInitializingOptions 保护内，避免 SelectedIndexChanged 误触发）
            InitLanguageSelection();

            _isInitializingOptions = false;

            // 设置系统唤醒状态
            keepAwakeCheckBox_CheckedChanged(null, EventArgs.Empty);

            // 若 EnergyStar 禁用，确保下次 DoProcessManage 提升后台进程（修复崩溃重启后进程被节流的问题）
            _needRunBoostAllBgProcesses = !checkBoxEnergyStar.Checked;

            // 初始化设置页
            SettingsLoadValues();

            // 初始化跑分页
            SetupBenchmarkDataGridView();

            // 刷新首页模式标签（Designer 中只显示模式名，运行时补上功率值）
            try
            {
                RefreshModeLabels();
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning("UI", $"刷新模式标签失败: {ex.Message}");
            }

            // 日志：记录启动事件
            AppContainer.Logger().Info("System", "RyzenTuner started");

            // 日志：清理过期日志
            try
            {
                AppContainer.Logger().Cleanup(AppSettings.Get("LogRetentionDays", 3));
                _lastLogCleanupTime = DateTime.UtcNow;
            }
            catch (Exception cleanupEx)
            {
                AppContainer.Logger().Warning("System", $"启动时日志清理失败: {cleanupEx.Message}");
            }

            // 初始化布局（关于页延迟到首次访问时加载）
            // RecalcCardColumns 在 Form1_Shown 中调用，此时布局已最终确定
        }

        // ================================================================
        // 页面切换
        // ================================================================

        private void NavButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                var pageId = "";
                if (btn == navHome) pageId = "home";
                else if (btn == navSettings) pageId = "settings";
                else if (btn == navBenchmark) pageId = "benchmark";
                else if (btn == navLogs) pageId = "log";
                else if (btn == navAbout) pageId = "about";

                SwitchPage(pageId);
            }
        }

        private void SwitchPage(string pageId)
        {
            // 跑分进行中时禁止离开跑分页（无法从其他页面停止跑分）
            if (_isBenchmarkRunning && pageId != "benchmark")
            {
                MessageBox.Show(
                    Strings.TextBenchmarkRunning,
                    Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // 隐藏所有页面
            pageHome.Visible = false;
            pageSettings.Visible = false;
            pageBenchmark.Visible = false;
            pageAbout.Visible = false;
            pageLog.Visible = false;

            // 重置导航按钮背景
            navHome.BackColor = Color.Transparent;
            navSettings.BackColor = Color.Transparent;
            navBenchmark.BackColor = Color.Transparent;
            navLogs.BackColor = Color.Transparent;
            navAbout.BackColor = Color.Transparent;

            // 显示目标页面
            switch (pageId)
            {
                case "settings":
                    pageSettings.Visible = true;
                    navSettings.BackColor = Color.White;
                    SettingsLoadValues();
                    break;
                case "benchmark":
                    pageBenchmark.Visible = true;
                    navBenchmark.BackColor = Color.White;
                    break;
                case "log":
                    pageLog.Visible = true;
                    navLogs.BackColor = Color.White;
                    LoadLogViewerData();
                    break;
                case "about":
                    pageAbout.Visible = true;
                    navAbout.BackColor = Color.White;
                    if (!_aboutInfoLoaded)
                    {
                        LoadAboutInfo();
                        _aboutInfoLoaded = true;
                    }
                    break;
                default:
                    pageHome.Visible = true;
                    navHome.BackColor = Color.White;
                    break;
            }
        }

        // ================================================================
        // 关于页
        // ================================================================

        private void LoadAboutInfo()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            version = Regex.Replace(version, @"(\d+\.\d+\.\d+)\.\d+", "$1");

            var year = DateTime.Now.Year.ToString();
            var ryzenadjDate = LoadRyzenAdjDate();
            var buildTime = LoadBuildTime();

            labelAboutVersion.Text = Strings.TextAboutVersion.Replace("{version}", version);
            labelAboutCopyright.Text = Strings.TextAboutCopyright.Replace("{year}", year);
            labelAboutBuildTime.Text = Strings.TextAboutBuildTime.Replace("{build_time}", buildTime);
            labelAboutRyzenAdj.Text = Strings.TextAboutRyzenAdj.Replace("{ryzenadj_date}", ryzenadjDate);
        }

        private static string LoadRyzenAdjDate()
        {
            try
            {
                var ryzenadjPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libryzenadj.dll");
                if (!File.Exists(ryzenadjPath))
                    return "N/A";

                using var stream = File.OpenRead(ryzenadjPath);
                using var reader = new BinaryReader(stream);

                stream.Seek(0x3C, SeekOrigin.Begin);
                var peOffset = reader.ReadInt32();
                stream.Seek(peOffset + 8, SeekOrigin.Begin);
                var timestamp = reader.ReadUInt32();

                if (timestamp == 0)
                    return "N/A";

                var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(timestamp)
                    .ToLocalTime();

                return date.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "N/A";
            }
        }

        private static string LoadBuildTime()
        {
            try
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(assemblyPath) || !File.Exists(assemblyPath))
                    return "N/A";

                var lastWriteTimeUtc = File.GetLastWriteTimeUtc(assemblyPath);
                var chinaTz = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                var chinaTime = TimeZoneInfo.ConvertTimeFromUtc(lastWriteTimeUtc, chinaTz);

                return chinaTime.ToString("yyyy-MM-dd HH:mm:ss") + " +08:00";
            }
            catch
            {
                return "N/A";
            }
        }

        // ================================================================
        // 设置页（内嵌 SettingsForm 逻辑）
        // ================================================================

        private void SettingsLoadValues()
        {
            TrySetNumericValue(numericUpDownPowerSaveMode, "PowerSaveMode");
            TrySetNumericValue(numericUpDownBalancedMode, "BalancedMode");
            TrySetNumericValue(numericUpDownPerformanceMode, "PerformanceMode");

            numericUpDownTctlTemp.Value = ClampNumeric(AppSettings.Get("TctlTemp", 100), numericUpDownTctlTemp);
            numericUpDownApuSkinTemp.Value = ClampNumeric(AppSettings.Get("ApuSkinTemp", 43), numericUpDownApuSkinTemp);
            numericUpDownPowerLimitUpdateInterval.Value = ClampNumeric(AppSettings.Get("PowerLimitUpdateInterval", 4), numericUpDownPowerLimitUpdateInterval);

            // 加载快捷键设置
            LoadHotkeySettings();

            // 加载日志设置
            var logLevel = AppSettings.Get("LogLevel", "Warning");
            for (var i = 0; i < comboBoxLogLevel.Items.Count; i++)
            {
                if (comboBoxLogLevel.Items[i].ToString() == logLevel)
                {
                    comboBoxLogLevel.SelectedIndex = i;
                    break;
                }
            }

            // 将已保存的日志级别应用到运行时日志记录器
            try
            {
                AppContainer.Logger().DefaultLogLevel =
                    AppContainer.Logger().ToLogLevel(logLevel);
            }
            catch
            {
                // 忽略日志级别解析失败
            }

            numericUpDownLogSaveDays.Value = ClampNumeric(AppSettings.Get("LogRetentionDays", 3), numericUpDownLogSaveDays);
        }

        private static void TrySetNumericValue(NumericUpDown control, string mode)
        {
            // 复用已有的双文化解析 — SettingsDefault 的[ ]索引器可能抛异常，
            // 但调用方已确保 mode 是有效的已定义模式名
            if (RyzenTunerUtils.TryGetPowerLimitByMode(mode, out var result))
            {
                control.Value = ClampNumeric((decimal)result, control);
            }
        }

        private static decimal ClampNumeric(decimal value, NumericUpDown control)
        {
            if (value < control.Minimum) return control.Minimum;
            if (value > control.Maximum) return control.Maximum;
            return value;
        }

        private static int ClampNumeric(int value, NumericUpDown control)
        {
            if (value < (int)control.Minimum) return (int)control.Minimum;
            if (value > (int)control.Maximum) return (int)control.Maximum;
            return value;
        }

        private void SettingsSave_Click(object? sender, EventArgs e)
        {
            // ===== 收集新的快捷键值 =====
            var newHotkeyPowerSave = GetHotkeyFromTextBox(textBoxHotkeyPowerSave);
            var newHotkeyBalanced = GetHotkeyFromTextBox(textBoxHotkeyBalanced);
            var newHotkeyPerformance = GetHotkeyFromTextBox(textBoxHotkeyPerformance);

            // ===== 检查快捷键是否有变化 =====
            var oldHotkeyPowerSave = AppSettings.Get("HotkeyPowerSaveMode", "");
            var oldHotkeyBalanced = AppSettings.Get("HotkeyBalancedMode", "");
            var oldHotkeyPerformance = AppSettings.Get("HotkeyPerformanceMode", "");

            var hotkeyPowerSaveChanged = newHotkeyPowerSave != oldHotkeyPowerSave;
            var hotkeyBalancedChanged = newHotkeyBalanced != oldHotkeyBalanced;
            var hotkeyPerformanceChanged = newHotkeyPerformance != oldHotkeyPerformance;

            // ===== 检查同一组合键是否被分配给多个模式 =====
            var hotkeyValues = new[] { newHotkeyPowerSave, newHotkeyBalanced, newHotkeyPerformance };
            var duplicateKeys = hotkeyValues
                .Select((v, i) => new { Value = v, Index = i })
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .GroupBy(x => x.Value)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Select(x => GetHotkeyDisplayText(x.Value)))
                .Distinct()
                .ToList();

            if (duplicateKeys.Count > 0)
            {
                var conflictMsg = string.Join("\r\n", duplicateKeys);
                MessageBox.Show(
                    Strings.TextHotkeyConflict
                        .Replace("{hotkey}", conflictMsg)
                        .Replace("\n", "\r\n"),
                    Strings.TextHotkeyConflictTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // ===== 只有快捷键有变化时才验证冲突 =====
            var hasHotkeyChanges = hotkeyPowerSaveChanged ||
                                    hotkeyBalancedChanged ||
                                    hotkeyPerformanceChanged;

            if (hasHotkeyChanges)
            {
                // 先注销旧的，再尝试注册新的
                UnregisterAllHotkeys();

                var conflictList = new List<string>();

                if (hotkeyPowerSaveChanged && !TryRegisterHotkey(newHotkeyPowerSave, HOTKEY_ID_POWERSAVE))
                    conflictList.Add(GetHotkeyDisplayText(newHotkeyPowerSave));
                if (hotkeyBalancedChanged && !TryRegisterHotkey(newHotkeyBalanced, HOTKEY_ID_BALANCED))
                    conflictList.Add(GetHotkeyDisplayText(newHotkeyBalanced));
                if (hotkeyPerformanceChanged && !TryRegisterHotkey(newHotkeyPerformance, HOTKEY_ID_PERFORMANCE))
                    conflictList.Add(GetHotkeyDisplayText(newHotkeyPerformance));

                // 恢复未更改的快捷键注册
                if (!hotkeyPowerSaveChanged)
                    TryRegisterHotkey(oldHotkeyPowerSave, HOTKEY_ID_POWERSAVE);
                if (!hotkeyBalancedChanged)
                    TryRegisterHotkey(oldHotkeyBalanced, HOTKEY_ID_BALANCED);
                if (!hotkeyPerformanceChanged)
                    TryRegisterHotkey(oldHotkeyPerformance, HOTKEY_ID_PERFORMANCE);

                if (conflictList.Count > 0)
                {
                    // 恢复旧的快捷键
                    UnregisterAllHotkeys();

                    var recoveryFailed = false;
                    if (!string.IsNullOrEmpty(oldHotkeyPowerSave))
                        recoveryFailed |= !TryRegisterHotkey(oldHotkeyPowerSave, HOTKEY_ID_POWERSAVE);
                    if (!string.IsNullOrEmpty(oldHotkeyBalanced))
                        recoveryFailed |= !TryRegisterHotkey(oldHotkeyBalanced, HOTKEY_ID_BALANCED);
                    if (!string.IsNullOrEmpty(oldHotkeyPerformance))
                        recoveryFailed |= !TryRegisterHotkey(oldHotkeyPerformance, HOTKEY_ID_PERFORMANCE);

                    // 恢复文本框显示
                    SetHotkeyTextBox(textBoxHotkeyPowerSave, oldHotkeyPowerSave);
                    SetHotkeyTextBox(textBoxHotkeyBalanced, oldHotkeyBalanced);
                    SetHotkeyTextBox(textBoxHotkeyPerformance, oldHotkeyPerformance);

                    var conflictMsg = string.Join("\r\n", conflictList);
                    var fullMsg = Strings.TextHotkeyConflict
                        .Replace("{hotkey}", conflictMsg)
                        .Replace("\n", "\r\n");

                    if (recoveryFailed)
                    {
                        fullMsg += "\r\n\r\n" + Strings.TextHotkeyConflictTitle +
                                   ": " + Strings.TextHotkeyRecoveryFailed;
                    }

                    MessageBox.Show(
                        fullMsg,
                        Strings.TextHotkeyConflictTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }
            // ===== 快捷键验证通过，保存所有设置 =====
            AppSettings.Set("PowerSaveMode", numericUpDownPowerSaveMode.Value.ToString("F0", CultureInfo.InvariantCulture));
            AppSettings.Set("BalancedMode", numericUpDownBalancedMode.Value.ToString("F0", CultureInfo.InvariantCulture));
            AppSettings.Set("PerformanceMode", numericUpDownPerformanceMode.Value.ToString("F0", CultureInfo.InvariantCulture));

            AppSettings.Set("TctlTemp", (int)numericUpDownTctlTemp.Value);
            AppSettings.Set("ApuSkinTemp", (int)numericUpDownApuSkinTemp.Value);
            AppSettings.Set("PowerLimitUpdateInterval", (int)numericUpDownPowerLimitUpdateInterval.Value);

            // 保存快捷键设置（快捷键已经注册成功，只需保存值）
            AppSettings.Set("HotkeyPowerSaveMode", newHotkeyPowerSave);
            AppSettings.Set("HotkeyBalancedMode", newHotkeyBalanced);
            AppSettings.Set("HotkeyPerformanceMode", newHotkeyPerformance);

            // 保存日志设置
            var selectedLogLevel = comboBoxLogLevel.SelectedItem?.ToString() ?? "Warning";
            AppSettings.Set("LogLevel", selectedLogLevel);
            AppSettings.Set("LogRetentionDays", (int)numericUpDownLogSaveDays.Value);

            // 应用日志级别到运行时日志记录器
            try
            {
                AppContainer.Logger().DefaultLogLevel =
                    AppContainer.Logger().ToLogLevel(selectedLogLevel);
            }
            catch (Exception logEx)
            {
                AppContainer.Logger().Warning("System", $"应用日志级别失败: {logEx.Message}");
            }

            // 保存后立即执行日志清理
            try
            {
                AppContainer.Logger().Cleanup(AppSettings.Get("LogRetentionDays", 3));
            }
            catch (Exception cleanupEx)
            {
                AppContainer.Logger().Warning("LogCleanup", $"日志清理失败: {cleanupEx.Message}");
            }

            // 刷新首页模式标签
            try
            {
                RefreshModeLabels();
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning("UI", $"刷新模式标签失败: {ex.Message}");
            }

            // 立即重新应用功率限制
            DoPowerLimit();
            notifyIcon1.Text = RyzenTunerUtils.GetLocalizedModeName(AppSettings.Get("CurrentMode", "BalancedMode"));
        }

        private void SettingsCancel_Click(object? sender, EventArgs e)
        {
            SettingsLoadValues();
            SwitchPage("home");
        }

        // ================================================================
        // 语言设置
        // ================================================================

        private void InitLanguageSelection()
        {
            try
            {
                var currentLang = AppSettings.Get("Language", "");
                if (string.IsNullOrEmpty(currentLang))
                {
                    currentLang = RyzenTunerUtils.DetectDefaultLanguageCode();
                }

                // 通过 Key 查找匹配项，避免魔数索引
                foreach (KeyValuePair<string, string> item in comboBoxLanguage.Items)
                {
                    if (item.Key == currentLang)
                    {
                        comboBoxLanguage.SelectedItem = item;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning("UI", $"初始化语言选择失败: {ex.Message}");

                // Fix #10: 设置一个安全的 fallback，保持 UI 一致
                try
                {
                    var fallback = RyzenTunerUtils.DetectDefaultLanguageCode();
                    foreach (KeyValuePair<string, string> item in comboBoxLanguage.Items)
                    {
                        if (item.Key == fallback)
                        {
                            comboBoxLanguage.SelectedItem = item;
                            return;
                        }
                    }
                }
                catch { /* 静默 — combo box 无选中项也可接受 */ }
            }
        }

        private void ComboBoxLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // 初始化阶段不保存语言设置（由 AutoSelectLang 处理）
            if (_isInitializingOptions)
                return;

            if (comboBoxLanguage.SelectedItem is not KeyValuePair<string, string> selected)
                return;

            var newLang = selected.Key;
            if (newLang == AppSettings.Get("Language", ""))
                return;

            // Fix #7: 先询问用户是否重启，再保存
            var result = MessageBox.Show(
                Strings.TextLanguageRestartHint,
                Strings.TextSettingsTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                // 用户取消 — 恢复 combo box 到当前保存的语言
                InitLanguageSelection();
                return;
            }

            // 用户确认重启后保存语言设置
            AppSettings.Set("Language", newLang);

            // Fix #6: 先释放 Mutex 再启动新进程，异常时恢复
            Program.ReleaseInstanceMutex();
            try
            {
                Process.Start(Application.ExecutablePath);
                Application.Exit();
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Error("System", $"重启失败: {ex.Message}");

                // Process.Start 失败 — 尝试重新获取 Mutex 恢复单例保护
                if (!Program.TryReacquireInstanceMutex())
                {
                    AppContainer.Logger().Error("System", "重新获取单例 Mutex 失败");
                }

                MessageBox.Show(
                    $"{Strings.TextLanguageRestartHint}\n\n启动新进程失败: {ex.Message}",
                    Strings.TextSettingsTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ================================================================
        // 跑分页（内嵌 BenchmarkForm 逻辑）
        // ================================================================

        private void SetupBenchmarkDataGridView()
        {
            dataGridViewResults.Columns.Clear();

            var columns = new[]
            {
                Strings.TextBenchmarkSetPower,
                Strings.TextBenchmarkScore,
                Strings.TextBenchmarkPowerMin,
                Strings.TextBenchmarkPowerMax,
                Strings.TextBenchmarkPowerAvg,
                Strings.TextBenchmarkPowerMid,
                Strings.TextBenchmarkTempMin,
                Strings.TextBenchmarkTempMax,
                Strings.TextBenchmarkTempAvg,
                Strings.TextBenchmarkTempMid,
                Strings.TextBenchmarkFreq,
                Strings.TextBenchmarkEfficiency,
                Strings.TextBenchmarkCapability
            };

            foreach (var header in columns)
            {
                dataGridViewResults.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = header,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                });
            }

            if (dataGridViewResults.Columns.Count >= 13)
            {
                dataGridViewResults.Columns[0].FillWeight = 80;
                dataGridViewResults.Columns[1].FillWeight = 100;
                for (var i = 2; i <= 11; i++)
                    dataGridViewResults.Columns[i].FillWeight = 90;
                dataGridViewResults.Columns[12].FillWeight = 120;
            }

            buttonExportCsv.Enabled = false;
        }

        private void EnableBenchmarkConfig(bool enabled)
        {
            comboBoxTestType.Enabled = enabled;
            numericUpDownStartPower.Enabled = enabled;
            numericUpDownStep.Enabled = enabled;
            numericUpDownEndPower.Enabled = enabled;
            numericUpDownDuration.Enabled = enabled;
            numericUpDownRestTime.Enabled = enabled;
            buttonStart.Enabled = enabled;
            buttonStop.Enabled = !enabled;
        }

        private async void BenchmarkStart_Click(object? sender, EventArgs e)
        {
            if (_isBenchmarkRunning)
            {
                MessageBox.Show(
                    Strings.TextBenchmarkRunning,
                    Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var config = new BenchmarkConfig
            {
                TestType = comboBoxTestType.SelectedIndex == 0
                    ? BenchmarkTestType.SingleCore
                    : BenchmarkTestType.MultiCore,
                StartTdp = (float)numericUpDownStartPower.Value,
                StepTdp = (float)numericUpDownStep.Value,
                EndTdp = (float)numericUpDownEndPower.Value,
                DurationSeconds = (int)numericUpDownDuration.Value * 60,
                RestSeconds = (int)numericUpDownRestTime.Value,
            };

            if (config.StartTdp > config.EndTdp)
            {
                MessageBox.Show(
                    Strings.TextBenchmarkErrorNoData,
                    Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var pointCount = config.GetTestPointCount();
            var totalMinutes = pointCount * (int)numericUpDownDuration.Value
                               + Math.Max(0, pointCount - 1) * (int)numericUpDownRestTime.Value / 60;

            var confirmMsg = Strings.TextBenchmarkConfirmStart
                .Replace("{count}", pointCount.ToString())
                .Replace("{time}", totalMinutes.ToString());

            if (MessageBox.Show(confirmMsg,
                    Strings.TextBenchmarkTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _isBenchmarkRunning = true;
            _benchmarkVersion++;
            var capturedVersion = _benchmarkVersion;
            _benchmarkTestType = config.TestType;
            _allResults.Clear();
            dataGridViewResults.Rows.Clear();
            buttonExportCsv.Enabled = false;
            progressBar.Visible = true;
            progressBar.Maximum = pointCount;
            progressBar.Value = 0;

            EnableBenchmarkConfig(false);
            labelStatus.Text = Strings.TextBenchmarkRunning;

            try
            {
                using (_engine = new BenchmarkEngine())
                {
                    _engine.OnProgressChanged += (current, total) =>
                    {
                        if (IsDisposed) return;
                        BeginInvoke(new Action(() =>
                        {
                            progressBar.Value = Math.Min(current, total);
                        }));
                    };

                    _engine.OnStatusChanged += (msg) =>
                    {
                        if (IsDisposed) return;
                        BeginInvoke(new Action(() =>
                        {
                            labelStatus.Text = msg;
                        }));
                    };

                    _engine.OnTestPointCompleted += (point) =>
                    {
                        if (IsDisposed) return;
                        BeginInvoke(new Action(() =>
                        {
                            _allResults.Add(point);
                            AddBenchmarkResultRow(point);
                        }));
                    };

                    _engine.OnCompleted += (_) =>
                    {
                        if (IsDisposed) return;
                        BeginInvoke(new Action(() =>
                        {
                            if (capturedVersion != _benchmarkVersion) return;
							if (_allResults.Count > 0)
                            {
                                RefreshAllResults();
                            }

                            buttonExportCsv.Enabled = _allResults.Count > 0;
                            EnableBenchmarkConfig(true);
                            progressBar.Visible = false;
                            // _isBenchmarkRunning 和 _engine 由 finally 块统一清理
                        }));
                    };

                    _engine.OnError += (error) =>
                    {
                        if (IsDisposed) return;
                        BeginInvoke(new Action(() =>
                        {
                            labelStatus.Text = error;
                            if (capturedVersion != _benchmarkVersion) return;
                            buttonExportCsv.Enabled = _allResults.Count > 0;
                            EnableBenchmarkConfig(true);
                            progressBar.Visible = false;
                            // _isBenchmarkRunning 和 _engine 由 finally 块统一清理
                            AppContainer.Logger().Error("Benchmark", $"能效分析错误: {error}");
                        }));
                    };

                    await _engine.RunAsync(config);
                }
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Error("Benchmark", $"跑分未处理异常: {ex}");
            }
            finally
            {
                // 异常保护：若引擎 setup/RunAsync 同步抛出导致 OnCompleted/OnError 未触发，
                // 确保 _isBenchmarkRunning 被重置，避免 DoPowerLimit 永久跳过
                if (_isBenchmarkRunning)
                {
                    _isBenchmarkRunning = false;
                    EnableBenchmarkConfig(true);
                    progressBar.Visible = false;
                    _engine = null;
                }
            }
        }

        private void BenchmarkStop_Click(object? sender, EventArgs e)
        {
            if (_engine == null || !_engine.IsRunning)
                return;

            var result = MessageBox.Show(
                Strings.TextBenchmarkCancel,
                Strings.TextBenchmarkTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _engine.Stop();
                EnableBenchmarkConfig(true);
                progressBar.Visible = false;
                // 注意：不清除 _isBenchmarkRunning，避免 DoPowerLimit 在引擎 cleanup
                // （RestoreOriginalSettings → ApplyTdpLimit）完成前进入并发写 SMU 寄存器。
                // OnCompleted/OnError 的 BeginInvoke 回调会在 cleanup 后清除此标志。
                labelStatus.Text = Strings.TextBenchmarkStopped;
            }
        }

        private void BenchmarkExportCsv_Click(object? sender, EventArgs e)
        {
            if (_allResults.Count == 0)
            {
                MessageBox.Show(
                    Strings.TextBenchmarkExportNoData,
                    Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = Strings.TextBenchmarkExportSaveFilter,
                FilterIndex = 1,
                RestoreDirectory = true,
                FileName =
                    $"{Strings.TextBenchmarkExportFileName}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                ExportResultsToCsv(sfd.FileName);
                MessageBox.Show(
                    Strings.TextBenchmarkExportSuccess.Replace("{path}", sfd.FileName),
                    Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Error("Benchmark", $"导出 CSV 失败: {ex}");
                MessageBox.Show(
                    $"{Strings.TextBenchmarkExportFailed}: {ex.Message}",
                    Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ExportResultsToCsv(string filePath)
        {
            var sb = new StringBuilder();
            var testType = _benchmarkTestType == BenchmarkTestType.SingleCore
                ? Strings.TextBenchmarkSingleCore
                : Strings.TextBenchmarkMultiCore;
            sb.AppendLine($"# {Strings.TextBenchmarkTitle}");
            sb.AppendLine($"# {Strings.TextBenchmarkTestType}: {testType}");
            sb.AppendLine($"# {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            var headers = dataGridViewResults.Columns
                .Cast<DataGridViewColumn>()
                .Select(c => c.HeaderText);

            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

            foreach (var r in _allResults)
            {
                var ci = CultureInfo.InvariantCulture;
                var values = new[]
                {
                    r.SetTdp.ToString("F0", ci),
                    r.Score.ToString("D", ci),
                    r.PowerMin.ToString("F2", ci),
                    r.PowerMax.ToString("F2", ci),
                    r.PowerAvg.ToString("F2", ci),
                    r.PowerMedian.ToString("F2", ci),
                    r.TempMin.ToString("F1", ci),
                    r.TempMax.ToString("F1", ci),
                    r.TempAvg.ToString("F1", ci),
                    r.TempMedian.ToString("F1", ci),
                    r.CpuFreqAvg.ToString("F0", ci),
                    r.Efficiency.ToString("F0", ci),
                    r.Capability.ToString("P0", ci),
                };
                sb.AppendLine(string.Join(",", values.Select(EscapeCsvField)));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsvField(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        private void AddBenchmarkResultRow(BenchmarkTestPoint point)
        {
            var rowIndex = dataGridViewResults.Rows.Add(
                $"{point.SetTdp:F0}",
                point.Score.ToString("N0"),
                $"{point.PowerMin:F2}",
                $"{point.PowerMax:F2}",
                $"{point.PowerAvg:F2}",
                $"{point.PowerMedian:F2}",
                $"{point.TempMin:F1}",
                $"{point.TempMax:F1}",
                $"{point.TempAvg:F1}",
                $"{point.TempMedian:F1}",
                $"{point.CpuFreqAvg:F0}",
                $"{point.Efficiency:F0}",
                $"{point.Capability:P0}"
            );

            if (rowIndex >= 0)
            {
                dataGridViewResults.FirstDisplayedScrollingRowIndex = rowIndex;
            }

            HighlightBestRow();
        }

        private void RefreshAllResults()
        {
            for (var i = 0; i < _allResults.Count && i < dataGridViewResults.Rows.Count; i++)
            {
                var r = _allResults[i];
                dataGridViewResults.Rows[i].Cells[1].Value = r.Score.ToString("N0");
                dataGridViewResults.Rows[i].Cells[11].Value = r.Efficiency.ToString("F0");
                dataGridViewResults.Rows[i].Cells[12].Value = r.Capability.ToString("P0");
            }

            HighlightBestRow();
        }

        private void HighlightBestRow()
        {
            foreach (DataGridViewRow row in dataGridViewResults.Rows)
            {
                row.DefaultCellStyle.BackColor = SystemColors.Window;
                row.DefaultCellStyle.ForeColor = SystemColors.ControlText;
            }

            if (_allResults.Count == 0 || dataGridViewResults.Rows.Count == 0)
                return;

            var bestEfficiency = float.MinValue;
            var bestRowIndex = -1;

            // 直接从 _allResults 读取数据，避免字符串 → 浮点的往返转换
            for (var i = 0; i < _allResults.Count && i < dataGridViewResults.Rows.Count; i++)
            {
                var eff = _allResults[i].Efficiency;
                if (eff > bestEfficiency)
                {
                    bestEfficiency = eff;
                    bestRowIndex = i;
                }
            }

            if (bestRowIndex >= 0)
            {
                dataGridViewResults.Rows[bestRowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
            }
        }

        private void LabelAboutLink_Click(object? sender, EventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://github.com/zqhong/RyzenTuner",
                    UseShellExecute = true,
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning("UI", $"打开 GitHub 链接失败: {ex.Message}");
            }
        }

        // ================================================================
        // 原有功能
        // ================================================================

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            RecalcCardColumns();
            if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "-hide")
            {
                Hide();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                // 图标在设计中已设置为始终可见 (notifyIcon1.Visible = true)
                return;
            }

            // 真正退出时注销全局快捷键
            UnregisterAllHotkeys();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
        }

        private void ExitAppToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private DateTime _lastResizeTime = DateTime.MinValue;

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                return;
            }

            // 防抖：拖拽窗口时 Resize 高频触发，限制重算频率
            var now = DateTime.UtcNow;
            if ((now - _lastResizeTime).TotalMilliseconds < 50)
                return;
            _lastResizeTime = now;

            RecalcCardColumns();
            try
            {
                RecalcLogLayout();
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning("MainForm", $"日志布局计算失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新计算日志查看页布局
        /// </summary>
        private void RecalcLogLayout()
        {
            if (pageLog == null || !pageLog.Visible || dataGridViewLogs == null)
                return;

            // 触发 DisplayedCells 列根据内容自适应宽度
            dataGridViewLogs.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);

            // 强制 Fill 列（details）重算以填满剩余宽度
            // 注：PerformLayout() 不会触发 Fill 列重算，需要切换 AutoSizeColumnsMode 来强制重算
            dataGridViewLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dataGridViewLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void RecalcCardColumns()
        {
            if (pageHome == null || !pageHome.Visible) return;
            const int gap = 12;
            int pad;
            pad = groupBoxMode.Padding.Left;
            int modeW = groupBoxMode.ClientSize.Width;
            if (modeW > 100) {
                int colW = (modeW - pad * 2 - gap * 2) / 3;
                if (colW < 50) return;
                radioButton3.Left = pad; radioButton4.Left = pad + colW + gap; radioButton5.Left = pad + 2 * (colW + gap);
            }
            pad = groupBoxStatus.Padding.Left;
            int statW = groupBoxStatus.ClientSize.Width;
            if (statW > 100) {
                int colW = (statW - pad * 2 - gap * 2) / 3;
                if (colW < 50) return;
                int c1 = pad, c2 = pad + colW + gap, c3 = pad + 2 * (colW + gap);
                labelCpuFreqTitle.Left = c1; currentFreqLabel.Left = c1;
                labelCpuPowerTitle.Left = c2; currentPowerLabel.Left = c2;
                labelCpuTempTitle.Left = c3; currentTempLabel.Left = c3;
            }
            pad = groupBoxParams.Padding.Left;
            int paramW = groupBoxParams.ClientSize.Width;
            if (paramW > 100) {
                int colW = (paramW - pad * 2 - gap * 2) / 3;
                if (colW < 50) return;
                int c1 = pad, c2 = pad + colW + gap, c3 = pad + 2 * (colW + gap);
                labelFastLimitTitle.Left = c1; fastLimitLabel.Left = c1;
                labelSlowLimitTitle.Left = c2; slowLimitLabel.Left = c2;
                labelTctlLimitTitle.Left = c3; tctlTempLabel.Left = c3;
                labelStampLimitTitle.Left = c1; stampLimitLabel.Left = c1;
                labelApuSkinTitle.Left = c2; apuSkinTempLabel.Left = c2;
            }
            pad = groupBoxOptions.Padding.Left;
            int optW = groupBoxOptions.ClientSize.Width;
            if (optW > 100) {
                int colW = (optW - pad * 2 - gap) / 2;
                if (colW < 50) return;
                checkBoxEnergyStar.Left = pad;
                keepAwakeCheckBox.Left = pad + colW + gap;
                launchAtLogonCheckBox.Left = pad;
                cpuBoostCheckBox.Left = pad + colW + gap;
            }
        }
        private void mainFormTimer_Tick(object sender, EventArgs e)
        {
            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            _tickCount++;

            // 根据用户配置的间隔控制 DoPowerLimit 的更新频率（默认 4 秒）
            var interval = AppSettings.Get("PowerLimitUpdateInterval", 4);
            if (interval < 1) interval = 1; // 防御：确保最小值为 1 秒
            if (DateTime.UtcNow - _lastPowerLimitRunTime >= TimeSpan.FromSeconds(interval))
            {
                _lastPowerLimitRunTime = DateTime.UtcNow;
                DoPowerLimit();
            }

            DoProcessManage();
            UpdateMonitoringInfo();

            // 每天自动清理过期日志（后台自动处理）
            if (DateTime.UtcNow - _lastLogCleanupTime >= TimeSpan.FromHours(24))
            {
                _lastLogCleanupTime = DateTime.UtcNow;
                try
                {
                    AppContainer.Logger().Cleanup(AppSettings.Get("LogRetentionDays", 3));
                }
                catch
                {
                    // 静默忽略后台清理失败
                }
            }
        }

        private void ChangeEnergyMode(object? sender, EventArgs e)
        {
            // 防止 SwitchToMode（快捷键触发）→ SyncEnergyModeSelection → CheckedChanged 循环
            if (_isChangingMode)
                return;

            if (sender is not RadioButton { Checked: true, Tag: { } tag })
                return;

            var checkedMode = tag.ToString();

            AppSettings.Set("CurrentMode", checkedMode);
            SyncEnergyModeSelection();

            DoPowerLimit();
        }

        private void checkBoxEnergyStar_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInitializingOptions)
            {
                return;
            }

            AppSettings.Set("EnergyStar", checkBoxEnergyStar.Checked);

            if (checkBoxEnergyStar.Checked)
            {
                _needRunBoostAllBgProcesses = false;
            }
            else
            {
                _needRunBoostAllBgProcesses = true;
            }
        }

        private void keepAwakeCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (_isInitializingOptions)
            {
                return;
            }

            AppSettings.Set("KeepAwake", keepAwakeCheckBox.Checked);

            if (AppSettings.GetBool("KeepAwake"))
            {
                Awake.KeepingSysAwake(true);
            }
            else
            {
                Awake.AllowSysSleep();
            }
        }

        private void launchAtLogonCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInitializingOptions)
            {
                return;
            }

            var isEnabled = launchAtLogonCheckBox.Checked;

            try
            {
                StartupTaskScheduler.SetEnabled(isEnabled);
                AppSettings.Set("LaunchAtLogon", isEnabled);
            }
            catch (Exception ex)
            {
                _isInitializingOptions = true;
                launchAtLogonCheckBox.Checked = !isEnabled;
                _isInitializingOptions = false;

                AppSettings.Set("LaunchAtLogon", !isEnabled);
                MessageBox.Show($"{Strings.TextFailedToUpdateLaunchAtLogon}\n\n{ex.Message}",
                    Strings.TextExceptionTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void cpuBoostCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInitializingOptions)
            {
                return;
            }

            AppSettings.Set("CpuBoostEnabled", cpuBoostCheckBox.Checked);

            DoPowerLimit();
        }

        private void RefreshModeLabels()
        {
            radioButton3.Text = RyzenTunerUtils.GetModeDetailText("PowerSaveMode");
            radioButton4.Text = RyzenTunerUtils.GetModeDetailText("BalancedMode");
            radioButton5.Text = RyzenTunerUtils.GetModeDetailText("PerformanceMode");
        }

        private void SyncEnergyModeSelection()
        {
            foreach (Control c in groupBoxMode.Controls)
            {
                if (c.Tag != null && c.Tag.ToString() == AppSettings.Get("CurrentMode", "BalancedMode"))
                {
                    var rb = (RadioButton)c;
                    rb.Checked = true;
                }
            }
        }

        private void SyncLaunchAtLogonSetting()
        {
            try
            {
                var isEnabled = StartupTaskScheduler.IsEnabled();

                if (AppSettings.GetBool("LaunchAtLogon") != isEnabled)
                {
                    AppSettings.Set("LaunchAtLogon", isEnabled);
                }
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning("System", $"Failed to query launch at logon status: {ex.Message}");
            }
        }

        private void SyncCpuBoostSetting()
        {
            try
            {
                var isEnabled = AppContainer.PowerConfig().IsCpuBoostEnabled();
                _lastCpuBoostEnabled = isEnabled;

                if (AppSettings.GetBool("CpuBoostEnabled") != isEnabled)
                {
                    AppSettings.Set("CpuBoostEnabled", isEnabled);
                }
            }
            catch (Exception ex)
            {
                // 不要用 AppSettings 的值覆盖 _lastCpuBoostEnabled — 保留 null 或上次成功值，
                // 避免 DoPowerLimit 在状态未知时反复切换 CPU boost
                AppContainer.Logger().Warning("System", $"Failed to query cpu boost status: {ex.Message}");
            }
        }


        /// <summary>
        /// 更新监控信息标签
        /// </summary>
        private void UpdateMonitoringInfo()
        {
            try
            {
                var hw = AppContainer.HardwareMonitor();
                // 跑分进行中时由 BenchmarkEngine 后台线程负责采集，避免竞争
                if (!_isBenchmarkRunning)
                {
                    hw.Monitor();
                }
                var proc = AppContainer.AmdProcessor();

                // 刷新 SMU 表后再读取（跑分进行中时由 BenchmarkEngine 后台线程负责写入，跳过以避免竞争）
                if (!_isBenchmarkRunning)
                {
                    proc.RefreshTable();
                }

                // ===== 当前状态（首页） =====
                currentFreqLabel.Text = string.Format(Strings.TextMonitorFreqFormat, hw.CpuFreq);
                currentPowerLabel.Text = string.Format(Strings.TextMonitorPowerFormat, hw.CpuPackagePower);
                currentTempLabel.Text = string.Format(Strings.TextMonitorTempFormat, hw.CpuTemperature);

                // ===== 生效参数（首页） =====
                var fastLimit = proc.GetFastLimit();
                fastLimitLabel.Text = float.IsNaN(fastLimit)
                    ? Strings.TextNotAvailable
                    : string.Format(Strings.TextMonitorPowerFormat, fastLimit);

                var slowLimit = proc.GetSlowLimit();
                slowLimitLabel.Text = float.IsNaN(slowLimit)
                    ? Strings.TextNotAvailable
                    : string.Format(Strings.TextMonitorPowerFormat, slowLimit);

                var tctlTemp = proc.GetTctlTempLimit();
                tctlTempLabel.Text = float.IsNaN(tctlTemp)
                    ? Strings.TextNotAvailable
                    : string.Format(Strings.TextMonitorTempIntFormat, tctlTemp);

                var stampLimit = proc.GetStampLimit();
                stampLimitLabel.Text = float.IsNaN(stampLimit)
                    ? Strings.TextNotAvailable
                    : string.Format(Strings.TextMonitorPowerFormat, stampLimit);

                try
                {
                    var apuSkinLimit = proc.GetApuSkinTempLimit();
                    var apuSkinValue = proc.GetApuSkinTempValue();
                    if (float.IsNaN(apuSkinLimit) && float.IsNaN(apuSkinValue))
                    {
                        apuSkinTempLabel.Text = Strings.TextNotAvailable;
                    }
                    else if (float.IsNaN(apuSkinValue))
                    {
                        apuSkinTempLabel.Text = string.Format(Strings.TextMonitorTempIntFormat, apuSkinLimit);
                    }
                    else
                    {
                        apuSkinTempLabel.Text = string.Format(Strings.TextMonitorApuSkinDetailFormat, apuSkinLimit, apuSkinValue);
                    }
                }
                catch (Exception apuEx)
                {
                    apuSkinTempLabel.Text = Strings.TextNotAvailable;
                    AppContainer.Logger().Warning("HardwareMonitor", $"读取 APU SkinTemp 失败: {apuEx.Message}");
                }
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning("HardwareMonitor", $"更新监控信息失败: {ex.Message}");
            }
        }

        // ================================================================
        // 功耗限制管理（与之前一致）
        // ================================================================

        private void DoPowerLimit()
        {
            if (_isApplyingPowerLimit || _isBenchmarkRunning)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (_lastPowerLimitErrorTime > _lastSuccessfulApplyTime &&
                now - _lastPowerLimitErrorTime < TimeSpan.FromSeconds(15))
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            _isApplyingPowerLimit = true;

            if (!_isErrorRecoveryPending)
            {
                _preErrorMode = AppSettings.Get("CurrentMode", "BalancedMode");
            }

            try
            {
                var processor = AppContainer.AmdProcessor();

                var stampLimit = RyzenAdjUtils.GetPowerLimit();
                var tctlTemp = RyzenAdjUtils.GetTctlTemp();
                var apuSkinTemp = RyzenAdjUtils.GetApuSkinTemp();
                var shouldEnableCpuBoost = AppSettings.GetBool("CpuBoostEnabled");

                notifyIcon1.Text = RyzenTunerUtils.GetLocalizedModeName(AppSettings.Get("CurrentMode", "BalancedMode"));

                var applyErrors = new List<string>();

                if (!processor.SetFastPpt(stampLimit))
                {
                    applyErrors.Add($"SetFastPpt({stampLimit:0.##}W)");
                }

                if (!processor.SetSlowPpt(stampLimit))
                {
                    applyErrors.Add($"SetSlowPpt({stampLimit:0.##}W)");
                }

                if (!processor.SetStampPpt(stampLimit))
                {
                    applyErrors.Add($"SetStampPpt({stampLimit:0.##}W)");
                }

                if (!processor.SetTctlTemp(tctlTemp))
                {
                    applyErrors.Add($"SetTctlTemp({tctlTemp}C)");
                }

                if (!processor.SetApuSkinTemp(apuSkinTemp))
                {
                    applyErrors.Add($"SetApuSkinTemp({apuSkinTemp}C)");
                }

                if (_lastCpuBoostEnabled.HasValue && _lastCpuBoostEnabled.Value != shouldEnableCpuBoost)
                {
                    var boostApplied =
                        shouldEnableCpuBoost
                            ? AppContainer.PowerConfig().EnableCpuBoost()
                            : AppContainer.PowerConfig().DisableCpuBoost();

                    if (!boostApplied)
                    {
                        applyErrors.Add(shouldEnableCpuBoost ? "EnableCpuBoost()" : "DisableCpuBoost()");
                    }
                    else
                    {
                        _lastCpuBoostEnabled = shouldEnableCpuBoost;
                    }
                }

                if (applyErrors.Count > 0)
                {
                    ReportPowerLimitApplyError(string.Join(", ", applyErrors));
                }
                else
                {
                    _lastPowerLimitApplyError = string.Empty;
                }

                if (applyErrors.Count == 0)
                {
                    _lastSuccessfulApplyTime = DateTime.UtcNow;

                    // 记录成功应用耗时
                    AppContainer.Logger().Debug("Call ryzenadj",
                        $"设置功率限制完成: {stampLimit:0.##} W, Tctl {tctlTemp}°C, APU Skin {apuSkinTemp}°C",
                        sw.ElapsedMilliseconds);

                    if (_isErrorRecoveryPending)
                    {
                        _isErrorRecoveryPending = false;
                        // 若恢复期间用户手动切换了模式，尊重用户选择，不覆盖
                        if (AppSettings.Get("CurrentMode", "") == "PerformanceMode")
                        {
                            // catch 块设了 PerformanceMode，用户未干预 — 尝试还原原始模式
                            SetCurrentMode(_preErrorMode);
                        }
                        // 否则用户已手动切换，保持用户的选择
                    }
                }
            }
            catch (Exception e)
            {
                _lastPowerLimitErrorTime = DateTime.UtcNow;
                sw.Stop();

                if (now - _lastPowerLimitErrorShownAt > TimeSpan.FromSeconds(30))
                {
                    _lastPowerLimitErrorShownAt = now;
                    AppContainer.Logger().Error("Call ryzenadj", e.ToString(), sw.ElapsedMilliseconds);
                    MessageBox.Show(e.Message,
                        Strings.TextExceptionTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                else
                {
                    AppContainer.Logger().Warning("Call ryzenadj", e.ToString(), sw.ElapsedMilliseconds);
                }

                _isErrorRecoveryPending = true;
                radioButton5.Checked = true;
            }
            finally
            {
                _isApplyingPowerLimit = false;
                sw.Stop();
            }
        }

        /// <summary>
        /// 应用功率限制。
        /// SMU 寄存器值会被系统/BIOS 覆盖，因此每个周期都重新设置，不做"值未变则跳过"优化。
        /// </summary>

        private void ReportPowerLimitApplyError(string errorText)
        {
            var message = Strings.TextRyzenAdjApplyError.Replace("{errors}", errorText);
            AppContainer.Logger().Warning("Call ryzenadj", message);

            if (_lastPowerLimitApplyError == message)
            {
                return;
            }

            _lastPowerLimitApplyError = message;
            notifyIcon1.BalloonTipTitle = Strings.TextAppName;
            notifyIcon1.BalloonTipText = message;
            notifyIcon1.ShowBalloonTip(3000);
        }

        private void DoProcessManage()
        {
            if (AppSettings.GetBool("EnergyStar"))
            {
                AppContainer.EnergyManager().HandleForeground();

                if (
                    _tickCount == 15 ||
                    _tickCount % 150 == 0
                )
                {
                    AppContainer.EnergyManager().ThrottleAllUserBackgroundProcesses();
                }

                return;
            }

            if (_needRunBoostAllBgProcesses)
            {
                // 立即提升，不等待 _tickCount 条件（用户刚关闭 EnergyStar，期望立即恢复）
                AppContainer.EnergyManager().BoostAllUserBackgroundProcesses();
                _needRunBoostAllBgProcesses = false;
            }
        }

        private void SetCurrentMode(string mode)
        {
            if (mode == AppSettings.Get("CurrentMode", "BalancedMode"))
            {
                return;
            }

            AppSettings.Set("CurrentMode", mode);
            SyncEnergyModeSelection();
        }

        // ================================================================
        // 全局快捷键处理
        // ================================================================

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // 窗体句柄重建后（DPI 变化、主题切换等）重新注册快捷键
            if (!DesignMode)
            {
                RegisterAllHotkeys();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                var id = (int)m.WParam;
                switch (id)
                {
                    case HOTKEY_ID_POWERSAVE:
                        SwitchToMode(MODE_POWER_SAVE);
                        break;
                    case HOTKEY_ID_BALANCED:
                        SwitchToMode(MODE_BALANCED);
                        break;
                    case HOTKEY_ID_PERFORMANCE:
                        SwitchToMode(MODE_PERFORMANCE);
                        break;
                }
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// 切换到指定模式（被快捷键触发）
        /// </summary>
        private void SwitchToMode(string mode)
        {
            // 跑分进行中或已有模式切换进行中时禁止重入
            if (_isBenchmarkRunning || _isChangingMode)
                return;

            if (mode == AppSettings.Get("CurrentMode", "BalancedMode"))
                return;

            _isChangingMode = true;
            try
            {
                AppSettings.Set("CurrentMode", mode);
                SyncEnergyModeSelection();
                DoPowerLimit();
            }
            finally
            {
                _isChangingMode = false;
            }
        }

        /// <summary>
        /// 注册所有已配置的快捷键
        /// </summary>
        private void RegisterAllHotkeys()
        {
            UnregisterAllHotkeys();

            var failedHotkeys = new List<string>();
            var hkPowerSave = AppSettings.Get("HotkeyPowerSaveMode", "");
            var hkBalanced = AppSettings.Get("HotkeyBalancedMode", "");
            var hkPerformance = AppSettings.Get("HotkeyPerformanceMode", "");

            if (!TryRegisterHotkey(hkPowerSave, HOTKEY_ID_POWERSAVE))
                failedHotkeys.Add(GetHotkeyDisplayText(hkPowerSave));
            if (!TryRegisterHotkey(hkBalanced, HOTKEY_ID_BALANCED))
                failedHotkeys.Add(GetHotkeyDisplayText(hkBalanced));
            if (!TryRegisterHotkey(hkPerformance, HOTKEY_ID_PERFORMANCE))
                failedHotkeys.Add(GetHotkeyDisplayText(hkPerformance));

            if (failedHotkeys.Count > 0)
            {
                AppContainer.Logger().Warning("HotkeyReg",
                    $"部分快捷键注册失败: {string.Join(", ", failedHotkeys)}");
                notifyIcon1.BalloonTipTitle = Strings.TextHotkeyConflictTitle;
                notifyIcon1.BalloonTipText = Strings.TextHotkeyConflict
                    .Replace("{hotkey}", string.Join(", ", failedHotkeys));
                notifyIcon1.ShowBalloonTip(3000);
            }
        }

        /// <summary>
        /// 尝试注册单个快捷键到当前窗口
        /// </summary>
        private bool TryRegisterHotkey(string hotkeyStr, int id)
        {
            if (string.IsNullOrEmpty(hotkeyStr))
                return true;

            if (!TryParseHotkeyString(hotkeyStr, out var modifiers, out var key))
                return false;

            // MOD_NOREPEAT 防止按键不放时重复触发
            var result = RegisterHotKey(Handle, id, modifiers | MOD_NOREPEAT, (uint)key);

            if (!result)
            {
                var errorCode = Marshal.GetLastWin32Error();
                AppContainer.Logger().Warning("HotkeyReg",
                    $"注册快捷键失败 (id={id}, hotkey=\"{hotkeyStr}\", errorCode={errorCode})");
            }

            return result;
        }

        /// <summary>
        /// 注销所有快捷键
        /// </summary>
        private void UnregisterAllHotkeys()
        {
            UnregisterHotKey(Handle, HOTKEY_ID_POWERSAVE);
            UnregisterHotKey(Handle, HOTKEY_ID_BALANCED);
            UnregisterHotKey(Handle, HOTKEY_ID_PERFORMANCE);
        }

        // ================================================================
        // 快捷键字符串辅助方法
        // ================================================================

        /// <summary>
        /// 将修饰键和虚拟键码转换为快捷键字符串（如 "Ctrl+Alt+P"）
        /// </summary>
        private static string FormatHotkeyString(uint modifiers, Keys key)
        {
            var parts = new List<string>();

            if ((modifiers & MOD_CONTROL) != 0)
                parts.Add("Ctrl");
            if ((modifiers & MOD_ALT) != 0)
                parts.Add("Alt");
            if ((modifiers & MOD_SHIFT) != 0)
                parts.Add("Shift");
            if ((modifiers & MOD_WIN) != 0)
                parts.Add("Win");

            // 获取键名（移除修饰键前缀，转换不友好的枚举名为可读文本）
            var keyName = key switch
            {
                Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => "",
                Keys.Menu or Keys.LMenu or Keys.RMenu => "",
                Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => "",
                Keys.LWin or Keys.RWin => "",
                Keys.D0 => "0",
                Keys.D1 => "1",
                Keys.D2 => "2",
                Keys.D3 => "3",
                Keys.D4 => "4",
                Keys.D5 => "5",
                Keys.D6 => "6",
                Keys.D7 => "7",
                Keys.D8 => "8",
                Keys.D9 => "9",
                Keys.Oemtilde => "~",
                Keys.Oemcomma => ",",
                Keys.OemPeriod => ".",
                Keys.OemMinus => "-",
                Keys.Oemplus => "Oemplus",
                Keys.OemQuestion => "/",
                Keys.OemOpenBrackets => "[",
                Keys.OemCloseBrackets => "]",
                Keys.OemQuotes => "'",
                Keys.OemPipe => "\\",
                Keys.OemSemicolon => ";",
                Keys.Capital => "CapsLock",
                Keys.Next => "PageDown",
                Keys.Prior => "PageUp",
                Keys.Return => "Enter",
                Keys.Scroll => "ScrollLock",
                Keys.Snapshot => "PrintScreen",
                _ => key.ToString(),
            };

            if (!string.IsNullOrEmpty(keyName))
                parts.Add(keyName);

            return parts.Count > 0 ? string.Join("+", parts) : "";
        }

        /// <summary>
        /// 解析快捷键字符串为修饰键掩码和虚拟键码
        /// </summary>
        private static bool TryParseHotkeyString(string str, out uint modifiers, out Keys key)
        {
            modifiers = 0;
            key = Keys.None;

            if (string.IsNullOrWhiteSpace(str))
                return false;

            var parts = str.Split('+');
            if (parts.Length < 2)
                return false;

            // 最后一个部分是键
            if (!Enum.TryParse(parts[parts.Length - 1], true, out key) || key == Keys.None)
                return false;

            // 前面的部分是修饰键
            for (var i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[i].ToLowerInvariant())
                {
                    case "ctrl":
                    case "control":
                        modifiers |= MOD_CONTROL;
                        break;
                    case "alt":
                        modifiers |= MOD_ALT;
                        break;
                    case "shift":
                        modifiers |= MOD_SHIFT;
                        break;
                    case "win":
                        modifiers |= MOD_WIN;
                        break;
                    default:
                        return false;
                }
            }

            // 必须至少有一个修饰键
            return modifiers != 0;
        }

        /// <summary>
        /// 获取快捷键的人可读显示文本（空键返回 "未设置" / "None"）
        /// </summary>
        private static string GetHotkeyDisplayText(string hotkeyStr)
        {
            return string.IsNullOrEmpty(hotkeyStr)
                ? Strings.TextHotkeyNone
                : hotkeyStr;
        }

        // ================================================================
        // 快捷键拾取器事件处理
        // ================================================================

        private void TextBoxHotkey_Enter(object? sender, EventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Text = Strings.TextHotkeyPressKeys;
            }
        }

        private void TextBoxHotkey_Leave(object? sender, EventArgs e)
        {
            if (sender is TextBox tb)
            {
                // 恢复为已保存的值
                var hotkeyStr = tb.Tag?.ToString() ?? "";
                tb.Text = GetHotkeyDisplayText(hotkeyStr);
            }
        }

        private void TextBoxHotkey_KeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            // 清除快捷键
            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Escape)
            {
                tb.Tag = "";
                tb.Text = Strings.TextHotkeyPressKeys;
                e.SuppressKeyPress = true;
                return;
            }

            // 只处理组合键（修饰键 + 功能/字母键）
            if (IsModifierKey(e.KeyCode))
            {
                e.SuppressKeyPress = true;
                return;
            }

            // 必须有至少一个修饰键
            var modifiers = e.Modifiers;
            if (modifiers == Keys.None && !IsWinKeyPressed())
            {
                e.SuppressKeyPress = true;
                return;
            }

            // 转换为内部修饰键表示
            uint modMask = 0;
            if ((modifiers & Keys.Control) != 0)
                modMask |= MOD_CONTROL;
            if ((modifiers & Keys.Alt) != 0)
                modMask |= MOD_ALT;
            if ((modifiers & Keys.Shift) != 0)
                modMask |= MOD_SHIFT;
            // Win 键无法通过 KeyEventArgs.Modifiers 检测，使用 GetAsyncKeyState 补偿
            if (IsWinKeyPressed())
                modMask |= MOD_WIN;

            var hotkeyStr = FormatHotkeyString(modMask, e.KeyCode);
            if (string.IsNullOrEmpty(hotkeyStr))
            {
                e.SuppressKeyPress = true;
                return;
            }

            tb.Tag = hotkeyStr;
            tb.Text = hotkeyStr;
            e.SuppressKeyPress = true;
        }

        // ================================================================
        // 快捷键设置加载/保存
        // ================================================================

        private void LoadHotkeySettings()
        {
            SetHotkeyTextBox(textBoxHotkeyPowerSave, AppSettings.Get("HotkeyPowerSaveMode", ""));
            SetHotkeyTextBox(textBoxHotkeyBalanced, AppSettings.Get("HotkeyBalancedMode", ""));
            SetHotkeyTextBox(textBoxHotkeyPerformance, AppSettings.Get("HotkeyPerformanceMode", ""));
        }

        private static void SetHotkeyTextBox(TextBox tb, string hotkeyStr)
        {
            tb.Tag = hotkeyStr;
            tb.Text = GetHotkeyDisplayText(hotkeyStr);
        }

        private static string GetHotkeyFromTextBox(TextBox tb)
        {
            return tb.Tag?.ToString() ?? "";
        }

        // ================================================================
        // 日志功能
        // ================================================================

        /// <summary>
        /// 加载日志查看器数据
        /// </summary>
        private void LoadLogViewerData()
        {
            try
            {
                var levelFilter = comboBoxLogLevelFilter.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(levelFilter) ||
                    levelFilter == Strings.TextLogLevelAll)
                {
                    levelFilter = null;
                }

                var searchText = textBoxLogSearch.Text;
                if (string.IsNullOrEmpty(searchText) ||
                    searchText == Strings.TextLogSearchPlaceholder)
                {
                    searchText = null;
                }

                var table = AppContainer.Logger().QueryLogs(levelFilter, searchText);

                // 设置 DataGridView 数据源
                dataGridViewLogs.DataSource = table;

                // 强制刷新列布局以适配当前窗体尺寸
                dataGridViewLogs.PerformLayout();

                // 配置列样式
                if (dataGridViewLogs.Columns.Count >= 5)
                {
                    if (dataGridViewLogs.Columns["timestamp"] != null)
                    {
                        dataGridViewLogs.Columns["timestamp"].HeaderText =
                            Strings.TextLogColumnTime;
                        dataGridViewLogs.Columns["timestamp"].ReadOnly = true;
                        dataGridViewLogs.Columns["timestamp"].AutoSizeMode =
                            DataGridViewAutoSizeColumnMode.DisplayedCells;
                        dataGridViewLogs.Columns["timestamp"].MinimumWidth = 140;
                    }

                    if (dataGridViewLogs.Columns["level"] != null)
                    {
                        dataGridViewLogs.Columns["level"].HeaderText =
                            Strings.TextLogColumnLevel;
                        dataGridViewLogs.Columns["level"].ReadOnly = true;
                        dataGridViewLogs.Columns["level"].AutoSizeMode =
                            DataGridViewAutoSizeColumnMode.DisplayedCells;
                        dataGridViewLogs.Columns["level"].MinimumWidth = 60;
                    }

                    if (dataGridViewLogs.Columns["action"] != null)
                    {
                        dataGridViewLogs.Columns["action"].HeaderText =
                            Strings.TextLogColumnAction;
                        dataGridViewLogs.Columns["action"].ReadOnly = true;
                        dataGridViewLogs.Columns["action"].AutoSizeMode =
                            DataGridViewAutoSizeColumnMode.None;
                    }

                    if (dataGridViewLogs.Columns["details"] != null)
                    {
                        dataGridViewLogs.Columns["details"].HeaderText =
                            Strings.TextLogColumnDetails;
                        dataGridViewLogs.Columns["details"].AutoSizeMode =
                            DataGridViewAutoSizeColumnMode.Fill;
                        dataGridViewLogs.Columns["details"].ReadOnly = true;
                    }

                    if (dataGridViewLogs.Columns["elapsed_ms"] != null)
                    {
                        dataGridViewLogs.Columns["elapsed_ms"].HeaderText =
                            Strings.TextLogColumnElapsed;
                        dataGridViewLogs.Columns["elapsed_ms"].ReadOnly = true;
                        dataGridViewLogs.Columns["elapsed_ms"].AutoSizeMode =
                            DataGridViewAutoSizeColumnMode.DisplayedCells;
                        dataGridViewLogs.Columns["elapsed_ms"].MinimumWidth = 60;
                    }
                }

            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning("LogViewer", $"加载日志数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索栏获得焦点时，若为占位符文字则清空
        /// </summary>
        private void TextBoxLogSearch_Enter(object? sender, EventArgs e)
        {
            if (textBoxLogSearch.Text == Strings.TextLogSearchPlaceholder)
            {
                textBoxLogSearch.Text = "";
            }
        }

        /// <summary>
        /// 搜索栏失去焦点时，若为空则恢复占位符文字
        /// </summary>
        private void TextBoxLogSearch_Leave(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxLogSearch.Text))
            {
                textBoxLogSearch.Text = Strings.TextLogSearchPlaceholder;
            }
        }

        /// <summary>
        /// 搜索栏按下回车键时触发搜索
        /// </summary>
        private void TextBoxLogSearch_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                e.Handled = true;
                LoadLogViewerData();
            }
        }

        /// <summary>
        /// 搜索按钮点击时触发搜索
        /// </summary>
        private void ButtonLogSearch_Click(object? sender, EventArgs e)
        {
            LoadLogViewerData();
        }

        /// <summary>
        /// 打开日志查看页（从设置页跳转）
        /// </summary>
        private void ButtonOpenLogViewer_Click(object? sender, EventArgs e)
        {
            SwitchPage("log");
        }

        /// <summary>
        /// 日志级别筛选变更
        /// </summary>
        private void ComboBoxLogLevelFilter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadLogViewerData();
        }

        /// <summary>
        /// 刷新日志
        /// </summary>
        private void ButtonRefreshLogs_Click(object? sender, EventArgs e)
        {
            LoadLogViewerData();
        }

        /// <summary>
        /// 清空所有日志
        /// </summary>
        private void ButtonClearLogs_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                Strings.TextLogClearConfirm,
                Strings.TextLogClear,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                AppContainer.Logger().DeleteAll();
                LoadLogViewerData();
            }
            catch (Exception ex)
            {
                var errorMsg = string.Format(Strings.TextLogClearFailed, ex.Message);
                AppContainer.Logger().Warning("LogCleanup", errorMsg);
                MessageBox.Show(
                    errorMsg,
                    Strings.TextExceptionTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}


