namespace RyzenTuner
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("icon.Icon")));
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
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("icon.Icon")));
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

