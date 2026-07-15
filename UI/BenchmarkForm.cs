using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RyzenTuner.Common.Benchmark;
using RyzenTuner.Common.Container;
using RyzenTuner.Properties;

namespace RyzenTuner.UI
{
    public partial class BenchmarkForm : Form
    {
        private BenchmarkEngine? _engine;
        private readonly List<BenchmarkTestPoint> _allResults = new();

        public BenchmarkForm()
        {
            // 与 MainForm 保持一致的字体
            string[] tryFontArr =
            {
                "微软雅黑",
                "思源黑体",
                "Arial",
            };
            foreach (var loopFont in tryFontArr)
            {
                if (Utils.CommonUtils.IsFontExists(loopFont))
                {
                    Font = new Font(loopFont, 10);
                    break;
                }
            }

            InitializeComponent();
            SetupDataGridView();
        }

        private void SetupDataGridView()
        {
            // 清空默认列
            dataGridViewResults.Columns.Clear();

            // 定义 13 列
            var columns = new[]
            {
                Properties.Strings.TextBenchmarkSetPower,
                Properties.Strings.TextBenchmarkScore,
                Properties.Strings.TextBenchmarkPowerMin,
                Properties.Strings.TextBenchmarkPowerMax,
                Properties.Strings.TextBenchmarkPowerAvg,
                Properties.Strings.TextBenchmarkPowerMid,
                Properties.Strings.TextBenchmarkTempMin,
                Properties.Strings.TextBenchmarkTempMax,
                Properties.Strings.TextBenchmarkTempAvg,
                Properties.Strings.TextBenchmarkTempMid,
                Properties.Strings.TextBenchmarkFreq,
                Properties.Strings.TextBenchmarkEfficiency,
                Properties.Strings.TextBenchmarkCapability,
            };

            foreach (var header in columns)
            {
                dataGridViewResults.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = header,
                    ReadOnly = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                });
            }

            // 调整列宽
            if (dataGridViewResults.Columns.Count >= 13)
            {
                dataGridViewResults.Columns[0].Width = 80;  // TDP
                dataGridViewResults.Columns[1].Width = 100; // Score
                dataGridViewResults.Columns[2].Width = 90;  // Pmin
                dataGridViewResults.Columns[3].Width = 90;  // Pmax
                dataGridViewResults.Columns[4].Width = 90;  // Pavg
                dataGridViewResults.Columns[5].Width = 90;  // Pmid
                dataGridViewResults.Columns[6].Width = 90;  // Tmin
                dataGridViewResults.Columns[7].Width = 90;  // Tmax
                dataGridViewResults.Columns[8].Width = 90;  // Tavg
                dataGridViewResults.Columns[9].Width = 90;  // Tmid
                dataGridViewResults.Columns[10].Width = 90; // Freq
                dataGridViewResults.Columns[11].Width = 90; // Efficiency
                dataGridViewResults.Columns[12].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // Capability
            }
        }

        private void BenchmarkForm_Load(object sender, EventArgs e)
        {
            Text = Properties.Strings.TextBenchmarkTitle;
            EnableConfigMode(true);
        }

        private void EnableConfigMode(bool enabled)
        {
            comboBoxTestType.Enabled = enabled;
            numericUpDownStartPower.Enabled = enabled;
            numericUpDownStep.Enabled = enabled;
            numericUpDownEndPower.Enabled = enabled;
            numericUpDownDuration.Enabled = enabled;
            buttonStart.Enabled = enabled;
            buttonStop.Enabled = !enabled;
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            // 构建配置
            var config = new BenchmarkConfig
            {
                TestType = comboBoxTestType.SelectedIndex == 0
                    ? BenchmarkTestType.SingleCore
                    : BenchmarkTestType.MultiCore,
                StartTdp = (float)numericUpDownStartPower.Value,
                StepTdp = (float)numericUpDownStep.Value,
                EndTdp = (float)numericUpDownEndPower.Value,
                DurationSeconds = (int)numericUpDownDuration.Value * 60,
                RestSeconds = (int)numericUpDownRestTime.Value,
            };

            // 验证配置
            if (config.StartTdp > config.EndTdp)
            {
                MessageBox.Show(
                    Properties.Strings.TextBenchmarkErrorNoData,
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var pointCount = config.GetTestPointCount();
            var totalMinutes = pointCount * (int)numericUpDownDuration.Value;

            // 确认对话框
            var confirmMsg = Properties.Strings.TextBenchmarkConfirmStart
                .Replace("{count}", pointCount.ToString())
                .Replace("{time}", totalMinutes.ToString());

            if (MessageBox.Show(confirmMsg,
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            // 重置结果
            _allResults.Clear();
            dataGridViewResults.Rows.Clear();
            progressBar.Visible = true;
            progressBar.Maximum = pointCount;
            progressBar.Value = 0;

            EnableConfigMode(false);
            labelStatus.Text = Properties.Strings.TextBenchmarkRunning;

            // 创建引擎并绑定事件
            using (_engine = new BenchmarkEngine())
            {
                _engine.OnProgressChanged += (current, total) =>
                {
                    if (IsDisposed) return;
                    BeginInvoke(new Action(() =>
                    {
                        progressBar.Value = Math.Min(current, total);
                    }));
                };

                _engine.OnStatusChanged += (msg) =>
                {
                    if (IsDisposed) return;
                    BeginInvoke(new Action(() =>
                    {
                        labelStatus.Text = msg;
                    }));
                };

                _engine.OnTestPointCompleted += (point) =>
                {
                    if (IsDisposed) return;
                    BeginInvoke(new Action(() =>
                    {
                        _allResults.Add(point);
                        AddResultRow(point);
                    }));
                };

                _engine.OnCompleted += (results) =>
                {
                    if (IsDisposed) return;
                    BeginInvoke(new Action(() =>
                    {
                        // 刷新能力发挥（计算完成后更新）
                        if (results.Count > 0)
                        {
                            UpdateCapabilityColumn(results);
                        }

                        EnableConfigMode(true);
                        progressBar.Visible = false;
                        _engine = null;
                    }));
                };

                _engine.OnError += (error) =>
                {
                    if (IsDisposed) return;
                    BeginInvoke(new Action(() =>
                    {
                        labelStatus.Text = error;
                        AppContainer.Logger().Error($"能效分析错误: {error}");
                    }));
                };

                // 运行跑分
                await _engine.RunAsync(config);
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (_engine == null || !_engine.IsRunning)
                return;

            var result = MessageBox.Show(
                Properties.Strings.TextBenchmarkCancel,
                Properties.Strings.TextBenchmarkTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _engine.Stop();
                EnableConfigMode(true);
                progressBar.Visible = false;
            }
        }

        /// <summary>
        /// 在 DataGridView 中添加一行结果
        /// </summary>
        private void AddResultRow(BenchmarkTestPoint point)
        {
            var rowIndex = dataGridViewResults.Rows.Add(
                $"{point.SetTdp:F0}",
                point.Score.ToString("N0"),
                $"{point.PowerMin:F2}",
                $"{point.PowerMax:F2}",
                $"{point.PowerAvg:F2}",
                $"{point.PowerMedian:F2}",
                $"{point.TempMin:F1}",
                $"{point.TempMax:F1}",
                $"{point.TempAvg:F1}",
                $"{point.TempMedian:F1}",
                $"{point.CpuFreqAvg:F0}",
                $"{point.Efficiency:F0}",
                $"{point.Capability:P0}"
            );

            // 滚动到最新行
            if (rowIndex >= 0)
            {
                dataGridViewResults.FirstDisplayedScrollingRowIndex = rowIndex;
            }

            // 高亮最高能效比
            HighlightBestRow();
        }

        /// <summary>
        /// 更新能力发挥列
        /// </summary>
        private void UpdateCapabilityColumn(List<BenchmarkTestPoint> results)
        {
            if (results.Count == 0)
                return;

            var maxScore = results.Max(r => r.Score);
            for (var i = 0; i < results.Count && i < dataGridViewResults.Rows.Count; i++)
            {
                var capability = maxScore > 0 ? (double)results[i].Score / maxScore : 0;
                dataGridViewResults.Rows[i].Cells[12].Value = capability.ToString("P0");
                results[i].Capability = capability;
            }

            HighlightBestRow();
        }

        /// <summary>
        /// 高亮最佳能效比的行
        /// </summary>
        private void HighlightBestRow()
        {
            // 清除之前的高亮
            foreach (DataGridViewRow row in dataGridViewResults.Rows)
            {
                row.DefaultCellStyle.BackColor = SystemColors.Window;
                row.DefaultCellStyle.ForeColor = SystemColors.ControlText;
            }

            if (dataGridViewResults.Rows.Count == 0)
                return;

            // 找能效比最高行
            var bestEfficiency = 0f;
            var bestRowIndex = -1;

            for (var i = 0; i < dataGridViewResults.Rows.Count; i++)
            {
                var cell = dataGridViewResults.Rows[i].Cells[11].Value?.ToString();
                if (float.TryParse(cell, out var eff) && eff > bestEfficiency)
                {
                    bestEfficiency = eff;
                    bestRowIndex = i;
                }
            }

            if (bestRowIndex >= 0)
            {
                dataGridViewResults.Rows[bestRowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 如果测试仍在运行，询问是否退出
            if (_engine != null && _engine.IsRunning)
            {
                var result = MessageBox.Show(
                    Properties.Strings.TextBenchmarkCancel,
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _engine.Stop();
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }
    }
}
