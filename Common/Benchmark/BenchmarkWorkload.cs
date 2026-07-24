using System;
using System.Diagnostics;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace RyzenTuner.Common.Benchmark
{
    /// <summary>
    /// CPU 跑分负载 —— 使用 xorshift64 伪随机数生成器作为纯 CPU 密集任务。
    /// 无需释放资源，可直接丢弃。
    ///
    /// 每次迭代执行一组移位-异或运算（xorshift64 PRNG 步进），
    /// 每一步的结果作为下一步的输入，形成完整的数据依赖链，
    /// 确保 JIT 编译器无法优化掉循环或提前计算。
    ///
    /// 关键设计原则：
    ///   - 热循环内零内存分配，零 GC 触发，确保 CPU 时间 100% 用于计算
    ///   - 每个工作线程独立计算，无锁竞争
    ///   - AboveNormal 优先级 + 工作线程数由调用方控制（通常为 Environment.ProcessorCount - 1），兼顾负载与响应
    /// </summary>
    public static class BenchmarkWorkload
    {
        // 线程种子偏移量：确保不同线程的高位不同，避免种子碰撞
        private const ulong ThreadSeedStride = 0x100000000;

        /// <summary>
        /// 运行跑分，返回迭代次数。
        /// </summary>
        /// <param name="threadCount">工作线程数</param>
        /// <param name="durationMs">测试持续时间（毫秒）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>所有线程总跑分成绩（含 PRNG 输出，非纯迭代计数）</returns>
        public static async Task<long> RunAsync(int threadCount, int durationMs, CancellationToken cancellationToken)
        {
            if (threadCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(threadCount), threadCount, "threadCount must be > 0");
            if (durationMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationMs), durationMs, "durationMs must be > 0");

            var tasks = new Task<long>[threadCount];
            for (var i = 0; i < threadCount; i++)
            {
                var threadSeed = (ulong)i * ThreadSeedStride + 1;
                tasks[i] = Task.Factory.StartNew(
                    () => RunWorker(threadSeed, durationMs, cancellationToken),
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            long total = 0;
            foreach (var r in results)
            {
                total += r;
            }

            return total;
        }

        /// <summary>
        /// 工作线程：运行 xorshift64 循环，零分配确保 100% CPU 利用率。
        /// </summary>
        private static long RunWorker(ulong seed, int durationMs, CancellationToken cancellationToken)
        {
            // 如果调用前已取消，立即返回避免无谓启动
            if (cancellationToken.IsCancellationRequested)
                return 0;

            // 设为 AboveNormal 优先级，使工作线程优先获取 CPU 时间，
            // 但保留一个核心给 UI/采样线程。
            // 失败时继续以默认值运行，不阻断测试。
            // 设为后台线程，防止用户在跑分中关闭程序时进程无法退出。
            Thread.CurrentThread.IsBackground = true;
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            }
            catch (Exception ex) when (ex is ThreadStateException or SecurityException)
            {
                Trace.WriteLine(
                    $"[BenchmarkWorkload] Failed to set thread priority: {ex.Message}");
            }

            // 使用 Stopwatch 计时，避免 Environment.TickCount 的 int 溢出问题
            var stopwatch = Stopwatch.StartNew();
            ulong state = seed;
            long score = 0;

            // 热循环 —— 纯 ALU 运算，零内存分配，单核跑满
            // 每 1024 次迭代才检查一次计时器，分摊 QueryPerformanceCounter 的 ~50-100ns 开销
            const int batchSize = 1024;
            while (stopwatch.ElapsedMilliseconds < durationMs && !cancellationToken.IsCancellationRequested)
            {
                for (var j = 0; j < batchSize; j++)
                {
                    // xorshift64 步进：数据依赖链，计算值影响后续所有步
                    state ^= state << 13;
                    state ^= state >> 7;
                    state ^= state << 17;

                    // 将 PRNG 状态的低字节融入 score，确保 JIT 无法消除 xorshift 计算
                    // score 同时包含迭代基数（每次 +1）和 PRNG 输出波动（每次 +0~255），
                    // 因此 score 不是纯迭代计数，而是用于相对比较的跑分
                    score += 1 + (long)(state & 0xFF);
                }
            }

            return score;
        }
    }
}
