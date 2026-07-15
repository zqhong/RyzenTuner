using System.ComponentModel;

namespace RyzenTuner.UI
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBoxModeLimits = new System.Windows.Forms.GroupBox();
            this.labelPerformanceMode = new System.Windows.Forms.Label();
            this.numericUpDownPerformanceMode = new System.Windows.Forms.NumericUpDown();
            this.labelPerformanceWatt = new System.Windows.Forms.Label();
            this.labelBalancedMode = new System.Windows.Forms.Label();
            this.numericUpDownBalancedMode = new System.Windows.Forms.NumericUpDown();
            this.labelBalancedWatt = new System.Windows.Forms.Label();
            this.labelPowerSaveMode = new System.Windows.Forms.Label();
            this.numericUpDownPowerSaveMode = new System.Windows.Forms.NumericUpDown();
            this.labelPowerSaveWatt = new System.Windows.Forms.Label();
            this.labelSleepMode = new System.Windows.Forms.Label();
            this.numericUpDownSleepMode = new System.Windows.Forms.NumericUpDown();
            this.labelSleepWatt = new System.Windows.Forms.Label();

            this.groupBoxAdvanced = new System.Windows.Forms.GroupBox();
            this.labelTctlTemp = new System.Windows.Forms.Label();
            this.numericUpDownTctlTemp = new System.Windows.Forms.NumericUpDown();
            this.labelTctlTempUnit = new System.Windows.Forms.Label();
            this.labelFastPPT = new System.Windows.Forms.Label();
            this.numericUpDownFastPPT = new System.Windows.Forms.NumericUpDown();
            this.labelFastPPTWatt = new System.Windows.Forms.Label();
            this.labelSlowPPT = new System.Windows.Forms.Label();
            this.numericUpDownSlowPPT = new System.Windows.Forms.NumericUpDown();
            this.labelSlowPPTWatt = new System.Windows.Forms.Label();

            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();

            this.groupBoxModeLimits.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPerformanceMode)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBalancedMode)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPowerSaveMode)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSleepMode)).BeginInit();
            this.groupBoxAdvanced.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTctlTemp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFastPPT)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSlowPPT)).BeginInit();
            this.SuspendLayout();

            //
            // groupBoxModeLimits
            //
            this.groupBoxModeLimits.Controls.Add(this.labelPerformanceMode);
            this.groupBoxModeLimits.Controls.Add(this.numericUpDownPerformanceMode);
            this.groupBoxModeLimits.Controls.Add(this.labelPerformanceWatt);
            this.groupBoxModeLimits.Controls.Add(this.labelBalancedMode);
            this.groupBoxModeLimits.Controls.Add(this.numericUpDownBalancedMode);
            this.groupBoxModeLimits.Controls.Add(this.labelBalancedWatt);
            this.groupBoxModeLimits.Controls.Add(this.labelPowerSaveMode);
            this.groupBoxModeLimits.Controls.Add(this.numericUpDownPowerSaveMode);
            this.groupBoxModeLimits.Controls.Add(this.labelPowerSaveWatt);
            this.groupBoxModeLimits.Controls.Add(this.labelSleepMode);
            this.groupBoxModeLimits.Controls.Add(this.numericUpDownSleepMode);
            this.groupBoxModeLimits.Controls.Add(this.labelSleepWatt);
            this.groupBoxModeLimits.Location = new System.Drawing.Point(12, 12);
            this.groupBoxModeLimits.Name = "groupBoxModeLimits";
            this.groupBoxModeLimits.Size = new System.Drawing.Size(636, 160);
            this.groupBoxModeLimits.TabIndex = 0;
            this.groupBoxModeLimits.TabStop = false;
            this.groupBoxModeLimits.Text = Properties.Strings.TextPowerLimitSettings;

            // 第一行左：睡眠模式
            this.labelSleepMode.AutoSize = true;
            this.labelSleepMode.Location = new System.Drawing.Point(14, 35);
            this.labelSleepMode.Name = "labelSleepMode";
            this.labelSleepMode.Size = new System.Drawing.Size(120, 24);
            this.labelSleepMode.TabIndex = 0;
            this.labelSleepMode.Text = Properties.Strings.SleepMode;

            this.numericUpDownSleepMode.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownSleepMode.Location = new System.Drawing.Point(170, 33);
            this.numericUpDownSleepMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownSleepMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownSleepMode.Name = "numericUpDownSleepMode";
            this.numericUpDownSleepMode.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownSleepMode.TabIndex = 1;
            this.numericUpDownSleepMode.Value = new decimal(new int[] { 8, 0, 0, 0 });

            this.labelSleepWatt.AutoSize = true;
            this.labelSleepWatt.Location = new System.Drawing.Point(256, 35);
            this.labelSleepWatt.Name = "labelSleepWatt";
            this.labelSleepWatt.Size = new System.Drawing.Size(24, 24);
            this.labelSleepWatt.TabIndex = 2;
            this.labelSleepWatt.Text = Properties.Strings.TextWatts;

            // 第二行左：省电模式
            this.labelPowerSaveMode.AutoSize = true;
            this.labelPowerSaveMode.Location = new System.Drawing.Point(14, 73);
            this.labelPowerSaveMode.Name = "labelPowerSaveMode";
            this.labelPowerSaveMode.Size = new System.Drawing.Size(120, 24);
            this.labelPowerSaveMode.TabIndex = 3;
            this.labelPowerSaveMode.Text = Properties.Strings.PowerSaveMode;

            this.numericUpDownPowerSaveMode.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownPowerSaveMode.Location = new System.Drawing.Point(170, 71);
            this.numericUpDownPowerSaveMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownPowerSaveMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownPowerSaveMode.Name = "numericUpDownPowerSaveMode";
            this.numericUpDownPowerSaveMode.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownPowerSaveMode.TabIndex = 4;
            this.numericUpDownPowerSaveMode.Value = new decimal(new int[] { 16, 0, 0, 0 });

            this.labelPowerSaveWatt.AutoSize = true;
            this.labelPowerSaveWatt.Location = new System.Drawing.Point(256, 73);
            this.labelPowerSaveWatt.Name = "labelPowerSaveWatt";
            this.labelPowerSaveWatt.Size = new System.Drawing.Size(24, 24);
            this.labelPowerSaveWatt.TabIndex = 5;
            this.labelPowerSaveWatt.Text = Properties.Strings.TextWatts;

            // 第三行左（实际上是在右列）：平衡模式
            this.labelBalancedMode.AutoSize = true;
            this.labelBalancedMode.Location = new System.Drawing.Point(370, 35);
            this.labelBalancedMode.Name = "labelBalancedMode";
            this.labelBalancedMode.Size = new System.Drawing.Size(120, 24);
            this.labelBalancedMode.TabIndex = 6;
            this.labelBalancedMode.Text = Properties.Strings.BalancedMode;

            this.numericUpDownBalancedMode.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownBalancedMode.Location = new System.Drawing.Point(470, 33);
            this.numericUpDownBalancedMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownBalancedMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownBalancedMode.Name = "numericUpDownBalancedMode";
            this.numericUpDownBalancedMode.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownBalancedMode.TabIndex = 7;
            this.numericUpDownBalancedMode.Value = new decimal(new int[] { 26, 0, 0, 0 });

            this.labelBalancedWatt.AutoSize = true;
            this.labelBalancedWatt.Location = new System.Drawing.Point(556, 35);
            this.labelBalancedWatt.Name = "labelBalancedWatt";
            this.labelBalancedWatt.Size = new System.Drawing.Size(24, 24);
            this.labelBalancedWatt.TabIndex = 8;
            this.labelBalancedWatt.Text = Properties.Strings.TextWatts;

            // 第四行（右列）：性能模式
            this.labelPerformanceMode.AutoSize = true;
            this.labelPerformanceMode.Location = new System.Drawing.Point(370, 73);
            this.labelPerformanceMode.Name = "labelPerformanceMode";
            this.labelPerformanceMode.Size = new System.Drawing.Size(120, 24);
            this.labelPerformanceMode.TabIndex = 9;
            this.labelPerformanceMode.Text = Properties.Strings.PerformanceMode;

            this.numericUpDownPerformanceMode.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownPerformanceMode.Location = new System.Drawing.Point(470, 71);
            this.numericUpDownPerformanceMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownPerformanceMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownPerformanceMode.Name = "numericUpDownPerformanceMode";
            this.numericUpDownPerformanceMode.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownPerformanceMode.TabIndex = 10;
            this.numericUpDownPerformanceMode.Value = new decimal(new int[] { 45, 0, 0, 0 });

            this.labelPerformanceWatt.AutoSize = true;
            this.labelPerformanceWatt.Location = new System.Drawing.Point(556, 73);
            this.labelPerformanceWatt.Name = "labelPerformanceWatt";
            this.labelPerformanceWatt.Size = new System.Drawing.Size(24, 24);
            this.labelPerformanceWatt.TabIndex = 11;
            this.labelPerformanceWatt.Text = Properties.Strings.TextWatts;

            //
            // groupBoxAdvanced
            //
            this.groupBoxAdvanced.Controls.Add(this.labelTctlTemp);
            this.groupBoxAdvanced.Controls.Add(this.numericUpDownTctlTemp);
            this.groupBoxAdvanced.Controls.Add(this.labelTctlTempUnit);
            this.groupBoxAdvanced.Controls.Add(this.labelFastPPT);
            this.groupBoxAdvanced.Controls.Add(this.numericUpDownFastPPT);
            this.groupBoxAdvanced.Controls.Add(this.labelFastPPTWatt);
            this.groupBoxAdvanced.Controls.Add(this.labelSlowPPT);
            this.groupBoxAdvanced.Controls.Add(this.numericUpDownSlowPPT);
            this.groupBoxAdvanced.Controls.Add(this.labelSlowPPTWatt);
            this.groupBoxAdvanced.Location = new System.Drawing.Point(12, 180);
            this.groupBoxAdvanced.Name = "groupBoxAdvanced";
            this.groupBoxAdvanced.Size = new System.Drawing.Size(636, 120);
            this.groupBoxAdvanced.TabIndex = 1;
            this.groupBoxAdvanced.TabStop = false;
            this.groupBoxAdvanced.Text = Properties.Strings.TextAdvancedSettings;

            // TctlTemp
            this.labelTctlTemp.AutoSize = true;
            this.labelTctlTemp.Location = new System.Drawing.Point(14, 33);
            this.labelTctlTemp.Name = "labelTctlTemp";
            this.labelTctlTemp.Size = new System.Drawing.Size(150, 24);
            this.labelTctlTemp.TabIndex = 0;
            this.labelTctlTemp.Text = Properties.Strings.TextTctlTemp;

            this.numericUpDownTctlTemp.Location = new System.Drawing.Point(170, 31);
            this.numericUpDownTctlTemp.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            this.numericUpDownTctlTemp.Minimum = new decimal(new int[] { 30, 0, 0, 0 });
            this.numericUpDownTctlTemp.Name = "numericUpDownTctlTemp";
            this.numericUpDownTctlTemp.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownTctlTemp.TabIndex = 1;
            this.numericUpDownTctlTemp.Value = new decimal(new int[] { 100, 0, 0, 0 });

            this.labelTctlTempUnit.AutoSize = true;
            this.labelTctlTempUnit.Location = new System.Drawing.Point(256, 33);
            this.labelTctlTempUnit.Name = "labelTctlTempUnit";
            this.labelTctlTempUnit.Size = new System.Drawing.Size(24, 24);
            this.labelTctlTempUnit.TabIndex = 2;
            this.labelTctlTempUnit.Text = Properties.Strings.TextDegreeCelsius;

            // FastPPT
            this.labelFastPPT.AutoSize = true;
            this.labelFastPPT.Location = new System.Drawing.Point(370, 33);
            this.labelFastPPT.Name = "labelFastPPT";
            this.labelFastPPT.Size = new System.Drawing.Size(150, 24);
            this.labelFastPPT.TabIndex = 3;
            this.labelFastPPT.Text = Properties.Strings.TextFastPPT;

            this.numericUpDownFastPPT.Location = new System.Drawing.Point(470, 31);
            this.numericUpDownFastPPT.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownFastPPT.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownFastPPT.Name = "numericUpDownFastPPT";
            this.numericUpDownFastPPT.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownFastPPT.TabIndex = 4;
            this.numericUpDownFastPPT.Value = new decimal(new int[] { 51, 0, 0, 0 });

            this.labelFastPPTWatt.AutoSize = true;
            this.labelFastPPTWatt.Location = new System.Drawing.Point(556, 33);
            this.labelFastPPTWatt.Name = "labelFastPPTWatt";
            this.labelFastPPTWatt.Size = new System.Drawing.Size(24, 24);
            this.labelFastPPTWatt.TabIndex = 5;
            this.labelFastPPTWatt.Text = Properties.Strings.TextWatts;

            // SlowPPT
            this.labelSlowPPT.AutoSize = true;
            this.labelSlowPPT.Location = new System.Drawing.Point(14, 71);
            this.labelSlowPPT.Name = "labelSlowPPT";
            this.labelSlowPPT.Size = new System.Drawing.Size(150, 24);
            this.labelSlowPPT.TabIndex = 6;
            this.labelSlowPPT.Text = Properties.Strings.TextSlowPPT;

            this.numericUpDownSlowPPT.Location = new System.Drawing.Point(170, 69);
            this.numericUpDownSlowPPT.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownSlowPPT.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownSlowPPT.Name = "numericUpDownSlowPPT";
            this.numericUpDownSlowPPT.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownSlowPPT.TabIndex = 7;
            this.numericUpDownSlowPPT.Value = new decimal(new int[] { 45, 0, 0, 0 });

            this.labelSlowPPTWatt.AutoSize = true;
            this.labelSlowPPTWatt.Location = new System.Drawing.Point(256, 71);
            this.labelSlowPPTWatt.Name = "labelSlowPPTWatt";
            this.labelSlowPPTWatt.Size = new System.Drawing.Size(24, 24);
            this.labelSlowPPTWatt.TabIndex = 8;
            this.labelSlowPPTWatt.Text = Properties.Strings.TextWatts;

            //
            // buttonSave
            //
            this.buttonSave.Location = new System.Drawing.Point(225, 315);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(100, 35);
            this.buttonSave.TabIndex = 2;
            this.buttonSave.Text = Properties.Strings.TextSave;
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);

            //
            // buttonCancel
            //
            this.buttonCancel.Location = new System.Drawing.Point(335, 315);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(100, 35);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = Properties.Strings.TextCancel;
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);

            //
            // SettingsForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(660, 365);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.groupBoxAdvanced);
            this.Controls.Add(this.groupBoxModeLimits);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = Properties.Strings.TextSettingsTitle;
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.groupBoxModeLimits.ResumeLayout(false);
            this.groupBoxModeLimits.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPerformanceMode)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBalancedMode)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPowerSaveMode)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSleepMode)).EndInit();
            this.groupBoxAdvanced.ResumeLayout(false);
            this.groupBoxAdvanced.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTctlTemp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFastPPT)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSlowPPT)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxModeLimits;
        private System.Windows.Forms.GroupBox groupBoxAdvanced;

        // 各模式功率限制控件
        private System.Windows.Forms.Label labelSleepMode;
        private System.Windows.Forms.NumericUpDown numericUpDownSleepMode;
        private System.Windows.Forms.Label labelSleepWatt;

        private System.Windows.Forms.Label labelPowerSaveMode;
        private System.Windows.Forms.NumericUpDown numericUpDownPowerSaveMode;
        private System.Windows.Forms.Label labelPowerSaveWatt;

        private System.Windows.Forms.Label labelBalancedMode;
        private System.Windows.Forms.NumericUpDown numericUpDownBalancedMode;
        private System.Windows.Forms.Label labelBalancedWatt;

        private System.Windows.Forms.Label labelPerformanceMode;
        private System.Windows.Forms.NumericUpDown numericUpDownPerformanceMode;
        private System.Windows.Forms.Label labelPerformanceWatt;

        // 高级设置控件
        private System.Windows.Forms.Label labelTctlTemp;
        private System.Windows.Forms.NumericUpDown numericUpDownTctlTemp;
        private System.Windows.Forms.Label labelTctlTempUnit;

        private System.Windows.Forms.Label labelFastPPT;
        private System.Windows.Forms.NumericUpDown numericUpDownFastPPT;
        private System.Windows.Forms.Label labelFastPPTWatt;

        private System.Windows.Forms.Label labelSlowPPT;
        private System.Windows.Forms.NumericUpDown numericUpDownSlowPPT;
        private System.Windows.Forms.Label labelSlowPPTWatt;

        // 按钮
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonCancel;
    }
}
