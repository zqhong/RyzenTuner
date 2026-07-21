using System.Diagnostics;
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
    ///   - AboveNormal 优先级 + 预留一个核心给 UI/采样，兼顾负载与响应
    /// </summary>
    public static class BenchmarkWorkload
    {
        /// <summary>
        /// 运行跑分，返回迭代次数。
        /// </summary>
        /// <param name="threadCount">工作线程数</param>
        /// <param name="durationMs">测试持续时间（毫秒）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>所有线程总迭代次数</returns>
        public static async Task<long> RunAsync(int threadCount, int durationMs, CancellationToken cancellationToken)
        {
            var tasks = new Task<long>[threadCount];
            for (var i = 0; i < threadCount; i++)
            {
                var threadSeed = (ulong)i * 0x100000000 + 1;
                tasks[i] = Task.Factory.StartNew(
                    () => RunWorker(threadSeed, durationMs, cancellationToken),
                    cancellationToken,
                    TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
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
            // AboveNormal 优先级：工作线程优先获取 CPU 时间，但保留一个核心给 UI/采样线程
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            Thread.CurrentThread.IsBackground = true;

            // 使用 Stopwatch 计时，避免 Environment.TickCount 的 int 溢出问题
            var sw = Stopwatch.StartNew();
            ulong state = seed;
            long iterations = 0;

            // 热循环 —— 纯 ALU 运算，零内存分配，单核跑满
            while (sw.ElapsedMilliseconds < durationMs && !cancellationToken.IsCancellationRequested)
            {
                // xorshift64 步进：数据依赖链，计算值影响后续所有步
                state ^= state << 13;
                state ^= state >> 7;
                state ^= state << 17;

                // 将计算结果融入迭代计数，确保 JIT 无法消除计算
                iterations += (long)(state & 0xFF);
                iterations++;
            }

            return iterations;
        }
    }
}
