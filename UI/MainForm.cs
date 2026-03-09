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
        private bool? _lastCpuBoostEnabled;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            checkBoxEnergyStar.Checked = Properties.Settings.Default.EnergyStar;
            keepAwakeCheckBox.Checked = Properties.Settings.Default.KeepAwake;
            textBox1.Text = Properties.Settings.Default.CustomMode;
            UpdateCustomModeInputState(false);
            SyncEnergyModeSelection();

            // 设置系统唤醒状态
            keepAwakeCheckBox_CheckedChanged(null, EventArgs.Empty);

            WindowState = FormWindowState.Minimized;
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

        private void AboutAppToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void ExitAppToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
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
            try
            {
                var processor = AppContainer.AmdProcessor();

                var stampLimit = RyzenAdjUtils.GetPowerLimit();
                var tctlTemp = RyzenAdjUtils.GetTctlTemp();
                var fastPpt = Settings.Default.FastPPT;
                var slowPpt = Settings.Default.SlowPPT;
                var shouldEnableCpuBoost = RyzenTunerUtils.IsPerformanceMode(stampLimit);

                notifyIcon1.Text = RyzenTunerUtils.GetNoticeText(stampLimit);
                
                AppContainer.Logger().Debug($"fastPPT: {fastPpt}, slowPPT: {slowPpt}, stampPPT: {stampLimit}, tctlTemp: {tctlTemp}");
                
                // 调用 ryzenadj 调整 Cpu 设置
                // 说明：
                // 假设 fastPPT 为 51 瓦，slowPPT 为 45 瓦，stampPPT 为 30 瓦。
                // 假设没有撞到功耗墙
                // 当打开网页的时候，处理器功耗会升到 51 瓦（fastPPT 定义）
                // 过了 x 秒后（由 SLOW PPT TIME CONSTANT定义） ，处理器功耗变为 45 瓦（slowPPT 定义）
                // 再过了 x 后秒（由 STAPM TIME CONSTANT 定义），处理器功耗变为 30 瓦（stampPPT 定义）
                // 之后，一直维持在 30 瓦
                
                // 备注：
                // ryzenadj 0.17.0 以及部分旧版本测试在当前环境存在问题：
                // 1、stamp limit 设置不生效
                // 2、调整 fast limit，会同时修改 fast limit 和 stamp limit
                // 因此，设置 fastPPT 的值跟 stamp 一样
                // 这样子的话，前期不会有性能爆发，在后面会有一段性能爆发
                var applyErrors = new List<string>();

                if (!TryApplyPowerLimit(_lastAppliedFastPpt, stampLimit, () => processor.SetFastPPT(stampLimit), out var fastPptChanged))
                {
                    applyErrors.Add($"SetFastPPT({stampLimit:0.##}W)");
                }

                if (fastPptChanged)
                {
                    _lastAppliedFastPpt = stampLimit;
                }

                if (!TryApplyPowerLimit(_lastAppliedSlowPpt, slowPpt, () => processor.SetSlowPPT(slowPpt), out var slowPptChanged))
                {
                    applyErrors.Add($"SetSlowPPT({slowPpt:0.##}W)");
                }

                if (slowPptChanged)
                {
                    _lastAppliedSlowPpt = slowPpt;
                }

                if (!TryApplyPowerLimit(_lastAppliedStampLimit, stampLimit, () => processor.SetStampPPT(stampLimit), out var stampPptChanged))
                {
                    applyErrors.Add($"SetStampPPT({stampLimit:0.##}W)");
                }

                if (stampPptChanged)
                {
                    _lastAppliedStampLimit = stampLimit;
                }

                if (!TryApplyTctlTemp(tctlTemp, () => processor.SetTctlTemp((uint)tctlTemp), out var tctlTempChanged))
                {
                    applyErrors.Add($"SetTctlTemp({tctlTemp}C)");
                }

                if (tctlTempChanged)
                {
                    _lastAppliedTctlTemp = tctlTemp;
                }

                // 配置系统电源计划
                // 1、仅在【性能模式】下开启睿频
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

                    if (boostChanged)
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
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                radioButton5.Checked = true;
                ChangeEnergyMode(radioButton5, EventArgs.Empty);
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
            var message = $"Failed to apply some RyzenAdj settings: {errorText}";
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
            if (checkBoxEnergyStar.Checked)
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


            foreach (ToolStripItem tsmi in contextMenuStrip1.Items)
            {
                if (tsmi.Tag != null && tsmi.Tag.ToString() == Properties.Settings.Default.CurrentMode)
                {
                    var tsmi2 = (ToolStripMenuItem)tsmi;
                    tsmi2.Checked = true;
                }
                else if (tsmi is ToolStripMenuItem)
                {
                    var tsmi2 = (ToolStripMenuItem)tsmi;
                    tsmi2.Checked = false;
                }
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
                MessageBox.Show("Custom power limit must be a valid number greater than 0.");
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

        private void ToolStripMenuItems_Clicked(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem.CheckState != CheckState.Checked)
            {
                menuItem.Checked = true;
            }

            // 根据用户点击的托盘菜单选项，自动选中不同的【功率限制】按钮
            foreach (Control c in powerLimitGroupBox.Controls)
            {
                if (c.Tag == null || c.Tag.ToString() != ((ToolStripMenuItem)sender).Tag.ToString())
                {
                    continue;
                }

                var radioButton = (RadioButton)c;
                radioButton.Checked = true;
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "-hide")
            {
                Hide();
            }
        }
    }
}
