using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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

            // 调整列宽（Fill 模式下设置 FillWeight 实现比例分配）
            if (dataGridViewResults.Columns.Count >= 13)
            {
                // TDP 窄列, Score 稍宽, 其余均匀, Capability 自适应
                dataGridViewResults.Columns[0].FillWeight = 80;
                dataGridViewResults.Columns[1].FillWeight = 100;
                for (var i = 2; i <= 11; i++)
                    dataGridViewResults.Columns[i].FillWeight = 90;
                dataGridViewResults.Columns[12].FillWeight = 120;
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
            numericUpDownRestTime.Enabled = enabled;
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
            buttonExportCsv.Enabled = false;
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
                        // 刷新所有显示值（跑分经过缩放后更新界面）
                        if (results.Count > 0)
                        {
                            RefreshAllResults(results);
                        }

                        buttonExportCsv.Enabled = results.Count > 0;
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
                        buttonExportCsv.Enabled = _allResults.Count > 0;
                        EnableConfigMode(true);
                        progressBar.Visible = false;
                        _engine = null;
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
        /// 导出 CSV
        /// </summary>
        private void buttonExportCsv_Click(object sender, EventArgs e)
        {
            if (_allResults.Count == 0)
            {
                MessageBox.Show(
                    Properties.Strings.TextBenchmarkExportNoData,
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = Properties.Strings.TextBenchmarkExportSaveFilter,
                FilterIndex = 1,
                RestoreDirectory = true,
                FileName =
                    $"{Properties.Strings.TextBenchmarkExportFileName}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                ExportResultsToCsv(sfd.FileName);
                MessageBox.Show(
                    Properties.Strings.TextBenchmarkExportSuccess.Replace("{path}", sfd.FileName),
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Error($"导出 CSV 失败: {ex}");
                MessageBox.Show(
                    $"{Properties.Strings.TextBenchmarkExportFailed}: {ex.Message}",
                    Properties.Strings.TextBenchmarkTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 将结果写入 CSV 文件
        /// </summary>
        private void ExportResultsToCsv(string filePath)
        {
            var sb = new StringBuilder();

            // UTF-8 BOM 确保 Excel 正确识别编码

            // 元数据
            var testType = comboBoxTestType.SelectedIndex == 0
                ? Properties.Strings.TextBenchmarkSingleCore
                : Properties.Strings.TextBenchmarkMultiCore;
            sb.AppendLine($"# {Properties.Strings.TextBenchmarkTitle}");
            sb.AppendLine($"# {Properties.Strings.TextBenchmarkTestType}: {testType}");
            sb.AppendLine($"# {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // CSV 表头（使用 DataGridView 列名，避免与 SetupDataGridView 不同步）
            var headers = dataGridViewResults.Columns
                .Cast<DataGridViewColumn>()
                .Select(c => c.HeaderText);

            sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

            // 数据行
            // 警告：此数组顺序必须与 SetupDataGridView 中的列定义完全一致。
            // 添加/删除/重排列时，两处必须同步更新。
            foreach (var r in _allResults)
            {
                var values = new[]
                {
                    $"{r.SetTdp:F0}",
                    r.Score.ToString("D"),
                    $"{r.PowerMin:F2}",
                    $"{r.PowerMax:F2}",
                    $"{r.PowerAvg:F2}",
                    $"{r.PowerMedian:F2}",
                    $"{r.TempMin:F1}",
                    $"{r.TempMax:F1}",
                    $"{r.TempAvg:F1}",
                    $"{r.TempMedian:F1}",
                    $"{r.CpuFreqAvg:F0}",
                    $"{r.Efficiency:F0}",
                    $"{r.Capability:P0}",
                };
                sb.AppendLine(string.Join(",", values.Select(EscapeCsvField)));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 转义 CSV 字段（包含逗号或引号时加双引号）
        /// </summary>
        private static string EscapeCsvField(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
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
        /// 刷新所有结果显示（跑分成绩缩放后更新界面）
        /// </summary>
        private void RefreshAllResults(List<BenchmarkTestPoint> results)
        {
            for (var i = 0; i < results.Count && i < dataGridViewResults.Rows.Count; i++)
            {
                var r = results[i];
                dataGridViewResults.Rows[i].Cells[1].Value = r.Score.ToString("N0");
                dataGridViewResults.Rows[i].Cells[11].Value = r.Efficiency.ToString("F0");
                dataGridViewResults.Rows[i].Cells[12].Value = r.Capability.ToString("P0");
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
