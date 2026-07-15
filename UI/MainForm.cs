using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using RyzenTuner.Common;
using RyzenTuner.Common.Container;
using RyzenTuner.Properties;
using RyzenTuner.Utils;

namespace RyzenTuner.UI
{
    public partial class MainForm : Form
    {
        private Int64 _tickCount;
        private readonly Color _customModeInputDefaultBackColor = SystemColors.Window;
        private readonly Color _customModeInputInvalidBackColor = Color.MistyRose;
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

        private void Form1_Load(object sender, EventArgs e)
        {
            _isInitializingOptions = true;
            checkBoxEnergyStar.Checked = Properties.Settings.Default.EnergyStar;
            keepAwakeCheckBox.Checked = Properties.Settings.Default.KeepAwake;
            SyncLaunchAtLogonSetting();
            SyncCpuBoostSetting();
            textBox1.Text = Properties.Settings.Default.CustomMode;
            UpdateCustomModeInputState(false);
            SyncEnergyModeSelection();
            _isInitializingOptions = false;

            // 设置系统唤醒状态
            keepAwakeCheckBox_CheckedChanged(null, EventArgs.Empty);
        }

        private void AboutAppToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void BenchmarkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _isBenchmarkRunning = true;
            using var benchmarkForm = new BenchmarkForm();
            benchmarkForm.FormClosed += (_, _) => _isBenchmarkRunning = false;
            benchmarkForm.ShowDialog(this);
        }

        private void ExitAppToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void checkBoxEnergyStar_CheckedChanged(object sender, EventArgs e)
        {
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (radioButton6.Checked)
                {
                    ChangeEnergyMode(radioButton6, e);
                }

                e.Cancel = true;
                Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
        }

        private void mainFormTimer_Tick(object sender, EventArgs e)
        {
            _tickCount++;
            AppContainer.HardwareMonitor().Monitor();

            DoPowerLimit();

            DoProcessManage();

            UpdateMonitoringInfo();
        }

/// <summary>
        /// 展开/收起监控信息面板
        /// </summary>
        private void monitoringToggleBtn_Click(object sender, EventArgs e)
        {
            if (monitoringGroupBox.Visible)
            {
                monitoringGroupBox.Visible = false;
                monitoringToggleBtn.Text = "▸";
                ClientSize = new Size(ClientSize.Width, monitoringToggleBtn.Bottom + 15);
            }
            else
            {
                monitoringGroupBox.Visible = true;
                monitoringToggleBtn.Text = "▾";
                ClientSize = new Size(ClientSize.Width, monitoringGroupBox.Bottom + 26);
                UpdateMonitoringInfo();
            }
        }

        /// <summary>
        /// 更新监控信息标签
        /// </summary>
        private void UpdateMonitoringInfo()
        {
            if (!monitoringGroupBox.Visible)
            {
                return;
            }
            try
            {
                var hw = AppContainer.HardwareMonitor();
                var proc = AppContainer.AmdProcessor();

                // 刷新 SMU 表后再读取，等效 ryzenadj --info 的行为
                proc.RefreshTable();

                // ===== RyzenAdj 功率限制（左列） =====

                var fastLimit = proc.GetFastLimit();
                fastLimitLabel.Text = float.IsNaN(fastLimit)
                    ? "FastPPT: N/A"
                    : $"FastPPT: {fastLimit:F1} W";

                var slowLimit = proc.GetSlowLimit();
                slowLimitLabel.Text = float.IsNaN(slowLimit)
                    ? "SlowPPT: N/A"
                    : $"SlowPPT: {slowLimit:F1} W";

                var stampLimit = proc.GetStampLimit();
                stampLimitLabel.Text = float.IsNaN(stampLimit)
                    ? "StapmPPT: N/A"
                    : $"StapmPPT: {stampLimit:F1} W";

                var tctlTemp = proc.GetTctlTempLimit();
                tctlTempLabel.Text = float.IsNaN(tctlTemp)
                    ? "TctlTemp: N/A"
                    : $"TctlTemp: {tctlTemp:F0} °C";

                try
                {
                    var apuSkinLimit = proc.GetApuSkinTempLimit();
                    var apuSkinValue = proc.GetApuSkinTempValue();
                    if (float.IsNaN(apuSkinLimit) && float.IsNaN(apuSkinValue))
                    {
                        apuSkinTempLabel.Text = "ApuSkinTemp: N/A";
                    }
                    else if (float.IsNaN(apuSkinValue))
                    {
                        apuSkinTempLabel.Text = $"ApuSkinTemp: {apuSkinLimit:F0} °C";
                    }
                    else
                    {
                        apuSkinTempLabel.Text = $"ApuSkinTemp: {apuSkinLimit:F0} °C ({apuSkinValue:F0} °C)";
                    }
                }
                catch (Exception ex)
                {
                    apuSkinTempLabel.Text = "ApuSkinTemp: N/A";
                    AppContainer.Logger().Warning($"读取 ApuSkinTemp 失败: {ex.Message}");
                }

                // ===== 系统状态（右列） =====

                currentPowerLabel.Text = $"封装功耗: {hw.CpuPackagePower:F1} W";
                currentFreqLabel.Text = $"当前频率: {hw.CpuFreq:F0} MHz";
                currentTempLabel.Text = $"当前温度: {hw.CpuTemperature:F1} °C";
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning($"更新监控信息失败: {ex.Message}");
            }
        }


        private void ChangeEnergyMode(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                var checkedMode = ((RadioButton)sender).Tag.ToString();

                if (checkedMode == "CustomMode" && !UpdateCustomModeInputState(true))
                {
                    SyncEnergyModeSelection();
                    return;
                }

                Settings.Default.CurrentMode = checkedMode;
                Settings.Default.Save();
                SyncEnergyModeSelection();

                DoPowerLimit();
            }
        }

        private void DoPowerLimit()
        {
            if (_isApplyingPowerLimit || _isBenchmarkRunning)
            {
                return;
            }

            // 错误冷却检查：上次错误后 15 秒内，跳过完整的 try-block，让定时器下次节拍再试
            // 防止持久性错误时每 2 秒无意义地执行全部 I/O 操作
            var now = DateTime.UtcNow;
            if (_lastPowerLimitErrorTime > _lastSuccessfulApplyTime &&
                now - _lastPowerLimitErrorTime < TimeSpan.FromSeconds(15))
            {
                return;
            }

            _isApplyingPowerLimit = true;

            // 仅首次进入（非恢复状态）时保存用户原始模式，防止冷却过期后被覆盖
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

                notifyIcon1.Text = RyzenTunerUtils.GetNoticeText(stampLimit);
                
                // 所有 PPT 限制均设为相同值（stampLimit），使 CPU 在所有时间窗口内维持一致功率
                var applyErrors = new List<string>();

                // 注意：仅在 applyAction 成功后更新 _lastApplied* 跟踪字段，
                // 防止单次瞬态失败导致该设置永久被跳过（见 TryApply* 中的 changed=true 在 applyAction 前设置）
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

                // 配置系统电源计划
                // 睿频改为显式开关，仅在用户手动切换后同步系统电源计划
                if (_lastCpuBoostEnabled != shouldEnableCpuBoost)
                {
                    var boostChanged = false;
                    var boostApplied = shouldEnableCpuBoost
                        ? TryApplyCpuBoost(true, () => AppContainer.PowerConfig().EnableCpuBoost(), out boostChanged)
                        : TryApplyCpuBoost(false, () => AppContainer.PowerConfig().DisableCpuBoost(), out boostChanged);

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

                    // 错误恢复后自动还原用户的原始模式
                    if (_isErrorRecoveryPending)
                    {
                        _isErrorRecoveryPending = false;
                        SetCurrentMode(_preErrorMode);
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

                // 切换到 PerformanceMode 作为安全回退
                _isErrorRecoveryPending = true;
                radioButton5.Checked = true;
                // 注意：radioButton5.Checked = true 会通过 CheckedChanged 事件触发
                // ChangeEnergyMode 保存 CurrentMode 并同步界面状态。
                // 其中的 DoPowerLimit 调用被重入防护门（_isApplyingPowerLimit）拦截，
                // 实际的硬件限制切换由下一次定时器节拍完成（最长 2048ms 后）。
                // 不在此处显式调用 ChangeEnergyMode（旧有冗余代码已移除）。
            }
            finally
            {
                _isApplyingPowerLimit = false;
            }
        }

        private static bool TryApplyPowerLimit(float? lastAppliedValue, float targetValue, Func<bool> applyAction, out bool changed)
        {
            changed = !lastAppliedValue.HasValue || Math.Abs(lastAppliedValue.Value - targetValue) >= 0.01f;
            if (!changed)
            {
                return true;
            }

            return applyAction();
        }

        private bool TryApplyTctlTemp(int targetValue, Func<bool> applyAction, out bool changed)
        {
            changed = !_lastAppliedTctlTemp.HasValue || _lastAppliedTctlTemp.Value != targetValue;
            if (!changed)
            {
                return true;
            }

            return applyAction();
        }

        private static bool TryApplyIntSetting(int? lastAppliedValue, int targetValue, Func<bool> applyAction, out bool changed)
        {
            changed = !lastAppliedValue.HasValue || lastAppliedValue.Value != targetValue;
            if (!changed)
            {
                return true;
            }

            return applyAction();
        }

        private bool TryApplyCpuBoost(bool targetValue, Func<bool> applyAction, out bool changed)
        {
            changed = !_lastCpuBoostEnabled.HasValue || _lastCpuBoostEnabled.Value != targetValue;
            if (!changed)
            {
                return true;
            }

            return applyAction();
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

        /// <summary>
        /// 执行进程管理任务（EnergyStar）
        /// </summary>
        private void DoProcessManage()
        {
            // 如果开启【EnergyStar】
            if (Properties.Settings.Default.EnergyStar)
            {
                AppContainer.EnergyManager().HandleForeground();

                // Throttle 当前用户的所有后台进程
                if (
                    // 首次运行 30 秒
                    _tickCount == 15 ||
                    // 每 5 分钟
                    _tickCount % 150 == 0
                )
                {
                    AppContainer.EnergyManager().ThrottleAllUserBackgroundProcesses();
                }

                return;
            }

            // 关闭【EnergyStar】的情况
            if (_needRunBoostAllBgProcesses)
            {
                // Boost 当前用户的所有后台进程：每 5 分钟检查一次，一般只运行一次
                if (_tickCount % 150 == 0)
                {
                    AppContainer.EnergyManager().BoostAllUserBackgroundProcesses();
                    _needRunBoostAllBgProcesses = false;
                }
            }
        }

        private void SyncEnergyModeSelection()
        {
            foreach (Control c in powerLimitGroupBox.Controls)
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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateCustomModeInputState(false);
        }

        private bool UpdateCustomModeInputState(bool showError)
        {
            if (TryGetValidatedCustomMode(textBox1.Text, out var customMode))
            {
                textBox1.BackColor = _customModeInputDefaultBackColor;

                var normalizedValue = customMode.ToString(CultureInfo.InvariantCulture);
                if (Properties.Settings.Default.CustomMode != normalizedValue)
                {
                    Properties.Settings.Default.CustomMode = normalizedValue;
                    Properties.Settings.Default.Save();
                }

                return true;
            }

            textBox1.BackColor = _customModeInputInvalidBackColor;

            if (showError)
            {
                MessageBox.Show(Properties.Strings.TextCustomPowerLimitInvalid,
                    Properties.Strings.TextExceptionTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            return false;
        }

        private static bool TryGetValidatedCustomMode(string text, out float customMode)
        {
            customMode = 0;
            var value = text.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var isValid = float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out customMode) ||
                          float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out customMode);

            return isValid && customMode > 0;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "-hide")
            {
                Hide();
            }
        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var settingsForm = new SettingsForm();
            if (settingsForm.ShowDialog(this) == DialogResult.OK)
            {
                // 刷新自定义模式输入框（可能默认值已变化）
                UpdateCustomModeInputState(false);
                // 刷新各模式标签显示（模式名-XXW）
                // 部分模式标签可能因设置值损坏而无法解析，单独捕获避免阻断后续操作
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
                // 更新通知栏文本
                notifyIcon1.Text = RyzenTunerUtils.GetNoticeText();
            }
        }

        private void RefreshModeLabels()
        {
            radioButton2.Text = RyzenTunerUtils.GetModeDetailText("SleepMode");
            radioButton3.Text = RyzenTunerUtils.GetModeDetailText("PowerSaveMode");
            radioButton4.Text = RyzenTunerUtils.GetModeDetailText("BalancedMode");
            radioButton5.Text = RyzenTunerUtils.GetModeDetailText("PerformanceMode");
        }

        /// <summary>
        /// 将当前模式设置为指定模式（不触发 DoPowerLimit）。
        /// 用于错误恢复时还原用户的原始选择。
        /// </summary>
        private void SetCurrentMode(string mode)
        {
            if (mode == Settings.Default.CurrentMode)
            {
                return;
            }

            // 直接保存模式并同步界面，避免通过 RadioButton.Checked 事件链
            // （ChangeEnergyMode 中的 DoPowerLimit 会被重入防护门拦截而无效，
            //  且直接赋值避免 CustomMode 输入验证干扰恢复流程）。
            Settings.Default.CurrentMode = mode;
            Settings.Default.Save();
            SyncEnergyModeSelection();
        }
    }
}
