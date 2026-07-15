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

            this.groupBoxAdvanced = new System.Windows.Forms.GroupBox();
            this.labelTctlTemp = new System.Windows.Forms.Label();
            this.numericUpDownTctlTemp = new System.Windows.Forms.NumericUpDown();
            this.labelTctlTempUnit = new System.Windows.Forms.Label();
            this.labelApuSkinTemp = new System.Windows.Forms.Label();
            this.numericUpDownApuSkinTemp = new System.Windows.Forms.NumericUpDown();
            this.labelApuSkinTempUnit = new System.Windows.Forms.Label();

            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();

            this.groupBoxModeLimits.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPerformanceMode)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBalancedMode)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPowerSaveMode)).BeginInit();
            this.groupBoxAdvanced.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTctlTemp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownApuSkinTemp)).BeginInit();
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
            this.groupBoxModeLimits.Location = new System.Drawing.Point(12, 12);
            this.groupBoxModeLimits.Name = "groupBoxModeLimits";
            this.groupBoxModeLimits.Size = new System.Drawing.Size(880, 130);
            this.groupBoxModeLimits.TabIndex = 0;
            this.groupBoxModeLimits.TabStop = false;
            this.groupBoxModeLimits.Text = Properties.Strings.TextPowerLimitSettings;

            // 第一行左：省电模式
            this.labelPowerSaveMode.AutoSize = true;
            this.labelPowerSaveMode.Location = new System.Drawing.Point(14, 35);
            this.labelPowerSaveMode.Name = "labelPowerSaveMode";
            this.labelPowerSaveMode.Size = new System.Drawing.Size(120, 24);
            this.labelPowerSaveMode.TabIndex = 3;
            this.labelPowerSaveMode.Text = Properties.Strings.PowerSaveMode;

            this.numericUpDownPowerSaveMode.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownPowerSaveMode.Location = new System.Drawing.Point(90, 33);
            this.numericUpDownPowerSaveMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownPowerSaveMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownPowerSaveMode.Name = "numericUpDownPowerSaveMode";
            this.numericUpDownPowerSaveMode.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownPowerSaveMode.TabIndex = 4;
            this.numericUpDownPowerSaveMode.Value = new decimal(new int[] { 16, 0, 0, 0 });

            this.labelPowerSaveWatt.AutoSize = true;
            this.labelPowerSaveWatt.Location = new System.Drawing.Point(178, 35);
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
            this.numericUpDownBalancedMode.Location = new System.Drawing.Point(446, 33);
            this.numericUpDownBalancedMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownBalancedMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownBalancedMode.Name = "numericUpDownBalancedMode";
            this.numericUpDownBalancedMode.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownBalancedMode.TabIndex = 7;
            this.numericUpDownBalancedMode.Value = new decimal(new int[] { 26, 0, 0, 0 });

            this.labelBalancedWatt.AutoSize = true;
            this.labelBalancedWatt.Location = new System.Drawing.Point(534, 35);
            this.labelBalancedWatt.Name = "labelBalancedWatt";
            this.labelBalancedWatt.Size = new System.Drawing.Size(24, 24);
            this.labelBalancedWatt.TabIndex = 8;
            this.labelBalancedWatt.Text = Properties.Strings.TextWatts;

            // 第二行左：性能模式
            this.labelPerformanceMode.AutoSize = true;
            this.labelPerformanceMode.Location = new System.Drawing.Point(14, 73);
            this.labelPerformanceMode.Name = "labelPerformanceMode";
            this.labelPerformanceMode.Size = new System.Drawing.Size(120, 24);
            this.labelPerformanceMode.TabIndex = 9;
            this.labelPerformanceMode.Text = Properties.Strings.PerformanceMode;

            this.numericUpDownPerformanceMode.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownPerformanceMode.Location = new System.Drawing.Point(90, 71);
            this.numericUpDownPerformanceMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownPerformanceMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownPerformanceMode.Name = "numericUpDownPerformanceMode";
            this.numericUpDownPerformanceMode.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownPerformanceMode.TabIndex = 10;
            this.numericUpDownPerformanceMode.Value = new decimal(new int[] { 45, 0, 0, 0 });

            this.labelPerformanceWatt.AutoSize = true;
            this.labelPerformanceWatt.Location = new System.Drawing.Point(178, 73);
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
            this.groupBoxAdvanced.Controls.Add(this.labelApuSkinTemp);
            this.groupBoxAdvanced.Controls.Add(this.numericUpDownApuSkinTemp);
            this.groupBoxAdvanced.Controls.Add(this.labelApuSkinTempUnit);
            this.groupBoxAdvanced.Location = new System.Drawing.Point(12, 180);
            this.groupBoxAdvanced.Name = "groupBoxAdvanced";
            this.groupBoxAdvanced.Size = new System.Drawing.Size(880, 120);
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

            this.numericUpDownTctlTemp.Location = new System.Drawing.Point(380, 31);
            this.numericUpDownTctlTemp.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            this.numericUpDownTctlTemp.Minimum = new decimal(new int[] { 30, 0, 0, 0 });
            this.numericUpDownTctlTemp.Name = "numericUpDownTctlTemp";
            this.numericUpDownTctlTemp.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownTctlTemp.TabIndex = 1;
            this.numericUpDownTctlTemp.Value = new decimal(new int[] { 100, 0, 0, 0 });

            this.labelTctlTempUnit.AutoSize = true;
            this.labelTctlTempUnit.Location = new System.Drawing.Point(470, 33);
            this.labelTctlTempUnit.Name = "labelTctlTempUnit";
            this.labelTctlTempUnit.Size = new System.Drawing.Size(24, 24);
            this.labelTctlTempUnit.TabIndex = 2;
            this.labelTctlTempUnit.Text = Properties.Strings.TextDegreeCelsius;

            // ApuSkinTemp
            this.labelApuSkinTemp.AutoSize = true;
            this.labelApuSkinTemp.Location = new System.Drawing.Point(14, 73);
            this.labelApuSkinTemp.Name = "labelApuSkinTemp";
            this.labelApuSkinTemp.Size = new System.Drawing.Size(320, 24);
            this.labelApuSkinTemp.TabIndex = 3;
            this.labelApuSkinTemp.Text = Properties.Strings.TextApuSkinTemp;

            this.numericUpDownApuSkinTemp.Location = new System.Drawing.Point(380, 71);
            this.numericUpDownApuSkinTemp.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            this.numericUpDownApuSkinTemp.Minimum = new decimal(new int[] { 30, 0, 0, 0 });
            this.numericUpDownApuSkinTemp.Name = "numericUpDownApuSkinTemp";
            this.numericUpDownApuSkinTemp.Size = new System.Drawing.Size(80, 30);
            this.numericUpDownApuSkinTemp.TabIndex = 4;
            this.numericUpDownApuSkinTemp.Value = new decimal(new int[] { 43, 0, 0, 0 });

            this.labelApuSkinTempUnit.AutoSize = true;
            this.labelApuSkinTempUnit.Location = new System.Drawing.Point(470, 73);
            this.labelApuSkinTempUnit.Name = "labelApuSkinTempUnit";
            this.labelApuSkinTempUnit.Size = new System.Drawing.Size(24, 24);
            this.labelApuSkinTempUnit.TabIndex = 5;
            this.labelApuSkinTempUnit.Text = Properties.Strings.TextDegreeCelsius;

            //
            // buttonSave
            //
            this.buttonSave.Location = new System.Drawing.Point(350, 325);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(100, 35);
            this.buttonSave.TabIndex = 6;
            this.buttonSave.Text = Properties.Strings.TextSave;
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);

            //
            // buttonCancel
            //
            this.buttonCancel.Location = new System.Drawing.Point(460, 325);
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
            this.ClientSize = new System.Drawing.Size(920, 370);
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
            this.groupBoxAdvanced.ResumeLayout(false);
            this.groupBoxAdvanced.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTctlTemp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownApuSkinTemp)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxModeLimits;
        private System.Windows.Forms.GroupBox groupBoxAdvanced;

        // 各模式功率限制控件
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

        private System.Windows.Forms.Label labelApuSkinTemp;
        private System.Windows.Forms.NumericUpDown numericUpDownApuSkinTemp;
        private System.Windows.Forms.Label labelApuSkinTempUnit;

        // 按钮
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonCancel;
    }
}
