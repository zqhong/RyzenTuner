using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

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
            SyncEnergyModeSelection();
            
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.EnergyStar = checkBox1.Checked;
            Properties.Settings.Default.Save();
            timer1_Tick(sender,e);
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
            if (checkBox1.Checked)
            {
                StartEnergyStar();
            }
            else
            {
                StopEnergyStar();
            }
        }

        public static void StartEnergyStar()
        {
            if (!System.Diagnostics.Process.GetProcessesByName("energystar").Any())
            {
                System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\energystar\\EnergyStar.exe");
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
            if (((RadioButton)sender).Checked )
            {
                string checkedMode = ((RadioButton)sender).Tag.ToString() ;
                Properties.Settings.Default.CurrentMode = checkedMode;
                Properties.Settings.Default.Save();
                SyncEnergyModeSelection();
                    
                try
                {
                    string[] ModeSetting = Properties.Settings.Default[checkedMode].ToString().Split('-');
                    float low = float.Parse(ModeSetting[0]);
                    float high = float.Parse(ModeSetting[1]);
                    if (high < low) throw new Exception();
                    notifyIcon1.Text = "RyzenTuner [" + checkedMode + "]\n最低功率：" + low + "W，最高功率：" + high + "W";
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\ryzenadj\\ryzenadj.exe";
                    startInfo.Arguments = "-a " + low * 1000 + " -b " + low * 1000 + " -c " + high * 1000;
                    process.StartInfo = startInfo;
                    process.Start();
                }
                catch
                {
                    MessageBox.Show("本模式功率范围设置有误，将恢复默认设置。");
                    radioButton5.Checked = true;
                    ChangeEnergyMode(radioButton5, e);
                }
                if (Properties.Settings.Default.CurrentMode == "CustomMode")
                {
                    textBox1.Enabled = false;
                }
                else
                {
                    textBox1.Enabled = true;
                }
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
                else if(tsmi is ToolStripMenuItem)
                {
                    ToolStripMenuItem tsmi2 = (ToolStripMenuItem)tsmi;
                    tsmi2.Checked = false;
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
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

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = "SCHTASKS.exe";
            startInfo.Arguments = "/Create /TN \"Ryzen Tuner\" /TR \"" + Application.ExecutablePath + " -hide\" /RU " + Environment.UserName + " /RL HIGHEST /SC ONLOGON /IT";
            process.StartInfo = startInfo;
            process.Start();
            MessageBox.Show("计划任务已添加，如需取消请自己到计划任务面板删除“Ryzen Tuner”任务。");
        }
    }
}
