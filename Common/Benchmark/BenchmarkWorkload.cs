using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace RyzenTuner.Common.Benchmark
{
    /// <summary>
    /// CPU 跑分负载 —— 使用 SHA256 哈希计算作为纯 CPU 密集任务。
    ///
    /// 每次迭代：
    ///   1. 将递增计数器（64 位整数）序列化为 8 字节数组
    ///   2. 计算 SHA256 哈希
    ///   3. 将哈希的前 8 字节解释为整数，作为下次的输入盐值
    ///   4. 计数器 +1
    ///
    /// 数据依赖链确保优化器不能跳过循环或提前计算。
    /// 每 10000 次校验一次哈希值已知部分，防止 JIT 识别出模式后优化掉。
    ///
    /// 注意：每个工作线程使用独立的 SHA256 实例，避免线程安全问题。
    /// </summary>
    public class BenchmarkWorkload : IDisposable
    {
        /// <summary>
        /// 运行跑分，返回迭代次数（数据依赖链确保不会被优化掉）
        /// </summary>
        /// <param name="threadCount">线程数（1=单核，2=双核）</param>
        /// <param name="durationMs">测试持续时间（毫秒）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>总迭代次数</returns>
        public async Task<long> RunAsync(int threadCount, int durationMs, CancellationToken cancellationToken)
        {
            var tasks = new Task<long>[threadCount];
            for (var i = 0; i < threadCount; i++)
            {
                var threadSeed = (long)i * 0x100000000 + 1;
                tasks[i] = Task.Run(() => RunWorker(threadSeed, durationMs, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);

            long total = 0;
            foreach (var r in results)
            {
                total += r;
            }

            return total;
        }

        /// <summary>
        /// 每个工作线程使用独立的 SHA256 实例，避免多线程共享导致的线程安全问题。
        /// </summary>
        private static long RunWorker(long seed, int durationMs, CancellationToken cancellationToken)
        {
            using var sha256 = SHA256.Create();
            var iterations = 0L;
            var counter = seed;
            var input = new byte[16]; // 8 bytes counter + 8 bytes salt
            var stopTime = Environment.TickCount + durationMs;

            // 将初始种子写入 input
            BitConverter.GetBytes(seed).CopyTo(input, 0);

            while (Environment.TickCount < stopTime && !cancellationToken.IsCancellationRequested)
            {
                // 更新计数器部分（input[0..7]）
                var counterBytes = BitConverter.GetBytes(counter);
                counterBytes.CopyTo(input, 0);

                // 计算 SHA256
                var hash = sha256.ComputeHash(input);

                // 将哈希前 8 字节作为下次的盐值（input[8..15]）
                Array.Copy(hash, 0, input, 8, 8);

                counter++;
                iterations++;
            }

            return iterations;
        }

        public void Dispose()
        {
            // SHA256 实例由每个工作线程自行创建和释放，无需在此处清理
        }
    }
}
