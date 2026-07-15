using System.ComponentModel;

namespace RyzenTuner.UI
{
    partial class BenchmarkForm
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
            this.components = new System.ComponentModel.Container();
            this.groupBoxConfig = new System.Windows.Forms.GroupBox();
            this.labelTestType = new System.Windows.Forms.Label();
            this.comboBoxTestType = new System.Windows.Forms.ComboBox();
            this.labelStartPower = new System.Windows.Forms.Label();
            this.numericUpDownStartPower = new System.Windows.Forms.NumericUpDown();
            this.labelStartWatts = new System.Windows.Forms.Label();
            this.labelStep = new System.Windows.Forms.Label();
            this.numericUpDownStep = new System.Windows.Forms.NumericUpDown();
            this.labelStepWatts = new System.Windows.Forms.Label();
            this.labelEndPower = new System.Windows.Forms.Label();
            this.numericUpDownEndPower = new System.Windows.Forms.NumericUpDown();
            this.labelEndWatts = new System.Windows.Forms.Label();
            this.labelDuration = new System.Windows.Forms.Label();
            this.numericUpDownDuration = new System.Windows.Forms.NumericUpDown();
            this.labelDurationUnit = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();

            this.labelStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();

            this.dataGridViewResults = new System.Windows.Forms.DataGridView();

            // Rest time controls - must be created before Controls.Add use
            this.labelRestTime = new System.Windows.Forms.Label();
            this.numericUpDownRestTime = new System.Windows.Forms.NumericUpDown();
            this.labelRestSecondsUnit = new System.Windows.Forms.Label();

            this.groupBoxConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartPower)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStep)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownEndPower)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRestTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewResults)).BeginInit();
            this.SuspendLayout();

            //
            // groupBoxConfig
            //
            this.groupBoxConfig.Controls.Add(this.labelTestType);
            this.groupBoxConfig.Controls.Add(this.comboBoxTestType);
            this.groupBoxConfig.Controls.Add(this.labelDuration);
            this.groupBoxConfig.Controls.Add(this.numericUpDownDuration);
            this.groupBoxConfig.Controls.Add(this.labelDurationUnit);
            this.groupBoxConfig.Controls.Add(this.buttonStart);
            this.groupBoxConfig.Controls.Add(this.buttonStop);
            this.groupBoxConfig.Controls.Add(this.buttonExportCsv);
            this.groupBoxConfig.Controls.Add(this.labelStartPower);
            this.groupBoxConfig.Controls.Add(this.numericUpDownStartPower);
            this.groupBoxConfig.Controls.Add(this.labelStartWatts);
            this.groupBoxConfig.Controls.Add(this.labelStep);
            this.groupBoxConfig.Controls.Add(this.numericUpDownStep);
            this.groupBoxConfig.Controls.Add(this.labelStepWatts);
            this.groupBoxConfig.Controls.Add(this.labelEndPower);
            this.groupBoxConfig.Controls.Add(this.numericUpDownEndPower);
            this.groupBoxConfig.Controls.Add(this.labelEndWatts);
            this.groupBoxConfig.Controls.Add(this.labelRestTime);
            this.groupBoxConfig.Controls.Add(this.numericUpDownRestTime);
            this.groupBoxConfig.Controls.Add(this.labelRestSecondsUnit);
            this.groupBoxConfig.Location = new System.Drawing.Point(12, 12);
            this.groupBoxConfig.Name = "groupBoxConfig";
            this.groupBoxConfig.Size = new System.Drawing.Size(1200, 120);
            this.groupBoxConfig.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxConfig.TabIndex = 0;
            this.groupBoxConfig.TabStop = false;
            this.groupBoxConfig.Text = Properties.Strings.TextBenchmark;

            // === Row 1: 测试内容  测试时间  休息时间 ===

            // labelTestType
            this.labelTestType.AutoSize = true;
            this.labelTestType.Location = new System.Drawing.Point(15, 32);
            this.labelTestType.Name = "labelTestType";
            this.labelTestType.Size = new System.Drawing.Size(100, 24);
            this.labelTestType.TabIndex = 0;
            this.labelTestType.Text = Properties.Strings.TextBenchmarkTestType;

            // comboBoxTestType
            this.comboBoxTestType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxTestType.Items.AddRange(new object[] {
                Properties.Strings.TextBenchmarkSingleCore,
                Properties.Strings.TextBenchmarkMultiCore
            });
            this.comboBoxTestType.Location = new System.Drawing.Point(100, 29);
            this.comboBoxTestType.Name = "comboBoxTestType";
            this.comboBoxTestType.Size = new System.Drawing.Size(180, 32);
            this.comboBoxTestType.TabIndex = 1;
            this.comboBoxTestType.SelectedIndex = 0;

            // labelDuration
            this.labelDuration.AutoSize = true;
            this.labelDuration.Location = new System.Drawing.Point(340, 32);
            this.labelDuration.Name = "labelDuration";
            this.labelDuration.Size = new System.Drawing.Size(100, 24);
            this.labelDuration.TabIndex = 11;
            this.labelDuration.Text = Properties.Strings.TextBenchmarkDuration;

            // numericUpDownDuration
            this.numericUpDownDuration.Location = new System.Drawing.Point(440, 30);
            this.numericUpDownDuration.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            this.numericUpDownDuration.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownDuration.Name = "numericUpDownDuration";
            this.numericUpDownDuration.Size = new System.Drawing.Size(70, 30);
            this.numericUpDownDuration.TabIndex = 12;
            this.numericUpDownDuration.Value = new decimal(new int[] { 2, 0, 0, 0 });

            // labelDurationUnit
            this.labelDurationUnit.AutoSize = true;
            this.labelDurationUnit.Location = new System.Drawing.Point(516, 32);
            this.labelDurationUnit.Name = "labelDurationUnit";
            this.labelDurationUnit.Size = new System.Drawing.Size(40, 24);
            this.labelDurationUnit.TabIndex = 13;
            this.labelDurationUnit.Text = Properties.Strings.TextBenchmarkMinutes;

            // labelRestTime
            this.labelRestTime.AutoSize = true;
            this.labelRestTime.Location = new System.Drawing.Point(610, 32);
            this.labelRestTime.Name = "labelRestTime";
            this.labelRestTime.Size = new System.Drawing.Size(100, 24);
            this.labelRestTime.TabIndex = 16;
            this.labelRestTime.Text = Properties.Strings.TextBenchmarkRestTime;

            // numericUpDownRestTime
            this.numericUpDownRestTime.Location = new System.Drawing.Point(700, 30);
            this.numericUpDownRestTime.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.numericUpDownRestTime.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownRestTime.Name = "numericUpDownRestTime";
            this.numericUpDownRestTime.Size = new System.Drawing.Size(70, 30);
            this.numericUpDownRestTime.TabIndex = 17;
            this.numericUpDownRestTime.Value = new decimal(new int[] { 5, 0, 0, 0 });

            // labelRestSecondsUnit
            this.labelRestSecondsUnit.AutoSize = true;
            this.labelRestSecondsUnit.Location = new System.Drawing.Point(776, 32);
            this.labelRestSecondsUnit.Name = "labelRestSecondsUnit";
            this.labelRestSecondsUnit.Size = new System.Drawing.Size(24, 24);
            this.labelRestSecondsUnit.TabIndex = 18;
            this.labelRestSecondsUnit.Text = Properties.Strings.TextBenchmarkSeconds;

            // === Row 2: 起始功耗  步进  结束功耗  [开始]  [停止] ===

            // labelStartPower
            this.labelStartPower.AutoSize = true;
            this.labelStartPower.Location = new System.Drawing.Point(15, 72);
            this.labelStartPower.Name = "labelStartPower";
            this.labelStartPower.Size = new System.Drawing.Size(100, 24);
            this.labelStartPower.TabIndex = 2;
            this.labelStartPower.Text = Properties.Strings.TextBenchmarkStartPower;

            // numericUpDownStartPower
            this.numericUpDownStartPower.Location = new System.Drawing.Point(100, 70);
            this.numericUpDownStartPower.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownStartPower.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownStartPower.Name = "numericUpDownStartPower";
            this.numericUpDownStartPower.Size = new System.Drawing.Size(70, 30);
            this.numericUpDownStartPower.TabIndex = 3;
            this.numericUpDownStartPower.Value = new decimal(new int[] { 16, 0, 0, 0 });

            // labelStartWatts
            this.labelStartWatts.AutoSize = true;
            this.labelStartWatts.Location = new System.Drawing.Point(176, 72);
            this.labelStartWatts.Name = "labelStartWatts";
            this.labelStartWatts.Size = new System.Drawing.Size(24, 24);
            this.labelStartWatts.TabIndex = 4;
            this.labelStartWatts.Text = Properties.Strings.TextBenchmarkWatts;

            // labelStep
            this.labelStep.AutoSize = true;
            this.labelStep.Location = new System.Drawing.Point(250, 72);
            this.labelStep.Name = "labelStep";
            this.labelStep.Size = new System.Drawing.Size(60, 24);
            this.labelStep.TabIndex = 5;
            this.labelStep.Text = Properties.Strings.TextBenchmarkStep;

            // numericUpDownStep
            this.numericUpDownStep.Location = new System.Drawing.Point(320, 70);
            this.numericUpDownStep.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            this.numericUpDownStep.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownStep.Name = "numericUpDownStep";
            this.numericUpDownStep.Size = new System.Drawing.Size(70, 30);
            this.numericUpDownStep.TabIndex = 6;
            this.numericUpDownStep.Value = new decimal(new int[] { 1, 0, 0, 0 });

            // labelStepWatts
            this.labelStepWatts.AutoSize = true;
            this.labelStepWatts.Location = new System.Drawing.Point(396, 72);
            this.labelStepWatts.Name = "labelStepWatts";
            this.labelStepWatts.Size = new System.Drawing.Size(24, 24);
            this.labelStepWatts.TabIndex = 7;
            this.labelStepWatts.Text = Properties.Strings.TextBenchmarkWatts;

            // labelEndPower
            this.labelEndPower.AutoSize = true;
            this.labelEndPower.Location = new System.Drawing.Point(470, 72);
            this.labelEndPower.Name = "labelEndPower";
            this.labelEndPower.Size = new System.Drawing.Size(100, 24);
            this.labelEndPower.TabIndex = 8;
            this.labelEndPower.Text = Properties.Strings.TextBenchmarkEndPower;

            // numericUpDownEndPower
            this.numericUpDownEndPower.Location = new System.Drawing.Point(570, 70);
            this.numericUpDownEndPower.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericUpDownEndPower.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDownEndPower.Name = "numericUpDownEndPower";
            this.numericUpDownEndPower.Size = new System.Drawing.Size(70, 30);
            this.numericUpDownEndPower.TabIndex = 9;
            this.numericUpDownEndPower.Value = new decimal(new int[] { 45, 0, 0, 0 });

            // labelEndWatts
            this.labelEndWatts.AutoSize = true;
            this.labelEndWatts.Location = new System.Drawing.Point(646, 72);
            this.labelEndWatts.Name = "labelEndWatts";
            this.labelEndWatts.Size = new System.Drawing.Size(24, 24);
            this.labelEndWatts.TabIndex = 10;
            this.labelEndWatts.Text = Properties.Strings.TextBenchmarkWatts;

            // buttonStart
            this.buttonStart.Location = new System.Drawing.Point(740, 66);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(120, 35);
            this.buttonStart.TabIndex = 14;
            this.buttonStart.Text = Properties.Strings.TextBenchmarkStart;
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);

            // buttonStop
            this.buttonStop.Enabled = false;
            this.buttonStop.Location = new System.Drawing.Point(880, 66);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(100, 35);
            this.buttonStop.TabIndex = 15;
            this.buttonStop.Text = Properties.Strings.TextBenchmarkStop;
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);

            // buttonExportCsv
            this.buttonExportCsv = new System.Windows.Forms.Button();
            this.buttonExportCsv.Enabled = false;
            this.buttonExportCsv.Location = new System.Drawing.Point(1000, 66);
            this.buttonExportCsv.Name = "buttonExportCsv";
            this.buttonExportCsv.Size = new System.Drawing.Size(120, 35);
            this.buttonExportCsv.TabIndex = 16;
            this.buttonExportCsv.Text = Properties.Strings.TextBenchmarkExportCsv;
            this.buttonExportCsv.UseVisualStyleBackColor = true;
            this.buttonExportCsv.Click += new System.EventHandler(this.buttonExportCsv_Click);

            //
            // labelStatus
            //
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(12, 120);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(0, 24);
            this.labelStatus.TabIndex = 1;

            //
            // progressBar
            //
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 145);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(1200, 23);
            this.progressBar.TabIndex = 2;
            this.progressBar.Visible = false;

            //
            // dataGridViewResults
            //
            this.dataGridViewResults.AllowUserToAddRows = false;
            this.dataGridViewResults.AllowUserToDeleteRows = false;
            this.dataGridViewResults.AllowUserToOrderColumns = false;
            this.dataGridViewResults.AllowUserToResizeColumns = true;
            this.dataGridViewResults.AllowUserToResizeRows = false;
            this.dataGridViewResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewResults.Location = new System.Drawing.Point(12, 175);
            this.dataGridViewResults.Name = "dataGridViewResults";
            this.dataGridViewResults.ReadOnly = true;
            this.dataGridViewResults.RowHeadersVisible = false;
            this.dataGridViewResults.Size = new System.Drawing.Size(1200, 480);
            this.dataGridViewResults.TabIndex = 3;
            this.dataGridViewResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;

            //
            // BenchmarkForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1224, 670);
            this.Controls.Add(this.groupBoxConfig);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.dataGridViewResults);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.Name = "BenchmarkForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.MinimumSize = new System.Drawing.Size(1000, 400);
            this.Text = Properties.Strings.TextBenchmarkTitle;
            this.Load += new System.EventHandler(this.BenchmarkForm_Load);
            this.groupBoxConfig.ResumeLayout(false);
            this.groupBoxConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartPower)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStep)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownEndPower)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRestTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewResults)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxConfig;
        private System.Windows.Forms.Label labelTestType;
        private System.Windows.Forms.ComboBox comboBoxTestType;
        private System.Windows.Forms.Label labelStartPower;
        private System.Windows.Forms.NumericUpDown numericUpDownStartPower;
        private System.Windows.Forms.Label labelStartWatts;
        private System.Windows.Forms.Label labelStep;
        private System.Windows.Forms.NumericUpDown numericUpDownStep;
        private System.Windows.Forms.Label labelStepWatts;
        private System.Windows.Forms.Label labelEndPower;
        private System.Windows.Forms.NumericUpDown numericUpDownEndPower;
        private System.Windows.Forms.Label labelEndWatts;
        private System.Windows.Forms.Label labelDuration;
        private System.Windows.Forms.NumericUpDown numericUpDownDuration;
        private System.Windows.Forms.Label labelDurationUnit;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonExportCsv;

        private System.Windows.Forms.Label labelRestTime;
        private System.Windows.Forms.NumericUpDown numericUpDownRestTime;
        private System.Windows.Forms.Label labelRestSecondsUnit;

        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.ProgressBar progressBar;

        private System.Windows.Forms.DataGridView dataGridViewResults;
    }
}
