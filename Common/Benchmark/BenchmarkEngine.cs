using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RyzenTuner.Common.Container;
using RyzenTuner.Properties;

namespace RyzenTuner.Common.Benchmark
{
    /// <summary>
    /// 能效分析跑分引擎
    ///
    /// 负责：
    ///   1. 遍历 TDP 范围，逐档设置功耗限制
    ///   2. 每档运行 BenchmarkWorkload 并采集传感器数据
    ///   3. 计算统计量（均值、中位数等）
    ///   4. 通过事件向 UI 层报告进度和结果
    /// </summary>
    public class BenchmarkEngine : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private bool _isRunning;
        private bool _disposed;

        /// <summary>每个测试点完成时触发</summary>
        public event Action<BenchmarkTestPoint>? OnTestPointCompleted;

        /// <summary>进度变更（当前索引, 总数）</summary>
        public event Action<int, int>? OnProgressChanged;

        /// <summary>状态消息</summary>
        public event Action<string>? OnStatusChanged;

        /// <summary>全部测试完成</summary>
        public event Action<List<BenchmarkTestPoint>>? OnCompleted;

        /// <summary>发生了错误</summary>
        public event Action<string>? OnError;

        public bool IsRunning => _isRunning;

        /// <summary>
        /// 异步执行跑分
        /// </summary>
        public async Task RunAsync(BenchmarkConfig config)
        {
            if (_isRunning)
                return;

            _isRunning = true;
            var results = new List<BenchmarkTestPoint>();
            var processor = AppContainer.AmdProcessor();
            var hwMonitor = AppContainer.HardwareMonitor();

            // 保存原始设置在 try 之前，确保 finally 中可访问
            var totalPoints = config.GetTestPointCount();
            var keepAwakeWas = Settings.Default.KeepAwake;
            var currentModeWas = Settings.Default.CurrentMode;
            var originalTctlTemp = Settings.Default.TctlTemp;
            var originalApuSkinTemp = Settings.Default.ApuSkinTemp;

            // 测试期间阻止系统休眠
            if (!keepAwakeWas)
            {
                Common.Awake.KeepingSysAwake(true);
            }

            try
            {
                for (var i = 0; i < totalPoints; i++)
                {
                    if (_cts.Token.IsCancellationRequested)
                        break;

                    var tdp = config.GetTdpAtIndex(i);
                    OnProgressChanged?.Invoke(i + 1, totalPoints);
                    OnStatusChanged?.Invoke(
                        Properties.Strings.TextBenchmarkProgress
                            .Replace("{tdp}", tdp.ToString("F0"))
                            .Replace("{current}", (i + 1).ToString())
                            .Replace("{total}", totalPoints.ToString())
                    );

                    // 1. 设置 TDP 限制
                    if (!ApplyTdpLimit(processor, tdp))
                    {
                        OnError?.Invoke($"设置 {tdp}W 失败，跳过此测试点");
                        continue;
                    }

                    // 2. 等待系统稳定（用户设定秒数）
                    OnStatusChanged?.Invoke($"正在稳定 {tdp}W...（{config.RestSeconds}秒）");
                    await WaitStableAsync(config.RestSeconds * 1000, _cts.Token);

                    // 3. 启动跑分
                    var point = await RunSingleTestPointAsync(config, tdp, processor, hwMonitor);

                    // 4. 记录结果
                    results.Add(point);
                    OnTestPointCompleted?.Invoke(point);
                }

                // 计算能力发挥（Capability）
                if (results.Count > 0)
                {
                    var maxScore = results.Max(r => r.Score);
                    foreach (var r in results)
                    {
                        r.Capability = maxScore > 0 ? (double)r.Score / maxScore : 0;
                    }
                }

                if (_cts.Token.IsCancellationRequested)
                {
                    OnStatusChanged?.Invoke(Properties.Strings.TextBenchmarkStopped);
                }
                else
                {
                    OnStatusChanged?.Invoke(Properties.Strings.TextBenchmarkDone);
                }

                OnCompleted?.Invoke(results);
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Error($"能效分析跑分异常: {ex}");
                OnError?.Invoke($"跑分异常: {ex.Message}");
            }
            finally
            {
                // 始终恢复原始设置（即使发生异常）
                RestoreOriginalSettings(processor, currentModeWas, originalTctlTemp, originalApuSkinTemp);

                // 始终恢复系统休眠状态（如果之前强制唤醒过）
                if (!keepAwakeWas)
                {
                    Common.Awake.AllowSysSleep();
                }

                _isRunning = false;
            }
        }

        /// <summary>
        /// 停止跑分
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _cts.Cancel();
        }

        /// <summary>
        /// 运行单档测试
        /// </summary>
        private async Task<BenchmarkTestPoint> RunSingleTestPointAsync(
            BenchmarkConfig config, float tdp,
            Processor.AmdProcessor processor,
            Hardware.HardwareMonitor hwMonitor)
        {
            var threadCount = config.TestType == BenchmarkTestType.MultiCore
                ? Math.Max(1, Environment.ProcessorCount - 1)
                : 1;
            var sampleIntervalMs = 500;
            var totalSamples = config.DurationSeconds * 1000 / sampleIntervalMs;

            var powerSamples = new List<float>(totalSamples);
            var tempSamples = new List<float>(totalSamples);
            var freqSamples = new List<float>(totalSamples);

            using var workload = new BenchmarkWorkload();

            // 启动数据采集任务
            var samplingTask = Task.Run(() =>
                SamplingLoop(hwMonitor, powerSamples, tempSamples, freqSamples,
                    config.DurationSeconds * 1000, sampleIntervalMs, _cts.Token));

            // 启动跑分
            var score = await workload.RunAsync(threadCount, config.DurationSeconds * 1000, _cts.Token);

            // 等待采集任务完成
            await samplingTask;

            // 计算统计量
            var point = new BenchmarkTestPoint
            {
                SetTdp = tdp,
                Score = score,
                PowerMin = powerSamples.Count > 0 ? powerSamples.Min() : 0,
                PowerMax = powerSamples.Count > 0 ? powerSamples.Max() : 0,
                PowerAvg = powerSamples.Count > 0 ? powerSamples.Average() : 0,
                PowerMedian = powerSamples.Count > 0 ? Median(powerSamples) : 0,
                TempMin = tempSamples.Count > 0 ? tempSamples.Min() : 0,
                TempMax = tempSamples.Count > 0 ? tempSamples.Max() : 0,
                TempAvg = tempSamples.Count > 0 ? tempSamples.Average() : 0,
                TempMedian = tempSamples.Count > 0 ? Median(tempSamples) : 0,
                CpuFreqAvg = freqSamples.Count > 0 ? freqSamples.Average() : 0,
                RawPowerSamples = powerSamples,
                RawTempSamples = tempSamples,
                RawFreqSamples = freqSamples,
            };

            return point;
        }

        /// <summary>
        /// 数据采集循环（每 sampleIntervalMs 采样一次）
        /// </summary>
        private void SamplingLoop(
            Hardware.HardwareMonitor hwMonitor,
            List<float> powerSamples, List<float> tempSamples, List<float> freqSamples,
            int totalDurationMs, int sampleIntervalMs,
            CancellationToken ct)
        {
            var stopTime = Environment.TickCount + totalDurationMs + 100; // 多留 100ms 余量
            var sw = new Stopwatch();

            while (Environment.TickCount < stopTime && !ct.IsCancellationRequested)
            {
                sw.Restart();

                // 刷新并读取传感器数据
                hwMonitor.Monitor();
                powerSamples.Add(hwMonitor.CpuPackagePower);
                tempSamples.Add(hwMonitor.CpuTemperature);
                freqSamples.Add(hwMonitor.CpuFreq);

                // 等待剩余时间以达到采样间隔
                var elapsed = sw.ElapsedMilliseconds;
                var remaining = sampleIntervalMs - (int)elapsed;
                if (remaining > 0 && !ct.IsCancellationRequested)
                {
                    try
                    {
                        Task.Delay(remaining, ct).Wait(ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 计算中位数
        /// </summary>
        private static float Median(List<float> sortedValues)
        {
            if (sortedValues.Count == 0)
                return 0;

            // 排序（采样数据本身是无序的）
            var sorted = new List<float>(sortedValues);
            sorted.Sort();

            var n = sorted.Count;
            if (n % 2 == 1)
            {
                return sorted[n / 2];
            }

            return (sorted[n / 2 - 1] + sorted[n / 2]) / 2f;
        }

        /// <summary>
        /// 设置全部三个 PPT 限制到同一值
        /// </summary>
        private static bool ApplyTdpLimit(Processor.AmdProcessor processor, float tdp)
        {
            var ok = true;
            ok &= processor.SetFastPPT(tdp);
            ok &= processor.SetSlowPPT(tdp);
            ok &= processor.SetStampPPT(tdp);
            return ok;
        }

        /// <summary>
        /// 等待稳定时间
        /// </summary>
        /// <param name="delayMs">延迟毫秒数</param>
        private async Task WaitStableAsync(int delayMs, CancellationToken ct)
        {
            try
            {
                await Task.Delay(delayMs, ct);
            }
            catch (OperationCanceledException)
            {
                // 取消时不处理
            }
        }

        /// <summary>
        /// 恢复原始设置
        /// </summary>
        private void RestoreOriginalSettings(
            Processor.AmdProcessor processor,
            string originalMode,
            int tctlTemp,
            int apuSkinTemp)
        {
            try
            {
                // 恢复原始 TDP 限制
                var originalTdp = Utils.RyzenTunerUtils.GetPowerLimitByMode(originalMode);
                ApplyTdpLimit(processor, originalTdp);

                if (tctlTemp >= 30)
                    processor.SetTctlTemp((uint)tctlTemp);
                if (apuSkinTemp >= 30)
                    processor.SetApuSkinTemp((uint)apuSkinTemp);

                // 恢复模式
                Settings.Default.CurrentMode = originalMode;
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning($"恢复原始设置失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
