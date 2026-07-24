using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RyzenTuner.Common.Container;
using RyzenTuner.Common.Settings;

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
        private CancellationTokenSource? _cts;
        private volatile int _isRunning;
        private int _disposed;

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

        public bool IsRunning => _isRunning != 0;

        /// <summary>
        /// 异步执行跑分
        /// </summary>
        public async Task RunAsync(BenchmarkConfig config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            if (Volatile.Read(ref _disposed) != 0)
                return;
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
                return;

            // 每次调用 RunAsync 重新创建 CancellationTokenSource，
            // 避免上一次 Stop() 取消后 Token 永久处于取消状态，导致后续跑分无法运行。
            // 注意：不传播旧 CTS 的取消状态——旧 CTS 的取消反映的是前一次运行的 Stop() 请求，
            // 不应影响当前新运行的开始。
            var oldCts = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
            oldCts?.Dispose();

            // 如果在 _disposed 检查之后、CTS 交换之前调用了 Dispose()，则新创建的 CTS 永不会被释放。
            // 此处二次检查 _disposed，若已释放则清理新 CTS 并立即返回。
            if (Volatile.Read(ref _disposed) != 0)
            {
                Interlocked.Exchange(ref _isRunning, 0);
                // 清理刚创建的 CTS（当前在 _cts 中），避免泄漏。
                // 同时将 _cts 置为 null，防止并发运行的 Dispose() 操作已释放的 CTS。
                Interlocked.Exchange(ref _cts, null)?.Dispose();
                return;
            }

            // 捕获当前 CTS 到局部变量，防止并发 Dispose() 导致 _cts 被释放后仍被访问
            var cts = Volatile.Read(ref _cts);
            if (cts is null)
            {
                Interlocked.Exchange(ref _isRunning, 0);
                return;
            }

            // 声明所有需要在 finally 中访问的变量（初始化值在 try 内设置）
            Processor.AmdProcessor? processor = null;
            Hardware.HardwareMonitor? hardwareMonitor = null;
            var keepAwakeWas = false;
            var currentModeWas = "BalancedMode";
            var originalTctlTemp = 100;
            var originalApuSkinTemp = 43;

            var results = new List<BenchmarkTestPoint>();
            try
            {
                processor = AppContainer.AmdProcessor();

                if (processor == null)
                {
                    OnError?.Invoke("AMDProcessor initialization failed");
                    return;
                }

                hardwareMonitor = AppContainer.HardwareMonitor();
                if (hardwareMonitor == null)
                {
                    OnError?.Invoke("HardwareMonitor initialization failed");
                    return;
                }

                keepAwakeWas = AppSettings.GetBool("KeepAwake");
                currentModeWas = AppSettings.Get("CurrentMode", "BalancedMode");
                originalTctlTemp = AppSettings.Get("TctlTemp", 100);
                originalApuSkinTemp = AppSettings.Get("ApuSkinTemp", 43);

                // 测试期间阻止系统休眠
                if (!keepAwakeWas)
                {
                    if (!Awake.KeepSystemAwake(true))
                    {
                        AppContainer.Logger().Warning("Benchmark", "保持系统唤醒失败，跑分期间系统可能进入休眠");
                    }
                }

                // 验证配置中至少有一个测试点
                if (config.TestPointCount <= 0)
                {
                    var errorMsg = $"跑分配置错误：没有有效的测试点（StartTdp={config.StartTdp}, EndTdp={config.EndTdp}, StepTdp={config.StepTdp}）";
                    OnError?.Invoke(errorMsg);
                    OnStatusChanged?.Invoke(errorMsg);
                    return;
                }

                var totalPoints = config.TestPointCount;

                for (var i = 0; i < totalPoints; i++)
                {
                    if (cts.IsCancellationRequested)
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
                        OnError?.Invoke($"设置 {tdp:F0}W 失败，跳过此测试点");
                        OnStatusChanged?.Invoke($"设置 {tdp:F0}W 失败，跳过此测试点");
                        continue;
                    }

                    // 2. 等待系统稳定（用户设定秒数）
                    OnStatusChanged?.Invoke($"正在稳定 {tdp:F0}W...（{config.RestSeconds}秒）");
                    var restMs = (int)Math.Min((long)config.RestSeconds * 1000,
                        int.MaxValue);
                    await WaitStableAsync(restMs, cts.Token);

                    // 如果等待期间被取消，跳过本测试点
                    if (cts.IsCancellationRequested)
                        break;

                    // 3. 启动跑分
                    var point = await RunSingleTestPointAsync(config, tdp, hardwareMonitor, cts.Token);

                    // 4. 记录原始结果（缩放后统一通知 OnTestPointCompleted）
                    results.Add(point);
                }

                // 将跑分成绩缩放到可读范围（控制在五位数以内，≤99,999）
                if (results.Count > 0)
                {
                    var maxScore = results.Max(r => r.Score);
                    long divisor = 1;

                    // 用整数循环计算 10 的 n 次幂，避免浮点数精度问题
                    if (maxScore > 99999)
                    {
                        var temp = maxScore;
                        while (temp > 99999)
                        {
                            temp /= 10;
                            divisor *= 10;
                        }

                        foreach (var r in results)
                        {
                            r.ScaledScore = Math.Max(r.Score / divisor, 1L);
                        }
                    }
                    else
                    {
                        // 无需缩放时，ScaledScore = Score
                        foreach (var r in results)
                        {
                            r.ScaledScore = r.Score;
                        }
                    }

                    // 计算能力发挥（Capability）
                    // 使用原始 Score 而非 ScaledScore 计算，避免 Math.Max 截断带来的精度失真
                    foreach (var r in results)
                    {
                        r.Capability = maxScore > 0 ? (double)r.Score / maxScore : 0;
                    }
                }

                // 所有测试点已完成缩放，此时 ScaledScore/Capability 为最终值，通知各点完成。
                // 延迟通知确保观察者收到的是完全初始化的对象。
                foreach (var r in results)
                {
                    OnTestPointCompleted?.Invoke(r);
                }

                if (cts.IsCancellationRequested)
                {
                    OnStatusChanged?.Invoke(Properties.Strings.TextBenchmarkStopped);
                }
                else
                {
                    OnStatusChanged?.Invoke(Properties.Strings.TextBenchmarkDone);
                }

                OnCompleted?.Invoke(results);
            }
            catch (OperationCanceledException)
            {
                // 取消是用户主动操作，不是异常
                OnStatusChanged?.Invoke(Properties.Strings.TextBenchmarkStopped);
                OnCompleted?.Invoke(results);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException)
            {
                AppContainer.Logger().Error("Benchmark", $"能效分析跑分异常: {ex}");
                OnError?.Invoke($"跑分异常: {ex.Message}");
                OnCompleted?.Invoke(results);
            }
            finally
            {
                // 始终恢复原始设置（即使发生异常；processor 为 null 表示初始化失败，无需恢复）
                if (processor != null)
                {
                    RestoreOriginalSettings(processor, currentModeWas, originalTctlTemp, originalApuSkinTemp);
                }

                // 始终恢复系统休眠状态（如果之前强制唤醒过）
                if (!keepAwakeWas)
                {
                    Awake.AllowSystemSleep();
                }

                Interlocked.Exchange(ref _isRunning, 0);

                // 释放本轮 RunAsync 创建的 CTS。使用 Interlocked.Exchange 原子化获取，
                // 避免与 Dispose() 中同样的操作发生竞态。
                // 如果 Dispose() 已将 _cts 置为 null（先执行了），此处拿到的 local 为 null，跳过释放。
                var local = Interlocked.Exchange(ref _cts, null);
                local?.Dispose();
            }
        }

        /// <summary>
        /// 停止跑分
        /// </summary>
        public void Stop()
        {
            if (Volatile.Read(ref _disposed) != 0 || _isRunning == 0)
                return;

            // 先捕获当前 _cts 再取消，避免 RunAsync 完成后重新进入（新 CTS）时误取消新实例
            var cts = Volatile.Read(ref _cts);
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // _cts 可能在捕获后被 RunAsync 的 Interlocked.Exchange 替换并释放。
                // 当前代码路径中 RunAsync 与 Stop 均由 UI 线程调用，此竞态理论存在但实际不会触发，
                // 此处防御性捕获以防止未来的重构风险。
            }
        }

        /// <summary>
        /// 运行单档测试
        /// </summary>
        private async Task<BenchmarkTestPoint> RunSingleTestPointAsync(
            BenchmarkConfig config, float tdp,
            Hardware.HardwareMonitor hardwareMonitor,
            CancellationToken ct)
        {
            // 多核测试时保留一个核心给系统进程（避免系统无响应），最少使用 1 个核心
            var threadCount = config.TestType == BenchmarkTestType.MultiCore
                ? Math.Max(1, Environment.ProcessorCount - 1)
                : 1;
            var sampleIntervalMs = 500;
            var totalDurationMs =
                (int)Math.Min((long)config.DurationSeconds * 1000, int.MaxValue);
            var totalSamples = Math.Max(1, totalDurationMs / sampleIntervalMs);

            var powerSamples = new List<float>(totalSamples);
            var tempSamples = new List<float>(totalSamples);
            var freqSamples = new List<float>(totalSamples);

            // 启动数据采集任务
            // 使用 Task.Run 确保采样循环运行在线程池上，避免因 SynchronizationContext
            // （UI 线程）导致每次 await Task.Delay 后回送到 UI 线程阻塞消息泵
            var samplingTask = Task.Run(() =>
                SamplingLoopAsync(hardwareMonitor, powerSamples, tempSamples, freqSamples,
                    totalDurationMs, sampleIntervalMs, ct));

            long score;
            try
            {
                // 启动跑分
                score = await BenchmarkWorkload.RunAsync(threadCount,
                    totalDurationMs, ct);
            }
            finally
            {
                // 不取消 _cts（该取消令牌被所有测试点共享）：
                // 采样循环会因总时长到期而自然结束，只需等待它完成即可。
                // 包裹在 try-catch 中避免掩盖原始工作负载异常
                try { await samplingTask; }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    AppContainer.Logger().Error("Benchmark", $"采样任务异常: {ex}");
                }
            }

            // 计算统计量
            // 先计算所有统计值再赋值，避免 Median() 就地排序影响对象初始值设定项中求值顺序的脆弱性
            var powerMin = powerSamples.Count > 0 ? powerSamples.Min() : 0f;
            var powerMax = powerSamples.Count > 0 ? powerSamples.Max() : 0f;
            var powerAvg = powerSamples.Count > 0 ? powerSamples.Average() : 0f;
            var powerMedian = powerSamples.Count > 0 ? Median(powerSamples) : 0f;
            var tempMin = tempSamples.Count > 0 ? tempSamples.Min() : 0f;
            var tempMax = tempSamples.Count > 0 ? tempSamples.Max() : 0f;
            var tempAvg = tempSamples.Count > 0 ? tempSamples.Average() : 0f;
            var tempMedian = tempSamples.Count > 0 ? Median(tempSamples) : 0f;
            var cpuFreqAvg = freqSamples.Count > 0 ? freqSamples.Average() : 0f;

            var point = new BenchmarkTestPoint
            {
                SetTdp = tdp,
                Score = score,
                PowerMin = powerMin,
                PowerMax = powerMax,
                PowerAvg = powerAvg,
                PowerMedian = powerMedian,
                TempMin = tempMin,
                TempMax = tempMax,
                TempAvg = tempAvg,
                TempMedian = tempMedian,
                CpuFreqAvg = cpuFreqAvg,
            };

            return point;
        }

        /// <summary>
        /// 数据采集循环（每 sampleIntervalMs 采样一次）
        /// </summary>
        private async Task SamplingLoopAsync(
            Hardware.HardwareMonitor hardwareMonitor,
            List<float> powerSamples, List<float> tempSamples, List<float> freqSamples,
            int totalDurationMs, int sampleIntervalMs,
            CancellationToken ct)
        {
            // 使用 Stopwatch 计时，避免 Environment.TickCount 的 int 溢出
            const int samplingBufferMs = 100;
            var totalSw = Stopwatch.StartNew();
            var sampleSw = new Stopwatch();

            while (totalSw.ElapsedMilliseconds < (long)totalDurationMs + samplingBufferMs && !ct.IsCancellationRequested)
            {
                sampleSw.Restart();

                // 刷新并读取传感器数据（使用快照方法，一次加锁读取全部值）
                hardwareMonitor.Monitor();
                var (power, temp, frequency) = hardwareMonitor.GetSnapshot();

                // 过滤 NaN 和 Infinity 值，避免污染统计量（Min/Max/Average 对 NaN/Infinity 敏感）
                // 注：float.IsNaN() 不捕获 float.IsInfinity()，需单独检查
                if (!float.IsNaN(power) && !float.IsInfinity(power))
                    powerSamples.Add(power);
                if (!float.IsNaN(temp) && !float.IsInfinity(temp))
                    tempSamples.Add(temp);
                if (!float.IsNaN(frequency) && !float.IsInfinity(frequency))
                    freqSamples.Add(frequency);

                // 等待剩余时间以达到采样间隔（异步等待，不阻塞线程池线程）
                var remaining = sampleIntervalMs - (int)Math.Min(sampleSw.ElapsedMilliseconds, int.MaxValue);
                if (remaining > 0 && !ct.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(remaining, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // CancellationTokenSource 已释放（并发 Dispose），退出采样循环
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 计算中位数
        /// </summary>
        private static float Median(List<float> values)
        {
            if (values.Count == 0)
                return 0;

            var sorted = values.ToList();
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
            ok &= processor.SetFastPpt(tdp);
            ok &= processor.SetSlowPpt(tdp);
            ok &= processor.SetStapmPpt(tdp);
            return ok;
        }

        /// <summary>
        /// 等待稳定时间
        /// </summary>
        /// <param name="delayMs">延迟毫秒数</param>
        /// <param name="ct">取消令牌</param>
        private async Task WaitStableAsync(int delayMs, CancellationToken ct)
        {
            try
            {
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 取消时不处理
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource 已释放（并发 Dispose），不做处理
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
            // 首先检查用户是否在跑分期间手动切换了模式
            var currentMode = AppSettings.Get("CurrentMode", "BalancedMode");
            var modeChanged = !string.Equals(currentMode, originalMode, StringComparison.Ordinal);

            if (modeChanged)
            {
                // 用户切换了模式 — 不恢复硬件设置，让下一 tick 自动应用用户选择的新模式值
                AppContainer.Logger().Info("Benchmark",
                    $"跑分期间用户已切换到 {currentMode}，跳过硬件恢复");
                return;
            }

            // 1. 尝试恢复原始 TDP 限制，失败时不阻断后续恢复
            try
            {
                var originalTdp = Utils.RyzenTunerUtils.GetPowerLimitByMode(originalMode);
                if (!ApplyTdpLimit(processor, originalTdp))
                {
                    AppContainer.Logger().Warning("Benchmark", $"恢复原始 TDP 失败 (ApplyTdpLimit 返回 false)");
                }
            }
            catch (Exception ex)
            {
                AppContainer.Logger().Warning("Benchmark", $"恢复原始 TDP 失败（获取配置异常）: {ex}");
            }

            // 2. 恢复温度限制（即使 TDP 恢复失败也应继续）
            // 注意：此方法在 finally 块中调用，必须捕获所有异常。
            // 如果 AmdProcessor 已被 Dispose（并发关闭），SetTctlTemp/SetApuSkinTemp
            // 会抛出 ObjectDisposedException，不能让它逃逸到 finally 外。
            const int minValidCelsius = 30;
            if (tctlTemp >= minValidCelsius)
            {
                try
                {
                    if (!processor.SetTctlTemp((uint)tctlTemp))
                    {
                        AppContainer.Logger().Warning("Benchmark", "恢复 TctlTemp 失败");
                    }
                }
                catch (Exception ex)
                {
                    AppContainer.Logger().Warning("Benchmark", $"恢复 TctlTemp 异常: {ex.Message}");
                }
            }

            if (apuSkinTemp >= minValidCelsius)
            {
                try
                {
                    if (!processor.SetApuSkinTemp((uint)apuSkinTemp))
                    {
                        AppContainer.Logger().Warning("Benchmark", "恢复 ApuSkinTemp 失败");
                    }
                }
                catch (Exception ex)
                {
                    AppContainer.Logger().Warning("Benchmark", $"恢复 ApuSkinTemp 异常: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            // 使用 Interlocked.Exchange 原子化获取并清空 _cts，
            // 避免与 RunAsync 中 Interlocked.Exchange 的 TOCTOU 竞态。
            // 如果 RunAsync 已替换 _cts 为新实例，此处只拿到旧实例，
            // 新实例由 RunAsync 的 finally 块负责释放。
            var cts = Interlocked.Exchange(ref _cts, null);
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // RunAsync 可能已在 finally 中释放了该 CTS，忽略
            }

            cts?.Dispose();
        }
    }
}
