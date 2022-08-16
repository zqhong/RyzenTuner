using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace RyzenTuner
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            checkBox1.Checked = Properties.Settings.Default.EnergyStar;
            keepAwakeCheckBox.Checked = Properties.Settings.Default.KeepAwake;
            textBox1.Text = Properties.Settings.Default.CustomMode;
            SyncEnergyModeSelection();
            
            // 设置系统唤醒状态
            keepAwakeCheckBox_CheckedChanged(null, EventArgs.Empty);

            WindowState = FormWindowState.Minimized;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.EnergyStar = checkBox1.Checked;
            Properties.Settings.Default.Save();
            timer1_Tick(sender, e);
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

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
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
            this.Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _hardwareMonitor.Monitor();

            // 启动/关闭 EnergyStar
            if (checkBox1.Checked && CommonUtils.IsSupportEnergyStar())
            {
                StartEnergyStar();
            }
            else
            {
                StopEnergyStar();
            }

            ApplyEnergyMode();
        }

        private static void StartEnergyStar()
        {
            if (!Process.GetProcessesByName("energystar").Any())
            {
                Process.Start(System.IO.Path.GetDirectoryName(Application.ExecutablePath) +
                              "\\energystar\\EnergyStar.exe");
            }
        }

        private static void StopEnergyStar()
        {
            foreach (var p in Process.GetProcessesByName("energystar"))
            {
                p.Kill();
            }
        }

        private void ChangeEnergyMode(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                string checkedMode = ((RadioButton)sender).Tag.ToString();
                Properties.Settings.Default.CurrentMode = checkedMode;
                Properties.Settings.Default.Save();
                SyncEnergyModeSelection();

                ApplyEnergyMode();
            }
        }

        private void ApplyEnergyMode()
        {
            try
            {
                notifyIcon1.Text = this.GetNoticeText();

                var powerLimit = GetPowerLimit();

                // 运行 ryzen adj
                var process = new Process();
                var startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = System.IO.Path.GetDirectoryName(Application.ExecutablePath) +
                               "\\ryzenadj\\ryzenadj.exe",
                    Arguments = CalcRyzenAdjArg()
                };
                process.StartInfo = startInfo;
                process.Start();

                // 配置系统电源计划
                // 1、仅在【性能模式】下开启睿频
                if (CommonUtils.IsPerformanceModeMode(powerLimit))
                {
                    CommonUtils.EnableCpuBoost();
                }
                else
                {
                    CommonUtils.DisableCpuBoost();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                radioButton5.Checked = true;
                ChangeEnergyMode(radioButton5, EventArgs.Empty);
            }
        }

        /**
         * 计算自动模式下的限制功率（单位：瓦）
         *
         * 改进：
         * 1、适配不同型号 CPU
         */
        private float AutoModePowerLimit()
        {
            var cpuUsage = _hardwareMonitor.CpuUsage;
            var videoCard3DUsage = _hardwareMonitor.VideoCard3DUsage;
            var cpuTemperature = _hardwareMonitor.CpuTemperature;

            var isNight = CommonUtils.IsNight(DateTime.Now);

            // 自动模式下，在几个模式下切换：待机、平衡、性能
            // 插电模式下
            var sleepPower = CommonUtils.GetPowerLimitByMode("SleepMode");
            var balancedPower = CommonUtils.GetPowerLimitByMode("BalancedMode");
            var performancePower = CommonUtils.GetPowerLimitByMode("PerformanceMode");


            // 电池模式下，最高性能为【平衡】
            if (SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline)
            {
                performancePower = CommonUtils.GetPowerLimitByMode("BalancedMode");
            }

            // 夜晚，最高性能为【平衡】
            if (isNight)
            {
                performancePower = CommonUtils.GetPowerLimitByMode("BalancedMode");
            }

            // 默认使用【平衡】
            var powerLimit = balancedPower;

            // 符合下面条件之一的情况下，使用【待机】
            var idleSecond = CommonUtils.GetIdleSecond();
            if (
                // 条件1：白天 && 非活跃时间超过32分钟 && CPU 占用小于 10% && 显卡占用小于 10%
                (!isNight && idleSecond >= 32 * 60 && cpuUsage < 10 && videoCard3DUsage < 10) ||
                // 条件2：夜晚 && 非活跃时间超过4分钟 && CPU 占用小于 20% && 显卡占用小于 20%
                (isNight && idleSecond >= 4 * 60 && cpuUsage < 20 && videoCard3DUsage < 20) ||
                // 条件3：锁屏状态下 && 非活跃时间超过 2 秒 && CPU 占用小于 15% && 显卡占用小于 15%
                (CommonUtils.IsSystemLocked() && idleSecond >= 2 && cpuUsage < 15 && videoCard3DUsage < 15)
            )
            {
                powerLimit = sleepPower;
            }

            // CPU 占用大于 50%，温度在 65 度以内，使用【性能模式】
            if (cpuUsage >= 50 && cpuTemperature < 65)
            {
                powerLimit = performancePower;
            }

            return powerLimit;
        }

        private void SyncEnergyModeSelection()
        {
            foreach (Control c in groupBox1.Controls)
            {
                if (c.Tag != null && c.Tag.ToString() == Properties.Settings.Default.CurrentMode)
                {
                    RadioButton rb = (RadioButton)c;
                    rb.Checked = true;
                }
            }

            foreach (ToolStripItem tsmi in contextMenuStrip1.Items)
            {
                if (tsmi.Tag != null && tsmi.Tag.ToString() == Properties.Settings.Default.CurrentMode)
                {
                    ToolStripMenuItem tsmi2 = (ToolStripMenuItem)tsmi;
                    tsmi2.Checked = true;
                }
                else if (tsmi is ToolStripMenuItem)
                {
                    ToolStripMenuItem tsmi2 = (ToolStripMenuItem)tsmi;
                    tsmi2.Checked = false;
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (radioButton6.Checked) radioButton5.Checked = true;
            Properties.Settings.Default.CustomMode = textBox1.Text;
            Properties.Settings.Default.Save();
        }

        private void ToolStripMenuItems_Clicked(object sender, EventArgs e)
        {
            foreach (Control c in groupBox1.Controls)
            {
                if (c.Tag != null && c.Tag.ToString() == ((ToolStripMenuItem)sender).Tag.ToString())
                {
                    RadioButton rb = (RadioButton)c;
                    rb.Checked = true;
                }
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "-hide")
            {
                this.Hide();
            }
        }

        private string GetModeDetailText(string mode)
        {
            return String.Format("{0}-{1}W",
                Properties.Strings.ResourceManager.GetString(mode),
                CommonUtils.GetPowerLimitByMode(mode)
            );
        }

        private float GetPowerLimit()
        {
            var powerLimit = CommonUtils.GetPowerLimitByMode(Properties.Settings.Default.CurrentMode);

            // 自动模式下，根据系统状态自动调整
            if (Properties.Settings.Default.CurrentMode == "AutoMode")
            {
                powerLimit = this.AutoModePowerLimit();
            }

            // 数值修正
            if (powerLimit < 0)
            {
                powerLimit = 1;
            }

            return powerLimit;
        }

        private string GetNoticeText()
        {
            var powerLimit = this.GetPowerLimit();

            var noticeText = string.Format(@"[{0}]
限制功率：{1:0}W，实际功率：{2:0}W
CPU: {3:0}%, CPU：{4:0}℃，GPU: {5:0}%",
                Properties.Settings.Default.CurrentMode,
                powerLimit,
                _hardwareMonitor.CpuPackagePower,
                _hardwareMonitor.CpuUsage,
                _hardwareMonitor.CpuTemperature,
                _hardwareMonitor.VideoCard3DUsage
            );
            if (noticeText.Length > 64)
            {
                noticeText = noticeText.Substring(0, 64);
            }

            return noticeText;
        }

        private string CalcRyzenAdjArg()
        {
            var powerLimit = this.GetPowerLimit();
            var argArr = new List<string>
            {
                // 持续功率限制
                $"--stapm-limit {powerLimit * 1000}",
                // 实际功率限制
                $"--fast-limit {powerLimit * 1000}",
                // 平均功率限制
                $"--slow-limit {powerLimit * 1000}"
            };

            // 预设参数
            if (CommonUtils.IsSleepMode(powerLimit))
            {
                argArr.Add("--tctl-temp 50");
            }
            else if (CommonUtils.IsPowerSaveModeMode(powerLimit))
            {
                argArr.Add("--tctl-temp 55");
            }
            else if (CommonUtils.IsBalancedMode(powerLimit))
            {
                argArr.Add("--tctl-temp 60");
            }
            else if (CommonUtils.IsPerformanceModeMode(powerLimit))
            {
                argArr.Add("--tctl-temp 70");
            }
            else
            {
                argArr.Add("--tctl-temp 90");
            }

            var argText = string.Join(" ", argArr.ToArray());
            argText = argText.Trim();

            return argText;
        }
    }
}