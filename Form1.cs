using System;
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
            checkBox3.Checked = Properties.Settings.Default.CloseToTray;
            textBox1.Text = Properties.Settings.Default.CustomMode;
            SyncEnergyModeSelection();
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.EnergyStar = checkBox1.Checked;
            Properties.Settings.Default.Save();
            timer1_Tick(sender, e);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.CloseToTray = checkBox3.Checked;
            Properties.Settings.Default.Save();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && Properties.Settings.Default.CloseToTray)
            {
                if (radioButton6.Checked)
                {
                    ChangeEnergyMode(radioButton6, e);
                }

                e.Cancel = true;
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // check energystar.exe is running, if not, start it
            // EnergyStar 需要 OS Build 版本大于等于 22000，即 Windows 11 21H2。EnergyStar 开发者建议使用 22H2
            // 参考：
            // https://github.com/imbushuo/EnergyStar/blob/master/EnergyStar/Program.cs#L29-L39
            // https://github.com/imbushuo/EnergyStar/issues/10
            if (checkBox1.Checked)
            {
                if (Environment.OSVersion.Version.Build >= 22000)
                {
                    StartEnergyStar();
                }
            }
            else
            {
                StopEnergyStar();
            }

            ApplyEnergyMode();
        }

        private void metricTimer_Tick(object sender, EventArgs e)
        {
            if (currentCPUUsage != 0)
            {
                previousCPUUsage = currentCPUUsage;
            }

            currentCPUUsage = new CommonUtils().CpuUsage();
        }

        private static void StartEnergyStar()
        {
            if (!System.Diagnostics.Process.GetProcessesByName("energystar").Any())
            {
                System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(Application.ExecutablePath) +
                                                 "\\energystar\\EnergyStar.exe");
            }
        }

        public static void StopEnergyStar()
        {
            foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcessesByName("energystar"))
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
                string[] ModeSetting = Properties.Settings.Default[Properties.Settings.Default.CurrentMode].ToString()
                    .Split('-');
                float low = float.Parse(ModeSetting[0]);
                float high = float.Parse(ModeSetting[1]);

                // 自动模式下，根据系统状态自动调整
                if (Properties.Settings.Default.CurrentMode == "AutoMode")
                {
                    low = high = new CommonUtils().AutoModePowerLimit(currentCPUUsage);
                }

                // 数值修正
                if (high < low)
                {
                    high = low;
                }

                if (low < 0)
                {
                    low = 1;
                }

                var noticeText = string.Format(@"[{0}]
持续功率：{1}W，最高功率：{2}W",
                    Properties.Settings.Default.CurrentMode,
                    low,
                    high
                );
                if (noticeText.Length > 64)
                {
                    noticeText = noticeText.Substring(0, 64);
                }

                notifyIcon1.Text = noticeText;

                Process process = new System.Diagnostics.Process();
                ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                // TODO：与上次参数一致的情况下，不调用 ryzenadj.exe
                
                // --stapm-limit：持续功率限制
                // --fast-limit：实际功率限制
                // --slow-limit：平均功率限制
                startInfo.FileName = System.IO.Path.GetDirectoryName(Application.ExecutablePath) +
                                     "\\ryzenadj\\ryzenadj.exe";
                startInfo.Arguments = "--stapm-limit " + low * 1000 + " --fast-limit " + low * 1000 + " --slow-limit " +
                                      high * 1000;
                process.StartInfo = startInfo;
                process.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                radioButton5.Checked = true;
                ChangeEnergyMode(radioButton5, new EventArgs());
            }
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
    }
}