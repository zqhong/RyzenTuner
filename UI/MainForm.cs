using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RyzenTuner.Common;
using RyzenTuner.Common.Benchmark;
using RyzenTuner.Common.Container;
using RyzenTuner.Properties;
using RyzenTuner.Utils;

namespace RyzenTuner.UI
{
    public partial class MainForm : Form
    {
        private Int64 _tickCount;
        private string _lastPowerLimitApplyError = string.Empty;
        private float? _lastAppliedFastPpt;
        private float? _lastAppliedSlowPpt;
        private float? _lastAppliedStampLimit;
        private int? _lastAppliedTctlTemp;
        private int? _lastAppliedApuSkinTemp;
        private bool? _lastCpuBoostEnabled;
        private DateTime _lastPowerLimitErrorShownAt = DateTime.MinValue;
        private DateTime _lastPowerLimitErrorTime = DateTime.MinValue;
        private DateTime _lastSuccessfulApplyTime = DateTime.MinValue;
        private string _preErrorMode = "BalancedMode";
        private bool _isErrorRecoveryPending;
        private bool _isApplyingPowerLimit;
        private bool _isInitializingOptions;
        private bool _isBenchmarkRunning;
        private bool _aboutInfoLoaded;

        // --- 跑分引擎字段 ---
        private BenchmarkEngine? _engine;
        private readonly List<BenchmarkTestPoint> _allResults = new();
        private BenchmarkTestType _benchmarkTestType;
        // 是否需要运行 BoostAllUserBackgroundProcesses 任务
        private bool _needRunBoostAllBgProcesses;

#if DEBUG
        private static string GetDebugBuildSuffix()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var buildTime = System.IO.File.GetLastWriteTime(assembly.Location);
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

#if DEBUG
            Text += GetDebugBuildSuffix();
#endif
        }

        /// <summary>
        /// 返回图标（在 Designer.cs 外部实现，避免 Rider CodeDom 解析器卡死）
        /// </summary>
        private static System.Drawing.Icon getIcon()
        {
            if (_cachedIcon != null)
                return _cachedIcon;
            try
            {
                _cachedIcon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch { }

            _cachedIcon ??= SystemIcons.Application;
            return _cachedIcon;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            // 运行时启动定时器
            mainFormTimer.Enabled = true;

            _isInitializingOptions = true;
            checkBoxEnergyStar.Checked = Properties.Settings.Default.EnergyStar;
            keepAwakeCheckBox.Checked = Properties.Settings.Default.KeepAwake;
            SyncLaunchAtLogonSetting();
            SyncCpuBoostSetting();
            SyncEnergyModeSelection();

            // 初始化语言选择（在 _isInitializingOptions 保护内，避免 SelectedIndexChanged 误触发）
            InitLanguageSelection();

            _isInitializingOptions = false;

            // 设置系统唤醒状态
            keepAwakeCheckBox_CheckedChanged(null, EventArgs.Empty);

            // 初始化设置页
            SettingsLoadValues();

            // 初始化跑分页
            SetupBenchmarkDataGridView();

            // 刷新首页模式标签（Designer 中只显示模式名，运行时补上功率值）
            RefreshModeLabels();

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
                    Properties.Strings.TextBenchmarkRunning,
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // 隐藏所有页面
            pageHome.Visible = false;
            pageSettings.Visible = false;
            pageBenchmark.Visible = false;
            pageAbout.Visible = false;

            // 重置导航按钮背景
            navHome.BackColor = Color.Transparent;
            navSettings.BackColor = Color.Transparent;
            navBenchmark.BackColor = Color.Transparent;
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
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            version = System.Text.RegularExpressions.Regex.Replace(version, @"(\d+\.\d+\.\d+)\.\d+", "$1");

            var year = DateTime.Now.Year.ToString();
            var ryzenadjDate = LoadRyzenAdjDate();

            labelAboutVersion.Text = Properties.Strings.TextAboutVersion.Replace("{version}", version);
            labelAboutCopyright.Text = Properties.Strings.TextAboutCopyright.Replace("{year}", year);
            labelAboutRyzenAdj.Text = Properties.Strings.TextAboutRyzenAdj.Replace("{ryzenadj_date}", ryzenadjDate);
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

        // ================================================================
        // 设置页（内嵌 SettingsForm 逻辑）
        // ================================================================

        private void SettingsLoadValues()
        {
            TrySetNumericValue(numericUpDownPowerSaveMode, Settings.Default.PowerSaveMode);
            TrySetNumericValue(numericUpDownBalancedMode, Settings.Default.BalancedMode);
            TrySetNumericValue(numericUpDownPerformanceMode, Settings.Default.PerformanceMode);

            numericUpDownTctlTemp.Value = ClampNumeric(Settings.Default.TctlTemp, numericUpDownTctlTemp);
            numericUpDownApuSkinTemp.Value = ClampNumeric(Settings.Default.ApuSkinTemp, numericUpDownApuSkinTemp);
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
            Settings.Default.PowerSaveMode = numericUpDownPowerSaveMode.Value.ToString("F0", CultureInfo.InvariantCulture);
            Settings.Default.BalancedMode = numericUpDownBalancedMode.Value.ToString("F0", CultureInfo.InvariantCulture);
            Settings.Default.PerformanceMode = numericUpDownPerformanceMode.Value.ToString("F0", CultureInfo.InvariantCulture);

            Settings.Default.TctlTemp = (int)numericUpDownTctlTemp.Value;
            Settings.Default.ApuSkinTemp = (int)numericUpDownApuSkinTemp.Value;

            Settings.Default.Save();

            // 刷新首页模式标签
            try
            {
                RefreshModeLabels();
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning($"刷新模式标签失败: {ex.Message}");
            }

            // 立即重新应用功率限制
            DoPowerLimit();
            notifyIcon1.Text = "";
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
                var currentLang = Properties.Settings.Default.Language;
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
                AppContainer.Logger().Warning($"初始化语言选择失败: {ex.Message}");

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
            if (newLang == Properties.Settings.Default.Language)
                return;

            // Fix #7: 先询问用户是否重启，再保存
            var result = MessageBox.Show(
                Properties.Strings.TextLanguageRestartHint,
                Properties.Strings.TextSettingsTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                // 用户取消 — 恢复 combo box 到当前保存的语言
                InitLanguageSelection();
                return;
            }

            // 用户确认重启后保存语言设置
            Properties.Settings.Default.Language = newLang;
            Properties.Settings.Default.Save();

            // Fix #6: 先释放 Mutex 再启动新进程，异常时恢复
            Program.ReleaseInstanceMutex();
            try
            {
                System.Diagnostics.Process.Start(Application.ExecutablePath);
                Application.Exit();
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Error($"重启失败: {ex.Message}");

                // Process.Start 失败 — 尝试重新获取 Mutex 恢复单例保护
                if (!Program.TryReacquireInstanceMutex())
                {
                    AppContainer.Logger().Error("重新获取单例 Mutex 失败");
                }

                MessageBox.Show(
                    $"{Properties.Strings.TextLanguageRestartHint}\n\n启动新进程失败: {ex.Message}",
                    Properties.Strings.TextSettingsTitle,
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
                Properties.Strings.TextBenchmarkSetPower,
                Properties.Strings.TextBenchmarkScore,
                Properties.Strings.TextBenchmarkPowerMin,
                Properties.Strings.TextBenchmarkPowerMax,
                Properties.Strings.TextBenchmarkPowerAvg,
                Properties.Strings.TextBenchmarkPowerMid,
                Properties.Strings.TextBenchmarkTempMin,
                Properties.Strings.TextBenchmarkTempMax,
                Properties.Strings.TextBenchmarkTempAvg,
                Properties.Strings.TextBenchmarkTempMid,
                Properties.Strings.TextBenchmarkFreq,
                Properties.Strings.TextBenchmarkEfficiency,
                Properties.Strings.TextBenchmarkCapability,
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
                    Properties.Strings.TextBenchmarkRunning,
                    Properties.Strings.TextBenchmarkTitle,
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
                    Properties.Strings.TextBenchmarkErrorNoData,
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var pointCount = config.GetTestPointCount();
            var totalMinutes = pointCount * (int)numericUpDownDuration.Value;

            var confirmMsg = Properties.Strings.TextBenchmarkConfirmStart
                .Replace("{count}", pointCount.ToString())
                .Replace("{time}", totalMinutes.ToString());

            if (MessageBox.Show(confirmMsg,
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _isBenchmarkRunning = true;
            _benchmarkTestType = config.TestType;
            _allResults.Clear();
            dataGridViewResults.Rows.Clear();
            buttonExportCsv.Enabled = false;
            progressBar.Visible = true;
            progressBar.Maximum = pointCount;
            progressBar.Value = 0;

            EnableBenchmarkConfig(false);
            labelStatus.Text = Properties.Strings.TextBenchmarkRunning;

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

                    _engine.OnCompleted += (results) =>
                    {
                        if (IsDisposed) return;
                        BeginInvoke(new Action(() =>
                        {
                            if (results.Count > 0)
                            {
                                RefreshAllResults(results);
                            }

                            buttonExportCsv.Enabled = results.Count > 0;
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
                            buttonExportCsv.Enabled = _allResults.Count > 0;
                            EnableBenchmarkConfig(true);
                            progressBar.Visible = false;
                            // _isBenchmarkRunning 和 _engine 由 finally 块统一清理
                            AppContainer.Logger().Error($"能效分析错误: {error}");
                        }));
                    };

                    await _engine.RunAsync(config);
                }
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
                Properties.Strings.TextBenchmarkCancel,
                Properties.Strings.TextBenchmarkTitle,
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
                labelStatus.Text = Properties.Strings.TextBenchmarkStopped;
            }
        }

        private void BenchmarkExportCsv_Click(object? sender, EventArgs e)
        {
            if (_allResults.Count == 0)
            {
                MessageBox.Show(
                    Properties.Strings.TextBenchmarkExportNoData,
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = Properties.Strings.TextBenchmarkExportSaveFilter,
                FilterIndex = 1,
                RestoreDirectory = true,
                FileName =
                    $"{Properties.Strings.TextBenchmarkExportFileName}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                ExportResultsToCsv(sfd.FileName);
                MessageBox.Show(
                    Properties.Strings.TextBenchmarkExportSuccess.Replace("{path}", sfd.FileName),
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Error($"导出 CSV 失败: {ex}");
                MessageBox.Show(
                    $"{Properties.Strings.TextBenchmarkExportFailed}: {ex.Message}",
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ExportResultsToCsv(string filePath)
        {
            var sb = new StringBuilder();
            var testType = _benchmarkTestType == BenchmarkTestType.SingleCore
                ? Properties.Strings.TextBenchmarkSingleCore
                : Properties.Strings.TextBenchmarkMultiCore;
            sb.AppendLine($"# {Properties.Strings.TextBenchmarkTitle}");
            sb.AppendLine($"# {Properties.Strings.TextBenchmarkTestType}: {testType}");
            sb.AppendLine($"# {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            var headers = dataGridViewResults.Columns
                .Cast<DataGridViewColumn>()
                .Select(c => c.HeaderText);

            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

            foreach (var r in _allResults)
            {
                var values = new[]
                {
                    $"{r.SetTdp:F0}",
                    r.Score.ToString("D"),
                    $"{r.PowerMin:F2}",
                    $"{r.PowerMax:F2}",
                    $"{r.PowerAvg:F2}",
                    $"{r.PowerMedian:F2}",
                    $"{r.TempMin:F1}",
                    $"{r.TempMax:F1}",
                    $"{r.TempAvg:F1}",
                    $"{r.TempMedian:F1}",
                    $"{r.CpuFreqAvg:F0}",
                    $"{r.Efficiency:F0}",
                    $"{r.Capability:P0}",
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

        private void RefreshAllResults(List<BenchmarkTestPoint> results)
        {
            // results 参数与 _allResults 字段持有相同的对象引用
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
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/zqhong/RyzenTuner",
                    UseShellExecute = true,
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning($"打开 GitHub 链接失败: {ex.Message}");
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
            }
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

            DoPowerLimit();
            DoProcessManage();
            UpdateMonitoringInfo();
        }

        private void ChangeEnergyMode(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                var tag = ((RadioButton)sender).Tag;
                if (tag == null) return;
                var checkedMode = tag.ToString();

                Settings.Default.CurrentMode = checkedMode;
                Settings.Default.Save();
                SyncEnergyModeSelection();

                DoPowerLimit();
            }
        }

        private void checkBoxEnergyStar_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInitializingOptions)
            {
                return;
            }

            Properties.Settings.Default.EnergyStar = checkBoxEnergyStar.Checked;
            Properties.Settings.Default.Save();

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

            Properties.Settings.Default.KeepAwake = keepAwakeCheckBox.Checked;
            Properties.Settings.Default.Save();

            if (Properties.Settings.Default.KeepAwake)
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
                Properties.Settings.Default.LaunchAtLogon = isEnabled;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                _isInitializingOptions = true;
                launchAtLogonCheckBox.Checked = !isEnabled;
                _isInitializingOptions = false;

                Properties.Settings.Default.LaunchAtLogon = !isEnabled;
                Properties.Settings.Default.Save();
                MessageBox.Show($"{Properties.Strings.TextFailedToUpdateLaunchAtLogon}\n\n{ex.Message}",
                    Properties.Strings.TextExceptionTitle,
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

            Properties.Settings.Default.CpuBoostEnabled = cpuBoostCheckBox.Checked;
            Properties.Settings.Default.Save();

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
                if (c.Tag != null && c.Tag.ToString() == Properties.Settings.Default.CurrentMode)
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

                if (Properties.Settings.Default.LaunchAtLogon != isEnabled)
                {
                    Properties.Settings.Default.LaunchAtLogon = isEnabled;
                    Properties.Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning($"Failed to query launch at logon status: {ex.Message}");
            }
        }

        private void SyncCpuBoostSetting()
        {
            try
            {
                var isEnabled = AppContainer.PowerConfig().IsCpuBoostEnabled();
                _lastCpuBoostEnabled = isEnabled;

                if (Properties.Settings.Default.CpuBoostEnabled != isEnabled)
                {
                    Properties.Settings.Default.CpuBoostEnabled = isEnabled;
                    Properties.Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                _lastCpuBoostEnabled = Properties.Settings.Default.CpuBoostEnabled;
                AppContainer.Logger().Warning($"Failed to query cpu boost status: {ex.Message}");
            }
        }

        /// <summary>
        /// 展开/收起监控信息面板（保留兼容，新 UI 中监控信息始终可见）
        /// </summary>


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

                // 刷新 SMU 表后再读取
                proc.RefreshTable();

                // ===== 当前状态（首页） =====
                currentFreqLabel.Text = $"{hw.CpuFreq:F0} MHz";
                currentPowerLabel.Text = $"{hw.CpuPackagePower:F1} W";
                currentTempLabel.Text = $"{hw.CpuTemperature:F1} ℃";

                // ===== 生效参数（首页） =====
                var fastLimit = proc.GetFastLimit();
                fastLimitLabel.Text = float.IsNaN(fastLimit) ? "N/A" : $"{fastLimit:F1} W";

                var slowLimit = proc.GetSlowLimit();
                slowLimitLabel.Text = float.IsNaN(slowLimit) ? "N/A" : $"{slowLimit:F1} W";

                var tctlTemp = proc.GetTctlTempLimit();
                tctlTempLabel.Text = float.IsNaN(tctlTemp) ? "N/A" : $"{tctlTemp:F0} ℃";

                var stampLimit = proc.GetStampLimit();
                stampLimitLabel.Text = float.IsNaN(stampLimit) ? "N/A" : $"{stampLimit:F1} W";

                try
                {
                    var apuSkinLimit = proc.GetApuSkinTempLimit();
                    var apuSkinValue = proc.GetApuSkinTempValue();
                    if (float.IsNaN(apuSkinLimit) && float.IsNaN(apuSkinValue))
                    {
                        apuSkinTempLabel.Text = "N/A";
                    }
                    else if (float.IsNaN(apuSkinValue))
                    {
                        apuSkinTempLabel.Text = $"{apuSkinLimit:F0} ℃";
                    }
                    else
                    {
                        apuSkinTempLabel.Text = $"{apuSkinLimit:F0} ℃ ({apuSkinValue:F0} ℃)";
                    }
                }
                catch (Exception apuEx)
                {
                    apuSkinTempLabel.Text = "N/A";
                    AppContainer.Logger().Warning($"读取 APU SkinTemp 失败: {apuEx.Message}");
                }
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning($"更新监控信息失败: {ex.Message}");
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

            _isApplyingPowerLimit = true;

            if (!_isErrorRecoveryPending)
            {
                _preErrorMode = Settings.Default.CurrentMode;
            }

            try
            {
                var processor = AppContainer.AmdProcessor();

                var stampLimit = RyzenAdjUtils.GetPowerLimit();
                var tctlTemp = RyzenAdjUtils.GetTctlTemp();
                var apuSkinTemp = RyzenAdjUtils.GetApuSkinTemp();
                var shouldEnableCpuBoost = Settings.Default.CpuBoostEnabled;

                notifyIcon1.Text = "";

                var applyErrors = new List<string>();

                if (!TryApplyPowerLimit(_lastAppliedFastPpt, stampLimit, () => processor.SetFastPPT(stampLimit), out var fastPptChanged))
                {
                    applyErrors.Add($"SetFastPPT({stampLimit:0.##}W)");
                }
                else if (fastPptChanged)
                {
                    _lastAppliedFastPpt = stampLimit;
                }

                if (!TryApplyPowerLimit(_lastAppliedSlowPpt, stampLimit, () => processor.SetSlowPPT(stampLimit), out var slowPptChanged))
                {
                    applyErrors.Add($"SetSlowPPT({stampLimit:0.##}W)");
                }
                else if (slowPptChanged)
                {
                    _lastAppliedSlowPpt = stampLimit;
                }

                if (!TryApplyPowerLimit(_lastAppliedStampLimit, stampLimit, () => processor.SetStampPPT(stampLimit), out var stampPptChanged))
                {
                    applyErrors.Add($"SetStampPPT({stampLimit:0.##}W)");
                }
                else if (stampPptChanged)
                {
                    _lastAppliedStampLimit = stampLimit;
                }

                if (!TryApplyTctlTemp(tctlTemp, () => processor.SetTctlTemp((uint)tctlTemp), out var tctlTempChanged))
                {
                    applyErrors.Add($"SetTctlTemp({tctlTemp}C)");
                }
                else if (tctlTempChanged)
                {
                    _lastAppliedTctlTemp = tctlTemp;
                }

                if (!TryApplyIntSetting(_lastAppliedApuSkinTemp, apuSkinTemp, () => processor.SetApuSkinTemp((uint)apuSkinTemp), out var apuSkinTempChanged))
                {
                    applyErrors.Add($"SetApuSkinTemp({apuSkinTemp}C)");
                }
                else if (apuSkinTempChanged)
                {
                    _lastAppliedApuSkinTemp = apuSkinTemp;
                }

                if (_lastCpuBoostEnabled != shouldEnableCpuBoost)
                {
                    var boostChanged = false;
                    var boostApplied = TryApplyCpuBoost(() =>
                        shouldEnableCpuBoost
                            ? AppContainer.PowerConfig().EnableCpuBoost()
                            : AppContainer.PowerConfig().DisableCpuBoost(),
                        out boostChanged);

                    if (!boostApplied)
                    {
                        applyErrors.Add(shouldEnableCpuBoost ? "EnableCpuBoost()" : "DisableCpuBoost()");
                    }
                    else if (boostChanged)
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

                    if (_isErrorRecoveryPending)
                    {
                        _isErrorRecoveryPending = false;
                        // 若恢复期间用户手动切换了模式，尊重用户选择，不覆盖
                        if (Settings.Default.CurrentMode == "PerformanceMode")
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

                if (now - _lastPowerLimitErrorShownAt > TimeSpan.FromSeconds(30))
                {
                    _lastPowerLimitErrorShownAt = now;
                    AppContainer.Logger().Error(e.ToString());
                    MessageBox.Show(e.Message,
                        Properties.Strings.TextExceptionTitle,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                else
                {
                    AppContainer.Logger().Warning(e.ToString());
                }

                _isErrorRecoveryPending = true;
                radioButton5.Checked = true;
            }
            finally
            {
                _isApplyingPowerLimit = false;
            }
        }

        /// <summary>
        /// 应用功率限制。
        /// SMU 寄存器值会被系统/BIOS 覆盖，因此每个周期都重新设置，不做"值未变则跳过"优化。
        /// </summary>
        private static bool TryApplyPowerLimit(float? lastAppliedValue, float targetValue, Func<bool> applyAction, out bool changed)
        {
            // ReSharper disable once UnusedParameter.Local
            _ = lastAppliedValue;
            var result = applyAction();
            changed = result;
            return result;
        }

        /// <summary>
        /// 应用 Tctl 温度限制。
        /// SMU 寄存器值会被系统/BIOS 覆盖，因此每个周期都重新设置，不做"值未变则跳过"优化。
        /// </summary>
        private bool TryApplyTctlTemp(int targetValue, Func<bool> applyAction, out bool changed)
        {
            // ReSharper disable once UnusedParameter.Local
            _ = _lastAppliedTctlTemp;
            var result = applyAction();
            changed = result;
            return result;
        }

        /// <summary>
        /// 应用整数型 SMU 设置（如 ApuSkinTemp）。
        /// SMU 寄存器值会被系统/BIOS 覆盖，因此每个周期都重新设置，不做"值未变则跳过"优化。
        /// </summary>
        private static bool TryApplyIntSetting(int? lastAppliedValue, int targetValue, Func<bool> applyAction, out bool changed)
        {
            // ReSharper disable once UnusedParameter.Local
            _ = lastAppliedValue;
            var result = applyAction();
            changed = result;
            return result;
        }

        private static bool TryApplyCpuBoost(Func<bool> applyAction, out bool changed)
        {
            var result = applyAction();
            changed = result;
            return result;
        }

        private void ReportPowerLimitApplyError(string errorText)
        {
            var message = Properties.Strings.TextRyzenAdjApplyError.Replace("{errors}", errorText);
            AppContainer.Logger().Warning(message);

            if (_lastPowerLimitApplyError == message)
            {
                return;
            }

            _lastPowerLimitApplyError = message;
            notifyIcon1.BalloonTipTitle = "RyzenTuner";
            notifyIcon1.BalloonTipText = message;
            notifyIcon1.ShowBalloonTip(3000);
        }

        private void DoProcessManage()
        {
            if (Properties.Settings.Default.EnergyStar)
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
            if (mode == Settings.Default.CurrentMode)
            {
                return;
            }

            Settings.Default.CurrentMode = mode;
            Settings.Default.Save();
            SyncEnergyModeSelection();
        }
    }
}
