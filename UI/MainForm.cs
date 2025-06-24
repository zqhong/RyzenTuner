using System;
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
                notifyIcon1.Text = RyzenTunerUtils.GetNoticeText();
                var processor = AppContainer.AmdProcessor();

                var stampLimit = RyzenAdjUtils.GetPowerLimit();
                var tctlTemp = RyzenAdjUtils.GetTctlTemp();
                var fastPpt = Settings.Default.FastPPT;
                var slowPpt = Settings.Default.SlowPPT;
                
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
                processor.SetFastPPT(stampLimit);
                processor.SetSlowPPT(slowPpt);
                processor.SetStampPPT(stampLimit);

                AppContainer.AmdProcessor().SetTctlTemp((uint)tctlTemp);

                // 配置系统电源计划
                // 1、仅在【性能模式】下开启睿频
                if (RyzenTunerUtils.IsPerformanceMode(stampLimit))
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