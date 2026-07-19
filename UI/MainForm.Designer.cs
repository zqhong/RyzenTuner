#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RyzenTuner.Utils;

namespace RyzenTuner.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private static System.Drawing.Icon _cachedIcon;

        
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

  private void InitializeComponent()
{
    // 检测设计时环境
    string currentProc = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
    if (currentProc.IndexOf("WindowsFormsDesigner", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
        currentProc.IndexOf("devenv", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
        System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
    {
        this.components = new System.ComponentModel.Container();
        return;
    }

    this.components = new System.ComponentModel.Container();

    // ============================================================
    // 控件实例化
    // ============================================================
    this.panelSidebar = new Panel();
    this.labelAppTitle = new Label();
    this.navHome = new Button();
    this.navSettings = new Button();
    this.navBenchmark = new Button();
    this.navAbout = new Button();
    this.navLogs = new Button();
    this.panelContent = new Panel();
    this.pageHome = new Panel();
    this.groupBoxMode = new GroupBox();
    this.radioButton3 = new RadioButton();
    this.radioButton4 = new RadioButton();
    this.radioButton5 = new RadioButton();
    this.groupBoxStatus = new GroupBox();
    this.labelCpuFreqTitle = new Label();
    this.currentFreqLabel = new Label();
    this.labelCpuPowerTitle = new Label();
    this.currentPowerLabel = new Label();
    this.labelCpuTempTitle = new Label();
    this.currentTempLabel = new Label();
    this.groupBoxParams = new GroupBox();
    this.labelFastLimitTitle = new Label();
    this.fastLimitLabel = new Label();
    this.labelSlowLimitTitle = new Label();
    this.slowLimitLabel = new Label();
    this.labelTctlLimitTitle = new Label();
    this.tctlTempLabel = new Label();
    this.labelStampLimitTitle = new Label();
    this.stampLimitLabel = new Label();
    this.labelApuSkinTitle = new Label();
    this.apuSkinTempLabel = new Label();
    this.groupBoxOptions = new GroupBox();
    this.checkBoxEnergyStar = new CheckBox();
    this.keepAwakeCheckBox = new CheckBox();
    this.launchAtLogonCheckBox = new CheckBox();
    this.cpuBoostCheckBox = new CheckBox();
    this.groupBoxLanguage = new GroupBox();
    this.labelLanguage = new Label();
    this.comboBoxLanguage = new ComboBox();
    this.pageSettings = new Panel();
    this.groupBoxSettingsPowerSave = new GroupBox();
    this.labelPowerSaveMode = new Label();
    this.numericUpDownPowerSaveMode = new NumericUpDown();
    this.labelPowerSaveWatt = new Label();
    this.groupBoxSettingsBalanced = new GroupBox();
    this.labelBalancedMode = new Label();
    this.numericUpDownBalancedMode = new NumericUpDown();
    this.labelBalancedWatt = new Label();
    this.groupBoxSettingsPerformance = new GroupBox();
    this.labelPerformanceMode = new Label();
    this.numericUpDownPerformanceMode = new NumericUpDown();
    this.labelPerformanceWatt = new Label();
    this.groupBoxAdvanced = new GroupBox();
    this.labelTctlTemp = new Label();
    this.numericUpDownTctlTemp = new NumericUpDown();
    this.labelTctlTempUnit = new Label();
    this.labelApuSkinTemp = new Label();
    this.numericUpDownApuSkinTemp = new NumericUpDown();
    this.labelApuSkinTempUnit = new Label();
    this.labelPowerLimitUpdateInterval = new Label();
    this.numericUpDownPowerLimitUpdateInterval = new NumericUpDown();
    this.labelPowerLimitUpdateIntervalUnit = new Label();
    this.buttonSave = new Button();
    this.buttonCancel = new Button();
    this.pageBenchmark = new Panel();
    this.groupBoxBenchmarkConfig = new GroupBox();
    this.labelTestType = new Label();
    this.comboBoxTestType = new ComboBox();
    this.labelDuration = new Label();
    this.numericUpDownDuration = new NumericUpDown();
    this.labelDurationUnit = new Label();
    this.labelRestTime = new Label();
    this.numericUpDownRestTime = new NumericUpDown();
    this.labelRestSecondsUnit = new Label();
    this.labelStartPower = new Label();
    this.numericUpDownStartPower = new NumericUpDown();
    this.labelStartWatts = new Label();
    this.labelStep = new Label();
    this.numericUpDownStep = new NumericUpDown();
    this.labelStepWatts = new Label();
    this.labelEndPower = new Label();
    this.numericUpDownEndPower = new NumericUpDown();
    this.labelEndWatts = new Label();
    this.buttonStart = new Button();
    this.buttonStop = new Button();
    this.buttonExportCsv = new Button();
    this.labelStatus = new Label();
    this.progressBar = new ProgressBar();
    this.dataGridViewResults = new DataGridView();
    this.pageAbout = new Panel();
    this.pageLog = new Panel();
    this.textBoxLogSearch = new TextBox();
    this.buttonLogSearch = new Button();
    this.comboBoxLogLevelFilter = new ComboBox();
    this.dataGridViewLogs = new DataGridView();
    this.buttonRefreshLogs = new Button();
    this.buttonClearLogs = new Button();
    this.labelAboutTitle = new Label();
    this.labelAboutVersion = new Label();
    this.labelAboutCopyright = new Label();
    this.labelAboutBuildTime = new Label();
    this.labelAboutRyzenAdj = new Label();
    this.labelAboutLink = new Label();
    this.notifyIcon1 = new NotifyIcon(this.components);
    this.mainFormTimer = new Timer(this.components);
    this.toolTipPowerLimitUpdateInterval = new System.Windows.Forms.ToolTip(this.components);
    this.groupBoxHotkey = new GroupBox();
    this.labelHotkeyPowerSave = new Label();
    this.textBoxHotkeyPowerSave = new TextBox();
    this.labelHotkeyBalanced = new Label();
    this.textBoxHotkeyBalanced = new TextBox();
    this.labelHotkeyPerformance = new Label();
    this.textBoxHotkeyPerformance = new TextBox();
    this.groupBoxLogSettings = new GroupBox();
    this.labelLogLevel = new Label();
    this.comboBoxLogLevel = new ComboBox();
    this.labelLogSaveDays = new Label();
    this.numericUpDownLogSaveDays = new NumericUpDown();
    this.labelLogSaveDaysUnit = new Label();
    this.buttonOpenLogViewer = new Button();

    // 【重要修正 1】必须挂起所有容器的布局，防止绝对定位计算出错
    this.SuspendLayout();
    this.panelSidebar.SuspendLayout();
    this.panelContent.SuspendLayout();
    this.pageHome.SuspendLayout();
    this.groupBoxMode.SuspendLayout();
    this.groupBoxStatus.SuspendLayout();
    this.groupBoxParams.SuspendLayout();
    this.groupBoxOptions.SuspendLayout();
    this.pageSettings.SuspendLayout();
    this.groupBoxSettingsPowerSave.SuspendLayout();
    this.groupBoxSettingsBalanced.SuspendLayout();
    this.groupBoxSettingsPerformance.SuspendLayout();
    this.groupBoxAdvanced.SuspendLayout();
    this.groupBoxLanguage.SuspendLayout();
    this.groupBoxHotkey.SuspendLayout();
    this.groupBoxLogSettings.SuspendLayout();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLogSaveDays)).BeginInit();
    this.pageBenchmark.SuspendLayout();
    this.pageLog.SuspendLayout();
    ((System.ComponentModel.ISupportInitialize)(this.dataGridViewLogs)).BeginInit();
    this.groupBoxBenchmarkConfig.SuspendLayout();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPowerSaveMode)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBalancedMode)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPerformanceMode)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTctlTemp)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownApuSkinTemp)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPowerLimitUpdateInterval)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDuration)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRestTime)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartPower)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStep)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownEndPower)).BeginInit();
    ((System.ComponentModel.ISupportInitialize)(this.dataGridViewResults)).BeginInit();
    this.pageAbout.SuspendLayout();

    // 动态字体设置
    string[] tryFontArr = { "微软雅黑", "思源黑体", "Arial" };
    foreach (string loopFont in tryFontArr)
    {
        if (CommonUtils.IsFontExists(loopFont))
        {
            this.Font = new Font(loopFont, 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(134)));
            break;
        }
    }

    // ============================================================
    // 侧边栏 panelSidebar
    // ============================================================
    this.panelSidebar.BackColor = Color.FromArgb(234, 234, 234);
    this.panelSidebar.Controls.Add(this.labelAppTitle);
    this.panelSidebar.Controls.Add(this.navHome);
    this.panelSidebar.Controls.Add(this.navSettings);
    this.panelSidebar.Controls.Add(this.navBenchmark);
    this.panelSidebar.Controls.Add(this.navAbout);
    this.panelSidebar.Controls.Add(this.navLogs);
    this.panelSidebar.Dock = DockStyle.Left;
    this.panelSidebar.Location = new Point(0, 0);
    this.panelSidebar.Name = "panelSidebar";
    this.panelSidebar.Size = new Size(200, 580);
    this.panelSidebar.TabIndex = 0;

    this.labelAppTitle.Font = new Font(this.Font.FontFamily, 16F, FontStyle.Bold);
    this.labelAppTitle.ForeColor = Color.Black;
    this.labelAppTitle.Location = new Point(12, 16);
    this.labelAppTitle.Name = "labelAppTitle";
    this.labelAppTitle.Size = new Size(176, 40);
    this.labelAppTitle.Text = "RyzenTuner";
    this.labelAppTitle.TextAlign = ContentAlignment.MiddleLeft;

    int navBtnHeight = 36;
    int[] navBtnY = { 72, 72 + navBtnHeight + 4, 72 + (navBtnHeight + 4) * 2, 72 + (navBtnHeight + 4) * 3, 72 + (navBtnHeight + 4) * 4 };
    string[] navBtnNames = { "navHome", "navSettings", "navBenchmark", "navLogs", "navAbout" };
    string[] navBtnTexts = { Properties.Strings.TextNavHome, Properties.Strings.TextNavSettings, Properties.Strings.TextNavBenchmark, Properties.Strings.TextNavLogs, Properties.Strings.TextNavAbout };
    Button[] navBtns = { this.navHome, this.navSettings, this.navBenchmark, this.navLogs, this.navAbout };

    for (int i = 0; i < 5; i++)
    {
        Button btn = navBtns[i];
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 0, 0, 0);
        btn.Font = new Font(this.Font.FontFamily, 12F);
        btn.Location = new Point(12, navBtnY[i]);
        btn.Name = navBtnNames[i];
        btn.Size = new Size(176, navBtnHeight);
        btn.TabIndex = i + 1;
        btn.Text = navBtnTexts[i];
        btn.TextAlign = ContentAlignment.MiddleLeft;
        btn.UseVisualStyleBackColor = false;
        btn.Click += new EventHandler(this.NavButton_Click);
    }
    this.navHome.BackColor = Color.White;

    // ============================================================
    // 内容区 panelContent
    // ============================================================
    this.panelContent.BackColor = Color.FromArgb(243, 243, 243);
    this.panelContent.Controls.Add(this.pageHome);
    this.panelContent.Controls.Add(this.pageSettings);
    this.panelContent.Controls.Add(this.pageBenchmark);
    this.panelContent.Controls.Add(this.pageAbout);
    this.panelContent.Controls.Add(this.pageLog);
    this.panelContent.Dock = DockStyle.Fill;
    this.panelContent.Location = new Point(200, 0);
    this.panelContent.Name = "panelContent";
    this.panelContent.Size = new Size(660, 580);
    this.panelContent.TabIndex = 1;

    // ============================================================
    // 首页 pageHome
    // ============================================================
    this.pageHome.Controls.Add(this.groupBoxMode);
    this.pageHome.Controls.Add(this.groupBoxStatus);
    this.pageHome.Controls.Add(this.groupBoxParams);
    this.pageHome.Controls.Add(this.groupBoxOptions);
    this.pageHome.Dock = DockStyle.Fill;
    this.pageHome.Location = new Point(0, 0);
    this.pageHome.Name = "pageHome";
    this.pageHome.Padding = new Padding(16);
    this.pageHome.Size = new Size(660, 580);
    this.pageHome.TabIndex = 0;

    this.groupBoxMode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    this.groupBoxMode.Controls.Add(this.radioButton3);
    this.groupBoxMode.Controls.Add(this.radioButton4);
    this.groupBoxMode.Controls.Add(this.radioButton5);
    this.groupBoxMode.Location = new Point(16, 16);
    this.groupBoxMode.Name = "groupBoxMode";
    this.groupBoxMode.Size = new Size(628, 64);
    this.groupBoxMode.Text = Properties.Strings.TextGroupMode;

    this.radioButton3.AutoSize = true;
    this.radioButton3.Location = new Point(24, 28);
    this.radioButton3.Name = "radioButton3";
    this.radioButton3.Text = Properties.Strings.PowerSaveMode;
    this.radioButton3.Tag = "PowerSaveMode";
    this.radioButton3.CheckedChanged += new EventHandler(this.ChangeEnergyMode);

    this.radioButton4.AutoSize = true;
    this.radioButton4.Location = new Point(224, 28);
    this.radioButton4.Name = "radioButton4";
    this.radioButton4.Text = Properties.Strings.BalancedMode;
    this.radioButton4.Tag = "BalancedMode";
    this.radioButton4.CheckedChanged += new EventHandler(this.ChangeEnergyMode);

    this.radioButton5.AutoSize = true;
    this.radioButton5.Checked = true;
    this.radioButton5.Location = new Point(425, 28);
    this.radioButton5.Name = "radioButton5";
    this.radioButton5.Text = Properties.Strings.PerformanceMode;
    this.radioButton5.Tag = "PerformanceMode";
    this.radioButton5.CheckedChanged += new EventHandler(this.ChangeEnergyMode);

    this.groupBoxStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    this.groupBoxStatus.Controls.Add(this.labelCpuFreqTitle);
    this.groupBoxStatus.Controls.Add(this.currentFreqLabel);
    this.groupBoxStatus.Controls.Add(this.labelCpuPowerTitle);
    this.groupBoxStatus.Controls.Add(this.currentPowerLabel);
    this.groupBoxStatus.Controls.Add(this.labelCpuTempTitle);
    this.groupBoxStatus.Controls.Add(this.currentTempLabel);
    this.groupBoxStatus.Location = new Point(16, 92);
    this.groupBoxStatus.Name = "groupBoxStatus";
    this.groupBoxStatus.Size = new Size(628, 88);
    this.groupBoxStatus.Text = Properties.Strings.TextGroupStatus;

    this.labelCpuFreqTitle.AutoSize = true;
    this.labelCpuFreqTitle.ForeColor = Color.Gray;
    this.labelCpuFreqTitle.Location = new Point(16, 24);
    this.labelCpuFreqTitle.Text = Properties.Strings.TextCpuFreqTitle;

    this.currentFreqLabel.AutoSize = true;
    this.currentFreqLabel.Font = new Font(this.Font.FontFamily, 18F, FontStyle.Bold);
    this.currentFreqLabel.Location = new Point(90, 44);
    this.currentFreqLabel.Text = "-- MHz";

    this.labelCpuPowerTitle.AutoSize = true;
    this.labelCpuPowerTitle.ForeColor = Color.Gray;
    this.labelCpuPowerTitle.Location = new Point(240, 24);
    this.labelCpuPowerTitle.Text = Properties.Strings.TextCpuPowerTitle;

    this.currentPowerLabel.AutoSize = true;
    this.currentPowerLabel.Font = new Font(this.Font.FontFamily, 18F, FontStyle.Bold);
    this.currentPowerLabel.Location = new Point(320, 44);
    this.currentPowerLabel.Text = "-- W";

    this.labelCpuTempTitle.AutoSize = true;
    this.labelCpuTempTitle.ForeColor = Color.Gray;
    this.labelCpuTempTitle.Location = new Point(440, 24);
    this.labelCpuTempTitle.Text = Properties.Strings.TextCpuTempTitle;

    this.currentTempLabel.AutoSize = true;
    this.currentTempLabel.Font = new Font(this.Font.FontFamily, 18F, FontStyle.Bold);
    this.currentTempLabel.Location = new Point(520, 44);
    this.currentTempLabel.Text = "-- ℃";

    this.groupBoxParams.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    this.groupBoxParams.Controls.Add(this.labelFastLimitTitle);
    this.groupBoxParams.Controls.Add(this.fastLimitLabel);
    this.groupBoxParams.Controls.Add(this.labelSlowLimitTitle);
    this.groupBoxParams.Controls.Add(this.slowLimitLabel);
    this.groupBoxParams.Controls.Add(this.labelTctlLimitTitle);
    this.groupBoxParams.Controls.Add(this.tctlTempLabel);
    this.groupBoxParams.Controls.Add(this.labelStampLimitTitle);
    this.groupBoxParams.Controls.Add(this.stampLimitLabel);
    this.groupBoxParams.Controls.Add(this.labelApuSkinTitle);
    this.groupBoxParams.Controls.Add(this.apuSkinTempLabel);
    this.groupBoxParams.Location = new Point(16, 192);
    this.groupBoxParams.Name = "groupBoxParams";
    this.groupBoxParams.Size = new Size(628, 140); // 调大以防被裁断
    this.groupBoxParams.Text = Properties.Strings.TextGroupParams;

    this.labelFastLimitTitle.AutoSize = true;
    this.labelFastLimitTitle.ForeColor = Color.Gray;
    this.labelFastLimitTitle.Location = new Point(16, 24);
    this.labelFastLimitTitle.Text = "Fast Limit";

    this.fastLimitLabel.AutoSize = true;
    this.fastLimitLabel.Font = new Font(this.Font.FontFamily, 18F, FontStyle.Bold);
    this.fastLimitLabel.Location = new Point(16, 44);
    this.fastLimitLabel.Text = "-- W";

    this.labelSlowLimitTitle.AutoSize = true;
    this.labelSlowLimitTitle.ForeColor = Color.Gray;
    this.labelSlowLimitTitle.Location = new Point(240, 24);
    this.labelSlowLimitTitle.Text = "Slow Limit";

    this.slowLimitLabel.AutoSize = true;
    this.slowLimitLabel.Font = new Font(this.Font.FontFamily, 18F, FontStyle.Bold);
    this.slowLimitLabel.Location = new Point(240, 44);
    this.slowLimitLabel.Text = "-- W";

    this.labelTctlLimitTitle.AutoSize = true;
    this.labelTctlLimitTitle.ForeColor = Color.Gray;
    this.labelTctlLimitTitle.Location = new Point(440, 24);
    this.labelTctlLimitTitle.Text = Properties.Strings.TextGroupParams_TctlLimit;

    this.tctlTempLabel.AutoSize = true;
    this.tctlTempLabel.Font = new Font(this.Font.FontFamily, 18F, FontStyle.Bold);
    this.tctlTempLabel.Location = new Point(440, 44);
    this.tctlTempLabel.Text = "-- ℃";

    this.labelStampLimitTitle.AutoSize = true;
    this.labelStampLimitTitle.ForeColor = Color.Gray;
    this.labelStampLimitTitle.Location = new Point(16, 80);
    this.labelStampLimitTitle.Text = "STAPM Limit";

    this.stampLimitLabel.AutoSize = true;
    this.stampLimitLabel.Font = new Font(this.Font.FontFamily, 18F, FontStyle.Bold);
    this.stampLimitLabel.Location = new Point(16, 100);
    this.stampLimitLabel.Text = "-- W";

    this.labelApuSkinTitle.AutoSize = true;
    this.labelApuSkinTitle.ForeColor = Color.Gray;
    this.labelApuSkinTitle.Location = new Point(240, 80);
    this.labelApuSkinTitle.Text = "ApuSkin Temp";

    this.apuSkinTempLabel.AutoSize = true;
    this.apuSkinTempLabel.Font = new Font(this.Font.FontFamily, 18F, FontStyle.Bold);
    this.apuSkinTempLabel.Location = new Point(240, 100);
    this.apuSkinTempLabel.Text = "-- ℃";

    this.groupBoxOptions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    this.groupBoxOptions.Controls.Add(this.checkBoxEnergyStar);
    this.groupBoxOptions.Controls.Add(this.keepAwakeCheckBox);
    this.groupBoxOptions.Controls.Add(this.launchAtLogonCheckBox);
    this.groupBoxOptions.Controls.Add(this.cpuBoostCheckBox);
    this.groupBoxOptions.Location = new Point(16, 345); // 向下推一点
    this.groupBoxOptions.Name = "groupBoxOptions";
    this.groupBoxOptions.Size = new Size(628, 100); // 调大高度以防截断
    this.groupBoxOptions.Text = Properties.Strings.TextGroupOptions;

    this.checkBoxEnergyStar.AutoSize = true;
    this.checkBoxEnergyStar.Location = new Point(16, 28);
    this.checkBoxEnergyStar.Name = "checkBoxEnergyStar";
    this.checkBoxEnergyStar.Text = Properties.Strings.TextEnableEnergyStar;
    this.checkBoxEnergyStar.CheckedChanged += new EventHandler(this.checkBoxEnergyStar_CheckedChanged);

    this.keepAwakeCheckBox.AutoSize = true;
    this.keepAwakeCheckBox.Location = new Point(280, 28);
    this.keepAwakeCheckBox.Name = "keepAwakeCheckBox";
    this.keepAwakeCheckBox.Text = Properties.Strings.TextStayAwake;
    this.keepAwakeCheckBox.CheckedChanged += new EventHandler(this.keepAwakeCheckBox_CheckedChanged);

    this.launchAtLogonCheckBox.AutoSize = true;
    this.launchAtLogonCheckBox.Location = new Point(16, 56);
    this.launchAtLogonCheckBox.Name = "launchAtLogonCheckBox";
    this.launchAtLogonCheckBox.Text = Properties.Strings.TextLaunchAtLogon;
    this.launchAtLogonCheckBox.CheckedChanged += new EventHandler(this.launchAtLogonCheckBox_CheckedChanged);

    this.cpuBoostCheckBox.AutoSize = true;
    this.cpuBoostCheckBox.Location = new Point(280, 56);
    this.cpuBoostCheckBox.Name = "cpuBoostCheckBox";
    this.cpuBoostCheckBox.Text = Properties.Strings.TextEnableCpuBoost;
    this.cpuBoostCheckBox.CheckedChanged += new EventHandler(this.cpuBoostCheckBox_CheckedChanged);

    // ============================================================
    // 设置页 pageSettings
    // ============================================================
    this.pageSettings.AutoScroll = true;
    this.pageSettings.Controls.Add(this.groupBoxSettingsPowerSave);
    this.pageSettings.Controls.Add(this.groupBoxSettingsBalanced);
    this.pageSettings.Controls.Add(this.groupBoxSettingsPerformance);
    this.pageSettings.Controls.Add(this.groupBoxLanguage);
    this.pageSettings.Controls.Add(this.groupBoxHotkey);
    this.pageSettings.Controls.Add(this.groupBoxLogSettings);
    this.pageSettings.Controls.Add(this.groupBoxAdvanced);
    this.pageSettings.Controls.Add(this.buttonSave);
    this.pageSettings.Controls.Add(this.buttonCancel);
    this.pageSettings.Dock = DockStyle.Fill;
    this.pageSettings.Location = new Point(0, 0);
    this.pageSettings.Name = "pageSettings";
    this.pageSettings.Padding = new Padding(16);
    this.pageSettings.Size = new Size(660, 580);
    this.pageSettings.Visible = false;

    this.groupBoxSettingsPowerSave.Controls.Add(this.labelPowerSaveMode);
    this.groupBoxSettingsPowerSave.Controls.Add(this.numericUpDownPowerSaveMode);
    this.groupBoxSettingsPowerSave.Controls.Add(this.labelPowerSaveWatt);
    this.groupBoxSettingsPowerSave.Location = new Point(16, 16);
    this.groupBoxSettingsPowerSave.Size = new Size(300, 60);
    this.groupBoxSettingsPowerSave.Text = Properties.Strings.PowerSaveMode;

    this.labelPowerSaveMode.AutoSize = true;
    this.labelPowerSaveMode.Location = new Point(12, 24);
    this.labelPowerSaveMode.Text = Properties.Strings.TextPowerLimit;

    this.numericUpDownPowerSaveMode.Location = new Point(90, 22);
    this.numericUpDownPowerSaveMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
    this.numericUpDownPowerSaveMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
    this.numericUpDownPowerSaveMode.Size = new Size(80, 26);
    this.numericUpDownPowerSaveMode.Value = new decimal(new int[] { 16, 0, 0, 0 });

    this.labelPowerSaveWatt.AutoSize = true;
    this.labelPowerSaveWatt.Location = new Point(176, 24);
    this.labelPowerSaveWatt.Text = "W";

    this.groupBoxSettingsBalanced.Controls.Add(this.labelBalancedMode);
    this.groupBoxSettingsBalanced.Controls.Add(this.numericUpDownBalancedMode);
    this.groupBoxSettingsBalanced.Controls.Add(this.labelBalancedWatt);
    this.groupBoxSettingsBalanced.Location = new Point(340, 16);
    this.groupBoxSettingsBalanced.Size = new Size(300, 60);
    this.groupBoxSettingsBalanced.Text = Properties.Strings.BalancedMode;

    this.labelBalancedMode.AutoSize = true;
    this.labelBalancedMode.Location = new Point(12, 24);
    this.labelBalancedMode.Text = Properties.Strings.TextPowerLimit;

    this.numericUpDownBalancedMode.Location = new Point(90, 22);
    this.numericUpDownBalancedMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
    this.numericUpDownBalancedMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
    this.numericUpDownBalancedMode.Size = new Size(80, 26);
    this.numericUpDownBalancedMode.Value = new decimal(new int[] { 26, 0, 0, 0 });

    this.labelBalancedWatt.AutoSize = true;
    this.labelBalancedWatt.Location = new Point(176, 24);
    this.labelBalancedWatt.Text = "W";

    this.groupBoxSettingsPerformance.Controls.Add(this.labelPerformanceMode);
    this.groupBoxSettingsPerformance.Controls.Add(this.numericUpDownPerformanceMode);
    this.groupBoxSettingsPerformance.Controls.Add(this.labelPerformanceWatt);
    this.groupBoxSettingsPerformance.Location = new Point(16, 88);
    this.groupBoxSettingsPerformance.Size = new Size(300, 60);
    this.groupBoxSettingsPerformance.Text = Properties.Strings.PerformanceMode;

    this.labelPerformanceMode.AutoSize = true;
    this.labelPerformanceMode.Location = new Point(12, 24);
    this.labelPerformanceMode.Text = Properties.Strings.TextPowerLimit;

    this.numericUpDownPerformanceMode.Location = new Point(90, 22);
    this.numericUpDownPerformanceMode.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
    this.numericUpDownPerformanceMode.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
    this.numericUpDownPerformanceMode.Size = new Size(80, 26);
    this.numericUpDownPerformanceMode.Value = new decimal(new int[] { 45, 0, 0, 0 });

    this.labelPerformanceWatt.AutoSize = true;
    this.labelPerformanceWatt.Location = new Point(176, 24);
    this.labelPerformanceWatt.Text = "W";

    this.groupBoxAdvanced.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    this.groupBoxAdvanced.Controls.Add(this.labelTctlTemp);
    this.groupBoxAdvanced.Controls.Add(this.numericUpDownTctlTemp);
    this.groupBoxAdvanced.Controls.Add(this.labelTctlTempUnit);
    this.groupBoxAdvanced.Controls.Add(this.labelApuSkinTemp);
    this.groupBoxAdvanced.Controls.Add(this.numericUpDownApuSkinTemp);
    this.groupBoxAdvanced.Controls.Add(this.labelApuSkinTempUnit);
    this.groupBoxAdvanced.Controls.Add(this.labelPowerLimitUpdateInterval);
    this.groupBoxAdvanced.Controls.Add(this.numericUpDownPowerLimitUpdateInterval);
    this.groupBoxAdvanced.Controls.Add(this.labelPowerLimitUpdateIntervalUnit);
    this.groupBoxAdvanced.Location = new Point(16, 164);
    this.groupBoxAdvanced.Size = new Size(624, 130);
    this.groupBoxAdvanced.Text = Properties.Strings.TextAdvancedSettings;

    this.labelTctlTemp.AutoSize = true;
    this.labelTctlTemp.Location = new Point(16, 28);
    this.labelTctlTemp.Text = Properties.Strings.TextTctlTemp;

    this.numericUpDownTctlTemp.Location = new Point(350, 26);
    this.numericUpDownTctlTemp.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
    this.numericUpDownTctlTemp.Minimum = new decimal(new int[] { 30, 0, 0, 0 });
    this.numericUpDownTctlTemp.Size = new Size(80, 26);
    this.numericUpDownTctlTemp.Value = new decimal(new int[] { 100, 0, 0, 0 });

    this.labelTctlTempUnit.AutoSize = true;
    this.labelTctlTempUnit.Location = new Point(436, 28);
    this.labelTctlTempUnit.Text = Properties.Strings.TextDegreeCelsius;

    this.labelApuSkinTemp.AutoSize = true;
    this.labelApuSkinTemp.Location = new Point(16, 60);
    this.labelApuSkinTemp.Text = Properties.Strings.TextApuSkinTemp;

    this.numericUpDownApuSkinTemp.Location = new Point(350, 58);
    this.numericUpDownApuSkinTemp.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
    this.numericUpDownApuSkinTemp.Minimum = new decimal(new int[] { 30, 0, 0, 0 });
    this.numericUpDownApuSkinTemp.Size = new Size(80, 26);
    this.numericUpDownApuSkinTemp.Value = new decimal(new int[] { 43, 0, 0, 0 });

    this.labelApuSkinTempUnit.AutoSize = true;
    this.labelApuSkinTempUnit.Location = new Point(436, 60);
    this.labelApuSkinTempUnit.Text = Properties.Strings.TextDegreeCelsius;

    // labelPowerLimitUpdateInterval
    this.labelPowerLimitUpdateInterval.AutoSize = true;
    this.labelPowerLimitUpdateInterval.Location = new Point(16, 92);
    this.labelPowerLimitUpdateInterval.Text = Properties.Strings.TextPowerLimitUpdateInterval;

    // numericUpDownPowerLimitUpdateInterval
    this.numericUpDownPowerLimitUpdateInterval.Location = new Point(350, 90);
    this.numericUpDownPowerLimitUpdateInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
    this.numericUpDownPowerLimitUpdateInterval.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
    this.numericUpDownPowerLimitUpdateInterval.Size = new Size(80, 26);
    this.numericUpDownPowerLimitUpdateInterval.Value = new decimal(new int[] { 4, 0, 0, 0 });

    // toolTipPowerLimitUpdateInterval
    this.toolTipPowerLimitUpdateInterval.SetToolTip(this.numericUpDownPowerLimitUpdateInterval, Properties.Strings.TextPowerLimitUpdateIntervalTip);

    // labelPowerLimitUpdateIntervalUnit
    this.labelPowerLimitUpdateIntervalUnit.AutoSize = true;
    this.labelPowerLimitUpdateIntervalUnit.Location = new Point(436, 92);
    this.labelPowerLimitUpdateIntervalUnit.Text = Properties.Strings.TextSecond;

    // ============================================================
    // 语言设置
    // ============================================================
    this.groupBoxLanguage.Controls.Add(this.labelLanguage);
    this.groupBoxLanguage.Controls.Add(this.comboBoxLanguage);
    this.groupBoxLanguage.Location = new Point(16, 386);
    this.groupBoxLanguage.Size = new Size(300, 60);
    this.groupBoxLanguage.Text = Properties.Strings.TextLanguage;

    this.labelLanguage.AutoSize = true;
    this.labelLanguage.Location = new Point(12, 24);
    this.labelLanguage.Text = Properties.Strings.TextLanguage;

    this.comboBoxLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
    this.comboBoxLanguage.Items.AddRange(new object[] {
        new KeyValuePair<string, string>("zh-CN", Properties.Strings.TextLanguageChinese),
        new KeyValuePair<string, string>("en-US", Properties.Strings.TextLanguageEnglish),
    });
    this.comboBoxLanguage.DisplayMember = "Value";
    this.comboBoxLanguage.Location = new Point(90, 22);
    this.comboBoxLanguage.Size = new Size(140, 26);
    // SelectedIndex 由 MainForm.InitLanguageSelection() 运行时设置
    this.comboBoxLanguage.SelectedIndexChanged += new EventHandler(this.ComboBoxLanguage_SelectedIndexChanged);

    // ---- 快捷键设置 ----
    this.groupBoxHotkey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    this.groupBoxHotkey.Controls.Add(this.labelHotkeyPowerSave);
    this.groupBoxHotkey.Controls.Add(this.textBoxHotkeyPowerSave);
    this.groupBoxHotkey.Controls.Add(this.labelHotkeyBalanced);
    this.groupBoxHotkey.Controls.Add(this.textBoxHotkeyBalanced);
    this.groupBoxHotkey.Controls.Add(this.labelHotkeyPerformance);
    this.groupBoxHotkey.Controls.Add(this.textBoxHotkeyPerformance);
    this.groupBoxHotkey.Location = new Point(16, 462);
    this.groupBoxHotkey.Size = new Size(624, 130);
    this.groupBoxHotkey.Text = Properties.Strings.TextHotkeySettings;

    // 省电模式快捷键
    this.labelHotkeyPowerSave.AutoSize = true;
    this.labelHotkeyPowerSave.Location = new Point(12, 28);
    this.labelHotkeyPowerSave.Text = Properties.Strings.TextHotkeyPowerSaveMode;

    this.textBoxHotkeyPowerSave.Location = new Point(160, 26);
    this.textBoxHotkeyPowerSave.Size = new Size(200, 26);
    this.textBoxHotkeyPowerSave.ReadOnly = true;
    this.textBoxHotkeyPowerSave.Text = Properties.Strings.TextHotkeyNone;
    this.textBoxHotkeyPowerSave.Enter += new EventHandler(this.TextBoxHotkey_Enter);
    this.textBoxHotkeyPowerSave.Leave += new EventHandler(this.TextBoxHotkey_Leave);
    this.textBoxHotkeyPowerSave.KeyDown += new KeyEventHandler(this.TextBoxHotkey_KeyDown);

    // 平衡模式快捷键
    this.labelHotkeyBalanced.AutoSize = true;
    this.labelHotkeyBalanced.Location = new Point(12, 60);
    this.labelHotkeyBalanced.Text = Properties.Strings.TextHotkeyBalancedMode;

    this.textBoxHotkeyBalanced.Location = new Point(160, 58);
    this.textBoxHotkeyBalanced.Size = new Size(200, 26);
    this.textBoxHotkeyBalanced.ReadOnly = true;
    this.textBoxHotkeyBalanced.Text = Properties.Strings.TextHotkeyNone;
    this.textBoxHotkeyBalanced.Enter += new EventHandler(this.TextBoxHotkey_Enter);
    this.textBoxHotkeyBalanced.Leave += new EventHandler(this.TextBoxHotkey_Leave);
    this.textBoxHotkeyBalanced.KeyDown += new KeyEventHandler(this.TextBoxHotkey_KeyDown);

    // 性能模式快捷键
    this.labelHotkeyPerformance.AutoSize = true;
    this.labelHotkeyPerformance.Location = new Point(12, 92);
    this.labelHotkeyPerformance.Text = Properties.Strings.TextHotkeyPerformanceMode;

    this.textBoxHotkeyPerformance.Location = new Point(160, 90);
    this.textBoxHotkeyPerformance.Size = new Size(200, 26);
    this.textBoxHotkeyPerformance.ReadOnly = true;
    this.textBoxHotkeyPerformance.Text = Properties.Strings.TextHotkeyNone;
    this.textBoxHotkeyPerformance.Enter += new EventHandler(this.TextBoxHotkey_Enter);
    this.textBoxHotkeyPerformance.Leave += new EventHandler(this.TextBoxHotkey_Leave);
    this.textBoxHotkeyPerformance.KeyDown += new KeyEventHandler(this.TextBoxHotkey_KeyDown);

    // ---- 日志设置 ----
    this.groupBoxLogSettings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    this.groupBoxLogSettings.Controls.Add(this.labelLogLevel);
    this.groupBoxLogSettings.Controls.Add(this.comboBoxLogLevel);
    this.groupBoxLogSettings.Controls.Add(this.labelLogSaveDays);
    this.groupBoxLogSettings.Controls.Add(this.numericUpDownLogSaveDays);
    this.groupBoxLogSettings.Controls.Add(this.labelLogSaveDaysUnit);
    this.groupBoxLogSettings.Controls.Add(this.buttonOpenLogViewer);
    this.groupBoxLogSettings.Location = new Point(16, 310);
    this.groupBoxLogSettings.Size = new Size(624, 60);
    this.groupBoxLogSettings.Text = Properties.Strings.TextLogSettings;

    this.labelLogLevel.AutoSize = true;
    this.labelLogLevel.Location = new Point(12, 28);
    this.labelLogLevel.Text = Properties.Strings.TextLogLevel;

    this.comboBoxLogLevel.DropDownStyle = ComboBoxStyle.DropDownList;
    this.comboBoxLogLevel.Items.AddRange(new object[] {
        "Trace", "Debug", "Info", "Warning", "Error", "Fatal"
    });
    this.comboBoxLogLevel.Location = new Point(80, 26);
    this.comboBoxLogLevel.Size = new Size(120, 26);
    this.comboBoxLogLevel.SelectedItem = "Warning";

    this.labelLogSaveDays.AutoSize = true;
    this.labelLogSaveDays.Location = new Point(230, 28);
    this.labelLogSaveDays.Text = Properties.Strings.TextLogSaveDays;

    this.numericUpDownLogSaveDays.Location = new Point(340, 26);
    this.numericUpDownLogSaveDays.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
    this.numericUpDownLogSaveDays.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
    this.numericUpDownLogSaveDays.Size = new Size(60, 26);
    this.numericUpDownLogSaveDays.Value = new decimal(new int[] { 3, 0, 0, 0 });

    this.labelLogSaveDaysUnit.AutoSize = true;
    this.labelLogSaveDaysUnit.Location = new Point(406, 28);
    this.labelLogSaveDaysUnit.Text = Properties.Strings.TextLogDay;

    this.buttonOpenLogViewer.Location = new Point(480, 24);
    this.buttonOpenLogViewer.Size = new Size(120, 26);
    this.buttonOpenLogViewer.Text = Properties.Strings.TextLogViewLogs;
    this.buttonOpenLogViewer.Click += new EventHandler(this.ButtonOpenLogViewer_Click);

    this.buttonSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
    this.buttonSave.Location = new Point(436, 600);
    this.buttonSave.Size = new Size(100, 35);
    this.buttonSave.Text = Properties.Strings.TextSave;
    this.buttonSave.Click += new EventHandler(this.SettingsSave_Click);

    this.buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
    this.buttonCancel.Location = new Point(544, 600);
    this.buttonCancel.Size = new Size(100, 35);
    this.buttonCancel.Text = Properties.Strings.TextCancel;
    this.buttonCancel.Click += new EventHandler(this.SettingsCancel_Click);

    // ============================================================
    // 跑分页 pageBenchmark
    // ============================================================
    this.pageBenchmark.Controls.Add(this.groupBoxBenchmarkConfig);
    this.pageBenchmark.Controls.Add(this.labelStatus);
    this.pageBenchmark.Controls.Add(this.progressBar);
    this.pageBenchmark.Controls.Add(this.dataGridViewResults);
    this.pageBenchmark.Dock = DockStyle.Fill;
    this.pageBenchmark.Location = new Point(0, 0);
    this.pageBenchmark.Name = "pageBenchmark";
    this.pageBenchmark.Padding = new Padding(16);
    this.pageBenchmark.Size = new Size(660, 580);
    this.pageBenchmark.Visible = false;

    this.groupBoxBenchmarkConfig.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelTestType);
    this.groupBoxBenchmarkConfig.Controls.Add(this.comboBoxTestType);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelDuration);
    this.groupBoxBenchmarkConfig.Controls.Add(this.numericUpDownDuration);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelDurationUnit);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelRestTime);
    this.groupBoxBenchmarkConfig.Controls.Add(this.numericUpDownRestTime);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelRestSecondsUnit);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelStartPower);
    this.groupBoxBenchmarkConfig.Controls.Add(this.numericUpDownStartPower);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelStartWatts);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelStep);
    this.groupBoxBenchmarkConfig.Controls.Add(this.numericUpDownStep);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelStepWatts);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelEndPower);
    this.groupBoxBenchmarkConfig.Controls.Add(this.numericUpDownEndPower);
    this.groupBoxBenchmarkConfig.Controls.Add(this.labelEndWatts);
    this.groupBoxBenchmarkConfig.Controls.Add(this.buttonStart);
    this.groupBoxBenchmarkConfig.Controls.Add(this.buttonStop);
    this.groupBoxBenchmarkConfig.Controls.Add(this.buttonExportCsv);
    this.groupBoxBenchmarkConfig.Location = new Point(16, 16);
    this.groupBoxBenchmarkConfig.Name = "groupBoxBenchmarkConfig";
    this.groupBoxBenchmarkConfig.Size = new Size(628, 125); // 调大高度以防按钮超出
    this.groupBoxBenchmarkConfig.Text = Properties.Strings.TextBenchmark;

    this.labelTestType.AutoSize = true;
    this.labelTestType.Location = new Point(12, 24);
    this.labelTestType.Text = Properties.Strings.TextBenchmarkTestType;

    this.comboBoxTestType.DropDownStyle = ComboBoxStyle.DropDownList;
    this.comboBoxTestType.Items.AddRange(new object[] {
        Properties.Strings.TextBenchmarkSingleCore,
        Properties.Strings.TextBenchmarkMultiCore
    });
    this.comboBoxTestType.Location = new Point(80, 22);
    this.comboBoxTestType.Size = new Size(140, 26);
    this.comboBoxTestType.SelectedIndex = 0;

    this.labelDuration.AutoSize = true;
    this.labelDuration.Location = new Point(260, 24);
    this.labelDuration.Text = Properties.Strings.TextBenchmarkDuration;

    this.numericUpDownDuration.Location = new Point(330, 22);
    this.numericUpDownDuration.Size = new Size(60, 26);
    this.numericUpDownDuration.Value = new decimal(new int[] { 2, 0, 0, 0 });

    this.labelDurationUnit.AutoSize = true;
    this.labelDurationUnit.Location = new Point(396, 24);
    this.labelDurationUnit.Text = Properties.Strings.TextBenchmarkMinutes;

    this.labelRestTime.AutoSize = true;
    this.labelRestTime.Location = new Point(460, 24);
    this.labelRestTime.Text = Properties.Strings.TextBenchmarkRestTime;

    this.numericUpDownRestTime.Location = new Point(528, 22);
    this.numericUpDownRestTime.Size = new Size(60, 26);
    this.numericUpDownRestTime.Value = new decimal(new int[] { 5, 0, 0, 0 });

    this.labelRestSecondsUnit.AutoSize = true;
    this.labelRestSecondsUnit.Location = new Point(594, 24);
    this.labelRestSecondsUnit.Text = Properties.Strings.TextBenchmarkSeconds;

    this.labelStartPower.AutoSize = true;
    this.labelStartPower.Location = new Point(12, 58);
    this.labelStartPower.Text = Properties.Strings.TextBenchmarkStartPower;

    this.numericUpDownStartPower.Location = new Point(80, 56);
    this.numericUpDownStartPower.Size = new Size(60, 26);
    this.numericUpDownStartPower.Value = new decimal(new int[] { 16, 0, 0, 0 });

    this.labelStartWatts.AutoSize = true;
    this.labelStartWatts.Location = new Point(146, 58);
    this.labelStartWatts.Text = Properties.Strings.TextBenchmarkWatts;

    this.labelStep.AutoSize = true;
    this.labelStep.Location = new Point(190, 58);
    this.labelStep.Text = Properties.Strings.TextBenchmarkStep;

    this.numericUpDownStep.Location = new Point(240, 56);
    this.numericUpDownStep.Size = new Size(60, 26);
    this.numericUpDownStep.Value = new decimal(new int[] { 1, 0, 0, 0 });

    this.labelStepWatts.AutoSize = true;
    this.labelStepWatts.Location = new Point(306, 58);
    this.labelStepWatts.Text = Properties.Strings.TextBenchmarkWatts;

    this.labelEndPower.AutoSize = true;
    this.labelEndPower.Location = new Point(350, 58);
    this.labelEndPower.Text = Properties.Strings.TextBenchmarkEndPower;

    this.numericUpDownEndPower.Location = new Point(418, 56);
    this.numericUpDownEndPower.Size = new Size(60, 26);
    this.numericUpDownEndPower.Value = new decimal(new int[] { 45, 0, 0, 0 });

    this.labelEndWatts.AutoSize = true;
    this.labelEndWatts.Location = new Point(484, 58);
    this.labelEndWatts.Text = Properties.Strings.TextBenchmarkWatts;

    this.buttonStart.Location = new Point(12, 88);
    this.buttonStart.Size = new Size(100, 26);
    this.buttonStart.Text = Properties.Strings.TextBenchmarkStart;
    this.buttonStart.Click += new EventHandler(this.BenchmarkStart_Click);

    this.buttonStop.Enabled = false;
    this.buttonStop.Location = new Point(120, 88);
    this.buttonStop.Size = new Size(80, 26);
    this.buttonStop.Text = Properties.Strings.TextBenchmarkStop;
    this.buttonStop.Click += new EventHandler(this.BenchmarkStop_Click);

    this.buttonExportCsv.Enabled = false;
    this.buttonExportCsv.Location = new Point(210, 88);
    this.buttonExportCsv.Size = new Size(100, 26);
    this.buttonExportCsv.Text = Properties.Strings.TextBenchmarkExportCsv;
    this.buttonExportCsv.Click += new EventHandler(this.BenchmarkExportCsv_Click);

    // 向下推，防止上层 GroupBox 变大后重叠
    this.labelStatus.AutoSize = true;
    this.labelStatus.Location = new Point(16, 145);
    this.labelStatus.Name = "labelStatus";
    this.labelStatus.Size = new Size(0, 18);

    this.progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
    this.progressBar.Location = new Point(16, 165);
    this.progressBar.Name = "progressBar";
    this.progressBar.Size = new Size(628, 20);
    this.progressBar.Visible = false;

    this.dataGridViewResults.AllowUserToAddRows = false;
    this.dataGridViewResults.AllowUserToDeleteRows = false;
    this.dataGridViewResults.AllowUserToResizeRows = false;
    this.dataGridViewResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    this.dataGridViewResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    this.dataGridViewResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    this.dataGridViewResults.Location = new Point(16, 195);
    this.dataGridViewResults.Name = "dataGridViewResults";
    this.dataGridViewResults.ReadOnly = true;
    this.dataGridViewResults.RowHeadersVisible = false;
    this.dataGridViewResults.Size = new Size(628, 365);

    // ============================================================
    // 关于页 pageAbout
    // ============================================================
    this.pageAbout.Controls.Add(this.labelAboutTitle);
    this.pageAbout.Controls.Add(this.labelAboutVersion);
    this.pageAbout.Controls.Add(this.labelAboutCopyright);
    this.pageAbout.Controls.Add(this.labelAboutBuildTime);
    this.pageAbout.Controls.Add(this.labelAboutRyzenAdj);
    this.pageAbout.Controls.Add(this.labelAboutLink);
    this.pageAbout.Dock = DockStyle.Fill;
    this.pageAbout.Location = new Point(0, 0);
    this.pageAbout.Name = "pageAbout";
    this.pageAbout.Padding = new Padding(40);
    this.pageAbout.Size = new Size(660, 580);
    this.pageAbout.Visible = false;

    this.labelAboutTitle.AutoSize = true;
    this.labelAboutTitle.Font = new Font(this.Font.FontFamily, 20F, FontStyle.Bold);
    this.labelAboutTitle.Location = new Point(40, 40);
    this.labelAboutTitle.Text = "RyzenTuner";

    this.labelAboutVersion.AutoSize = true;
    this.labelAboutVersion.ForeColor = Color.Gray;
    this.labelAboutVersion.Location = new Point(40, 88);
    this.labelAboutVersion.Text = "Version --";

    this.labelAboutCopyright.AutoSize = true;
    this.labelAboutCopyright.Location = new Point(40, 124);
    this.labelAboutCopyright.Text = "Copyright ...";

    this.labelAboutBuildTime.AutoSize = true;
    this.labelAboutBuildTime.ForeColor = Color.Gray;
    this.labelAboutBuildTime.Location = new Point(40, 160);
    this.labelAboutBuildTime.Text = "Build time ...";

    this.labelAboutRyzenAdj.AutoSize = true;
    this.labelAboutRyzenAdj.Location = new Point(40, 196);
    this.labelAboutRyzenAdj.Text = "ryzenadj ...";

    this.labelAboutLink.AutoSize = true;
    this.labelAboutLink.Cursor = Cursors.Hand;
    this.labelAboutLink.ForeColor = Color.FromArgb(0, 95, 184);
    this.labelAboutLink.Location = new Point(40, 232);
    this.labelAboutLink.Text = "https://github.com/zqhong/RyzenTuner";
    this.labelAboutLink.Click += new EventHandler(this.LabelAboutLink_Click);

    // ============================================================
    // 日志查看页 pageLog
    // ============================================================
    this.pageLog.Controls.Add(this.textBoxLogSearch);
    this.pageLog.Controls.Add(this.buttonLogSearch);
    this.pageLog.Controls.Add(this.comboBoxLogLevelFilter);
    this.pageLog.Controls.Add(this.dataGridViewLogs);
    this.pageLog.Controls.Add(this.buttonRefreshLogs);
    this.pageLog.Controls.Add(this.buttonClearLogs);
    this.pageLog.Dock = DockStyle.Fill;
    this.pageLog.Location = new Point(0, 0);
    this.pageLog.Name = "pageLog";
    this.pageLog.Padding = new Padding(16);
    this.pageLog.Size = new Size(660, 580);
    this.pageLog.Visible = false;

    // 搜索栏
    this.textBoxLogSearch.Location = new Point(16, 16);
    this.textBoxLogSearch.Size = new Size(420, 26);
    this.textBoxLogSearch.Text = Properties.Strings.TextLogSearchPlaceholder;
    this.textBoxLogSearch.Enter += new EventHandler(this.TextBoxLogSearch_Enter);
    this.textBoxLogSearch.Leave += new EventHandler(this.TextBoxLogSearch_Leave);
    this.textBoxLogSearch.KeyPress += new KeyPressEventHandler(this.TextBoxLogSearch_KeyPress);

    this.buttonLogSearch.Location = new Point(442, 16);
    this.buttonLogSearch.Size = new Size(80, 26);
    this.buttonLogSearch.Text = Properties.Strings.TextLogSearchButton;
    this.buttonLogSearch.Click += new EventHandler(this.ButtonLogSearch_Click);

    this.comboBoxLogLevelFilter.DropDownStyle = ComboBoxStyle.DropDownList;
    this.comboBoxLogLevelFilter.Items.AddRange(new object[] {
        Properties.Strings.TextLogLevelAll,
        "Trace", "Debug", "Info", "Warning", "Error", "Fatal"
    });
    this.comboBoxLogLevelFilter.Location = new Point(16, 52);
    this.comboBoxLogLevelFilter.Size = new Size(120, 26);
    this.comboBoxLogLevelFilter.SelectedIndex = 0;
    this.comboBoxLogLevelFilter.SelectedIndexChanged += new EventHandler(this.ComboBoxLogLevelFilter_SelectedIndexChanged);

    this.buttonRefreshLogs.Location = new Point(150, 52);
    this.buttonRefreshLogs.Size = new Size(80, 26);
    this.buttonRefreshLogs.Text = Properties.Strings.TextLogRefresh;
    this.buttonRefreshLogs.Click += new EventHandler(this.ButtonRefreshLogs_Click);

    this.buttonClearLogs.Location = new Point(240, 52);
    this.buttonClearLogs.Size = new Size(120, 26);
    this.buttonClearLogs.Text = Properties.Strings.TextLogClear;
    this.buttonClearLogs.Click += new EventHandler(this.ButtonClearLogs_Click);

    this.dataGridViewLogs.AllowUserToAddRows = false;
    this.dataGridViewLogs.AllowUserToDeleteRows = false;
    this.dataGridViewLogs.AllowUserToResizeRows = false;
    this.dataGridViewLogs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
    this.dataGridViewLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    this.dataGridViewLogs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    this.dataGridViewLogs.Location = new Point(16, 92);
    this.dataGridViewLogs.Name = "dataGridViewLogs";
    this.dataGridViewLogs.ReadOnly = true;
    this.dataGridViewLogs.RowHeadersVisible = false;
    this.dataGridViewLogs.Size = new Size(628, 468);

    // ============================================================
    // 系统托盘及计时器
    // ============================================================
    this.contextMenuStripIcon = new ContextMenuStrip(this.components);
    this.toolStripMenuItemExit = new ToolStripMenuItem();
    this.contextMenuStripIcon.Items.Add(this.toolStripMenuItemExit);
    this.toolStripMenuItemExit.Text = Properties.Strings.TextExit;
    this.toolStripMenuItemExit.Click += new EventHandler(this.ExitAppToolStripMenuItem_Click);

    this.notifyIcon1.ContextMenuStrip = this.contextMenuStripIcon;
    this.notifyIcon1.Icon = GetIcon();
    this.notifyIcon1.Visible = true;
    this.notifyIcon1.MouseDoubleClick += new MouseEventHandler(this.notifyIcon1_MouseDoubleClick);

    this.mainFormTimer.Interval = 2048;
    this.mainFormTimer.Tick += new EventHandler(this.mainFormTimer_Tick);

    // ============================================================
    // Form 主窗体属性及最后恢复布局
    // ============================================================
    this.Controls.Add(this.panelContent);
    this.Controls.Add(this.panelSidebar); // Dock 顺序，建议把 Sidebar 放在最后添加以抢占 Left

    // 【重要修正 2】修复 DPI 缩放问题
    this.AutoScaleDimensions = new SizeF(96F, 96F);
    this.AutoScaleMode = AutoScaleMode.Dpi; // 使用 DPI 缩放代替 Font 缩放能更好地适配高分屏
    this.ClientSize = new Size(860, 580);
    this.Icon = GetIcon();
    this.MinimumSize = new Size(760, 510);
    this.Name = "MainForm";
    
    string softwareVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
    int lastDot = softwareVersion.LastIndexOf('.');
    if (lastDot > 0)
        softwareVersion = softwareVersion.Substring(0, lastDot);
    this.Text = "RyzenTuner " + softwareVersion;

    this.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);
    this.Load += new EventHandler(this.Form1_Load);
    this.Resize += new EventHandler(this.Form1_Resize);
    this.Shown += new EventHandler(this.Form1_Shown);

    // 【重要修正 1 补充】恢复所有的布局控制
    this.panelSidebar.ResumeLayout(false);
    this.panelContent.ResumeLayout(false);
    this.pageHome.ResumeLayout(false);
    this.pageHome.PerformLayout();
    this.groupBoxMode.ResumeLayout(false);
    this.groupBoxMode.PerformLayout();
    this.groupBoxStatus.ResumeLayout(false);
    this.groupBoxStatus.PerformLayout();
    this.groupBoxParams.ResumeLayout(false);
    this.groupBoxParams.PerformLayout();
    this.groupBoxOptions.ResumeLayout(false);
    this.groupBoxOptions.PerformLayout();
    this.pageSettings.ResumeLayout(false);
    this.groupBoxSettingsPowerSave.ResumeLayout(false);
    this.groupBoxSettingsPowerSave.PerformLayout();
    this.groupBoxSettingsBalanced.ResumeLayout(false);
    this.groupBoxSettingsBalanced.PerformLayout();
    this.groupBoxSettingsPerformance.ResumeLayout(false);
    this.groupBoxSettingsPerformance.PerformLayout();
    this.groupBoxAdvanced.ResumeLayout(false);
    this.groupBoxAdvanced.PerformLayout();
    this.groupBoxLanguage.ResumeLayout(false);
    this.groupBoxLanguage.PerformLayout();
    this.groupBoxHotkey.ResumeLayout(false);
    this.groupBoxHotkey.PerformLayout();
    this.pageBenchmark.ResumeLayout(false);
    this.pageBenchmark.PerformLayout();
    this.groupBoxBenchmarkConfig.ResumeLayout(false);
    this.groupBoxBenchmarkConfig.PerformLayout();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPowerSaveMode)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBalancedMode)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPerformanceMode)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTctlTemp)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownApuSkinTemp)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPowerLimitUpdateInterval)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDuration)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRestTime)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartPower)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStep)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.numericUpDownEndPower)).EndInit();
    ((System.ComponentModel.ISupportInitialize)(this.dataGridViewResults)).EndInit();
    this.pageAbout.ResumeLayout(false);
    this.pageAbout.PerformLayout();
    this.ResumeLayout(false);
}
        // ============================================================
        // 控件字段声明
        // ============================================================

        // ---- 侧边栏 ----
        private Panel panelSidebar;
        private Label labelAppTitle;
        private Button navHome;
        private Button navSettings;
        private Button navBenchmark;
        private Button navAbout;
        private Button navLogs;

        // ---- 内容区 ----
        private Panel panelContent;
        private Panel pageHome;
        private Panel pageSettings;
        private Panel pageBenchmark;
        private Panel pageAbout;
        private Panel pageLog;

        // ---- 首页 ----
        private GroupBox groupBoxMode;
        private RadioButton radioButton3;
        private RadioButton radioButton4;
        private RadioButton radioButton5;
        private GroupBox groupBoxStatus;
        private Label labelCpuFreqTitle;
        private Label currentFreqLabel;
        private Label labelCpuPowerTitle;
        private Label currentPowerLabel;
        private Label labelCpuTempTitle;
        private Label currentTempLabel;
        private GroupBox groupBoxParams;
        private Label labelFastLimitTitle;
        private Label fastLimitLabel;
        private Label labelSlowLimitTitle;
        private Label slowLimitLabel;
        private Label labelTctlLimitTitle;
        private Label tctlTempLabel;
        private Label labelStampLimitTitle;
        private Label stampLimitLabel;
        private Label labelApuSkinTitle;
        private Label apuSkinTempLabel;
        private GroupBox groupBoxOptions;
        private CheckBox checkBoxEnergyStar;
        private CheckBox keepAwakeCheckBox;
        private CheckBox launchAtLogonCheckBox;
        private CheckBox cpuBoostCheckBox;

        // ---- 设置页 ----
        private GroupBox groupBoxSettingsPowerSave;
        private Label labelPowerSaveMode;
        private NumericUpDown numericUpDownPowerSaveMode;
        private Label labelPowerSaveWatt;
        private GroupBox groupBoxSettingsBalanced;
        private Label labelBalancedMode;
        private NumericUpDown numericUpDownBalancedMode;
        private Label labelBalancedWatt;
        private GroupBox groupBoxSettingsPerformance;
        private Label labelPerformanceMode;
        private NumericUpDown numericUpDownPerformanceMode;
        private Label labelPerformanceWatt;
        private GroupBox groupBoxAdvanced;
        private Label labelTctlTemp;
        private NumericUpDown numericUpDownTctlTemp;
        private Label labelTctlTempUnit;
        private Label labelApuSkinTemp;
        private NumericUpDown numericUpDownApuSkinTemp;
        private Label labelApuSkinTempUnit;
        private Label labelPowerLimitUpdateInterval;
        private NumericUpDown numericUpDownPowerLimitUpdateInterval;
        private Label labelPowerLimitUpdateIntervalUnit;
        private ToolTip toolTipPowerLimitUpdateInterval;
        private Button buttonSave;
        private Button buttonCancel;

        // ---- 语言设置 ----
        private GroupBox groupBoxLanguage;
        private Label labelLanguage;
        private ComboBox comboBoxLanguage;

        // ---- 快捷键设置 ----
        private GroupBox groupBoxHotkey;
        private Label labelHotkeyPowerSave;
        private TextBox textBoxHotkeyPowerSave;
        private Label labelHotkeyBalanced;
        private TextBox textBoxHotkeyBalanced;
        private Label labelHotkeyPerformance;
        private TextBox textBoxHotkeyPerformance;

        // ---- 跑分页 ----
        private GroupBox groupBoxBenchmarkConfig;
        private Label labelTestType;
        private ComboBox comboBoxTestType;
        private Label labelDuration;
        private NumericUpDown numericUpDownDuration;
        private Label labelDurationUnit;
        private Label labelRestTime;
        private NumericUpDown numericUpDownRestTime;
        private Label labelRestSecondsUnit;
        private Label labelStartPower;
        private NumericUpDown numericUpDownStartPower;
        private Label labelStartWatts;
        private Label labelStep;
        private NumericUpDown numericUpDownStep;
        private Label labelStepWatts;
        private Label labelEndPower;
        private NumericUpDown numericUpDownEndPower;
        private Label labelEndWatts;
        private Button buttonStart;
        private Button buttonStop;
        private Button buttonExportCsv;
        private Label labelStatus;
        private ProgressBar progressBar;
        private DataGridView dataGridViewResults;

        // ---- 关于页 ----
        private Label labelAboutTitle;
        private Label labelAboutVersion;
        private Label labelAboutCopyright;
        private Label labelAboutBuildTime;
        private Label labelAboutRyzenAdj;
        private Label labelAboutLink;

        // ---- 日志设置 ----
        private GroupBox groupBoxLogSettings;
        private Label labelLogLevel;
        private ComboBox comboBoxLogLevel;
        private Label labelLogSaveDays;
        private NumericUpDown numericUpDownLogSaveDays;
        private Label labelLogSaveDaysUnit;
        private Button buttonOpenLogViewer;

        // ---- 日志查看页 ----
        private ComboBox comboBoxLogLevelFilter;
        private DataGridView dataGridViewLogs;
        private Button buttonRefreshLogs;
        private Button buttonClearLogs;
        private TextBox textBoxLogSearch;
        private Button buttonLogSearch;

        // ---- 基础组件 ----
        private NotifyIcon notifyIcon1;
        private ContextMenuStrip contextMenuStripIcon;
        private ToolStripMenuItem toolStripMenuItemExit;
        private Timer mainFormTimer;

    }
}

