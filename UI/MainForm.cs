using System;
using System.Windows.Forms;
using RyzenTuner.Common;
using RyzenTuner.Common.Container;
using RyzenTuner.Utils;

namespace RyzenTuner.UI
{
    public partial class MainForm : Form
    {
        private Int64 _tickCount;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            checkBoxEnergyStar.Checked = Properties.Settings.Default.EnergyStar;
            keepAwakeCheckBox.Checked = Properties.Settings.Default.KeepAwake;
            textBox1.Text = Properties.Settings.Default.CustomMode;
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
            new AboutForm().ShowDialog();
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
                Properties.Settings.Default.CurrentMode = checkedMode;
                Properties.Settings.Default.Save();
                SyncEnergyModeSelection();

                DoPowerLimit();
            }
        }

        private void DoPowerLimit()
        {
            try
            {
                notifyIcon1.Text = RyzenTunerUtils.GetNoticeText();

                var powerLimit = RyzenAdjUtils.GetPowerLimit();
                var tctlTemp = RyzenAdjUtils.GetTctlTemp();

                // 调用 ryzenadj 调整 Cpu 设置
                AppContainer.AmdProcessor().SetAllTdpLimit(powerLimit);
                AppContainer.AmdProcessor().SetTctlTemp((uint)tctlTemp);

                // 配置系统电源计划
                // 1、仅在【性能模式】下开启睿频
                if (RyzenTunerUtils.IsPerformanceMode(powerLimit))
                {
                    AppContainer.PowerConfig().EnableCpuBoost();
                }
                else
                {
                    AppContainer.PowerConfig().DisableCpuBoost();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                radioButton5.Checked = true;
                ChangeEnergyMode(radioButton5, EventArgs.Empty);
            }
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
            if (radioButton6.Checked) radioButton5.Checked = true;
            Properties.Settings.Default.CustomMode = textBox1.Text;
            Properties.Settings.Default.Save();
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