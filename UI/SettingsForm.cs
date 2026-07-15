using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using RyzenTuner.Properties;
using RyzenTuner.Utils;

namespace RyzenTuner.UI
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            // 与 MainForm 保持一致的字体，避免标签宽度不一致导致布局错位
            string[] tryFontArr =
            {
                "微软雅黑",
                "思源黑体",
                "Arial",
            };
            foreach (var loopFont in tryFontArr)
            {
                if (CommonUtils.IsFontExists(loopFont))
                {
                    Font = new Font(loopFont, 10);
                    break;
                }
            }

            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 各模式功率限制
            TrySetNumericValue(numericUpDownSleepMode, Settings.Default.SleepMode);
            TrySetNumericValue(numericUpDownPowerSaveMode, Settings.Default.PowerSaveMode);
            TrySetNumericValue(numericUpDownBalancedMode, Settings.Default.BalancedMode);
            TrySetNumericValue(numericUpDownPerformanceMode, Settings.Default.PerformanceMode);

            // 高级设置
            numericUpDownTctlTemp.Value = ClampNumeric(Settings.Default.TctlTemp, numericUpDownTctlTemp);
            numericUpDownApuSkinTemp.Value = ClampNumeric(Settings.Default.ApuSkinTemp, numericUpDownApuSkinTemp);
        }

        private static void TrySetNumericValue(NumericUpDown control, string value)
        {
            // 使用双重区域解析：优先当前区域，再回退到不变区域
            // 防止跨区域设置迁移时数据静默丢失（与 MainForm.TryGetValidatedCustomMode 保持一致）
            var isValid = float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var result)
                          || float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

            if (isValid)
            {
                control.Value = ClampNumeric((decimal)result, control);
            }
        }

        private static decimal ClampNumeric(decimal value, NumericUpDown control)
        {
            if (value < control.Minimum) return control.Minimum;
            if (value > control.Maximum) return control.Maximum;
            return value;
        }

        private static int ClampNumeric(int value, NumericUpDown control)
        {
            if (value < (int)control.Minimum) return (int)control.Minimum;
            if (value > (int)control.Maximum) return (int)control.Maximum;
            return value;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            // 保存各模式功率限制（存储为字符串，兼容浮点数）
            // 使用不变区域格式化，确保配置文件跨区域可移植
            Settings.Default.SleepMode = numericUpDownSleepMode.Value.ToString("F0", CultureInfo.InvariantCulture);
            Settings.Default.PowerSaveMode = numericUpDownPowerSaveMode.Value.ToString("F0", CultureInfo.InvariantCulture);
            Settings.Default.BalancedMode = numericUpDownBalancedMode.Value.ToString("F0", CultureInfo.InvariantCulture);
            Settings.Default.PerformanceMode = numericUpDownPerformanceMode.Value.ToString("F0", CultureInfo.InvariantCulture);

            // 保存高级设置
            Settings.Default.TctlTemp = (int)numericUpDownTctlTemp.Value;
            Settings.Default.ApuSkinTemp = (int)numericUpDownApuSkinTemp.Value;

            Settings.Default.Save();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            Text = Properties.Strings.TextSettingsTitle;
        }
    }
}
