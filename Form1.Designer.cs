using System.IO;

namespace RyzenTuner
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /**
         * 返回当前应用的图标
         */
        private System.Drawing.Icon getIcon()
        {
            var stringInBase64 = "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";
            byte[] bytes = System.Convert.FromBase64String(stringInBase64);
            using(var ms = new MemoryStream(bytes)) {
                return new System.Drawing.Icon(ms);
            }
        }
        
        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.radioButton6 = new System.Windows.Forms.RadioButton();
            this.radioButton5 = new System.Windows.Forms.RadioButton();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.自动模式25WCtrlAltShiftF1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.极致续航模式4WToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.续航模式6WToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.游戏模式15WToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.恢复默认设置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.自定义ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.退出ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            
            this.timer1 = new System.Windows.Forms.Timer(this.components);

            this._cpuUsage = new CpuUsage();
            this._cpuUsage.GetCpuUsage();
            
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.radioButton6);
            this.groupBox1.Controls.Add(this.radioButton5);
            this.groupBox1.Controls.Add(this.radioButton4);
            this.groupBox1.Controls.Add(this.radioButton3);
            this.groupBox1.Controls.Add(this.radioButton2);
            this.groupBox1.Controls.Add(this.radioButton1);
            this.groupBox1.Location = new System.Drawing.Point(25, 21);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(706, 141);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "功耗限制";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(583, 87);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(82, 35);
            this.textBox1.TabIndex = 6;
            this.textBox1.Text = "5-15";
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // radioButton6
            // 
            this.radioButton6.AutoSize = true;
            this.radioButton6.Location = new System.Drawing.Point(464, 88);
            this.radioButton6.Name = "radioButton6";
            this.radioButton6.Size = new System.Drawing.Size(113, 28);
            this.radioButton6.TabIndex = 5;
            this.radioButton6.Tag = "CustomMode";
            this.radioButton6.Text = "自定义";
            this.radioButton6.UseVisualStyleBackColor = true;
            this.radioButton6.CheckedChanged += new System.EventHandler(this.ChangeEnergyMode);
            // 
            // radioButton5
            // 
            this.radioButton5.AutoSize = true;
            this.radioButton5.Checked = true;
            this.radioButton5.Location = new System.Drawing.Point(227, 88);
            this.radioButton5.Name = "radioButton5";
            this.radioButton5.Size = new System.Drawing.Size(185, 28);
            this.radioButton5.TabIndex = 4;
            this.radioButton5.TabStop = true;
            this.radioButton5.Tag = "FactoryDefaultMode";
            this.radioButton5.Text = "恢复默认设置";
            this.radioButton5.UseVisualStyleBackColor = true;
            this.radioButton5.CheckedChanged += new System.EventHandler(this.ChangeEnergyMode);
            // 
            // radioButton4
            // 
            this.radioButton4.AutoSize = true;
            this.radioButton4.Location = new System.Drawing.Point(24, 88);
            this.radioButton4.Name = "radioButton4";
            this.radioButton4.Size = new System.Drawing.Size(185, 28);
            this.radioButton4.TabIndex = 3;
            this.radioButton4.Tag = "GamingMode";
            this.radioButton4.Text = "游戏模式";
            this.radioButton4.UseVisualStyleBackColor = true;
            this.radioButton4.CheckedChanged += new System.EventHandler(this.ChangeEnergyMode);
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Location = new System.Drawing.Point(464, 37);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(173, 28);
            this.radioButton3.TabIndex = 2;
            this.radioButton3.Tag = "BatteryLifeMode";
            this.radioButton3.Text = "续航模式";
            this.radioButton3.UseVisualStyleBackColor = true;
            this.radioButton3.CheckedChanged += new System.EventHandler(this.ChangeEnergyMode);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(227, 38);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(221, 28);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.Tag = "ExtendedBatteryLifeMode";
            this.radioButton2.Text = "极致续航模式";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.ChangeEnergyMode);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(24, 38);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(197, 28);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.Tag = "AutoMode";
            this.radioButton1.Text = "自动模式";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.ChangeEnergyMode);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.checkBox3);
            this.groupBox3.Controls.Add(this.checkBox1);
            this.groupBox3.Location = new System.Drawing.Point(29, 180);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(702, 85);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "其他工具";
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(261, 34);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(210, 28);
            this.checkBox3.TabIndex = 2;
            this.checkBox3.Text = "关闭到托盘菜单";
            this.checkBox3.UseVisualStyleBackColor = true;
            this.checkBox3.CheckedChanged += new System.EventHandler(this.checkBox3_CheckedChanged);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(21, 34);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(222, 28);
            this.checkBox1.TabIndex = 0;
            this.checkBox1.Text = "启用 EnergyStar";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = this.getIcon();
            this.notifyIcon1.Text = "Tuner For Ryzen 运行中";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.自动模式25WCtrlAltShiftF1ToolStripMenuItem,
            this.极致续航模式4WToolStripMenuItem,
            this.续航模式6WToolStripMenuItem,
            this.游戏模式15WToolStripMenuItem,
            this.恢复默认设置ToolStripMenuItem,
            this.自定义ToolStripMenuItem,
            this.toolStripSeparator1,
            this.退出ToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(281, 276);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // 自动模式25WCtrlAltShiftF1ToolStripMenuItem
            // 
            this.自动模式25WCtrlAltShiftF1ToolStripMenuItem.CheckOnClick = true;
            this.自动模式25WCtrlAltShiftF1ToolStripMenuItem.Name = "自动模式25WCtrlAltShiftF1ToolStripMenuItem";
            this.自动模式25WCtrlAltShiftF1ToolStripMenuItem.Size = new System.Drawing.Size(280, 38);
            this.自动模式25WCtrlAltShiftF1ToolStripMenuItem.Tag = "AutoMode";
            this.自动模式25WCtrlAltShiftF1ToolStripMenuItem.Text = "自动模式";
            this.自动模式25WCtrlAltShiftF1ToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItems_Clicked);
            // 
            // 极致续航模式4WToolStripMenuItem
            // 
            this.极致续航模式4WToolStripMenuItem.CheckOnClick = true;
            this.极致续航模式4WToolStripMenuItem.Name = "极致续航模式4WToolStripMenuItem";
            this.极致续航模式4WToolStripMenuItem.Size = new System.Drawing.Size(280, 38);
            this.极致续航模式4WToolStripMenuItem.Tag = "ExtendedBatteryLifeMode";
            this.极致续航模式4WToolStripMenuItem.Text = "极致续航模式";
            this.极致续航模式4WToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItems_Clicked);
            // 
            // 续航模式6WToolStripMenuItem
            // 
            this.续航模式6WToolStripMenuItem.CheckOnClick = true;
            this.续航模式6WToolStripMenuItem.Name = "续航模式6WToolStripMenuItem";
            this.续航模式6WToolStripMenuItem.Size = new System.Drawing.Size(280, 38);
            this.续航模式6WToolStripMenuItem.Tag = "BatteryLifeMode";
            this.续航模式6WToolStripMenuItem.Text = "续航模式";
            this.续航模式6WToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItems_Clicked);
            // 
            // 游戏模式15WToolStripMenuItem
            // 
            this.游戏模式15WToolStripMenuItem.CheckOnClick = true;
            this.游戏模式15WToolStripMenuItem.Name = "游戏模式15WToolStripMenuItem";
            this.游戏模式15WToolStripMenuItem.Size = new System.Drawing.Size(280, 38);
            this.游戏模式15WToolStripMenuItem.Tag = "GamingMode";
            this.游戏模式15WToolStripMenuItem.Text = "游戏模式";
            this.游戏模式15WToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItems_Clicked);
            
            // 
            // 恢复默认设置ToolStripMenuItem
            // 
            this.恢复默认设置ToolStripMenuItem.CheckOnClick = true;
            this.恢复默认设置ToolStripMenuItem.Name = "恢复默认设置ToolStripMenuItem";
            this.恢复默认设置ToolStripMenuItem.Size = new System.Drawing.Size(280, 38);
            this.恢复默认设置ToolStripMenuItem.Tag = "FactoryDefaultMode";
            this.恢复默认设置ToolStripMenuItem.Text = "恢复默认设置";
            this.恢复默认设置ToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItems_Clicked);
            // 
            // 自定义ToolStripMenuItem
            // 
            this.自定义ToolStripMenuItem.CheckOnClick = true;
            this.自定义ToolStripMenuItem.Name = "自定义ToolStripMenuItem";
            this.自定义ToolStripMenuItem.Size = new System.Drawing.Size(280, 38);
            this.自定义ToolStripMenuItem.Tag = "CustomMode";
            this.自定义ToolStripMenuItem.Text = "自定义";
            this.自定义ToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItems_Clicked);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(277, 6);
            // 
            // 退出ToolStripMenuItem
            // 
            this.退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            this.退出ToolStripMenuItem.Size = new System.Drawing.Size(280, 38);
            this.退出ToolStripMenuItem.Text = "退出";
            this.退出ToolStripMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);
            
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 2048;        // 单位：毫秒
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(757, 291);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Icon = this.getIcon();
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.Text = "Tuner for Ryzen";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.RadioButton radioButton6;
        private System.Windows.Forms.RadioButton radioButton5;
        private System.Windows.Forms.RadioButton radioButton4;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 自动模式25WCtrlAltShiftF1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 极致续航模式4WToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 续航模式6WToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 游戏模式15WToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 恢复默认设置ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 自定义ToolStripMenuItem;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem 退出ToolStripMenuItem;

        private CpuUsage _cpuUsage;
    }
}

