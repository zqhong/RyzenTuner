using System;

namespace RyzenTuner.Common.Benchmark
{
    /// <summary>
    /// 跑分类型
    /// </summary>
    public enum BenchmarkTestType
    {
        SingleCore,
        MultiCore,
    }

    /// <summary>
    /// 跑分配置参数
    /// </summary>
    public class BenchmarkConfig
    {
        private const double FloatingPointTolerance = 1e-6;

        /// <summary>
        /// 测试类型（单核 / 多核）
        /// </summary>
        public BenchmarkTestType TestType { get; set; }

        /// <summary>
        /// 起始功耗（W）
        /// </summary>
        public float StartTdp { get; set; }

        /// <summary>
        /// 步进（W）
        /// </summary>
        public float StepTdp { get; set; }

        /// <summary>
        /// 结束功耗（W）
        /// </summary>
        public float EndTdp { get; set; }

        /// <summary>
        /// 每档测试时间（秒）
        /// </summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// 切换功率后每档测试前的休息时间（秒）
        /// </summary>
        public int RestSeconds { get; set; } = 5;

        /// <summary>
        /// 测试点总数
        /// </summary>
        public int TestPointCount
        {
            get
            {
                if (StepTdp <= 0 || EndTdp < StartTdp)
                    return 0;

                // 加 epsilon 避免浮点精度导致的 off-by-one 错误
                return (int)Math.Floor((EndTdp - StartTdp) / StepTdp + FloatingPointTolerance) + 1;
            }
        }

        /// <summary>
        /// 获取指定索引的 TDP 值
        /// </summary>
        public float GetTdpAtIndex(int index)
        {
            if (index < 0 || index >= TestPointCount)
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"Index must be between 0 and {TestPointCount - 1}");

            return StartTdp + index * StepTdp;
        }
    }

    /// <summary>
    /// 单档测试结果
    /// </summary>
    public class BenchmarkTestPoint
    {
        private const float MinPowerThreshold = 0.01f;

        /// <summary>设定功耗（W）</summary>
        public float SetTdp { get; set; }

        /// <summary>跑分成绩（迭代次数）</summary>
        public long Score { get; set; }

        /// <summary>缩放后的分数（用于显示，原始 Score 保留给 Efficiency 计算）</summary>
        public long ScaledScore { get; set; }

        // ---- 实际功耗统计 ----
        public float PowerMin { get; set; }
        public float PowerMax { get; set; }
        public float PowerAvg { get; set; }
        public float PowerMedian { get; set; }

        // ---- CPU 温度统计 ----
        public float TempMin { get; set; }
        public float TempMax { get; set; }
        public float TempAvg { get; set; }
        public float TempMedian { get; set; }

        /// <summary>CPU 频率平均值（MHz）</summary>
        public float CpuFreqAvg { get; set; }

        /// <summary>能效比 = Score / PowerAvg（使用 double 精度，避免大 Score 值时的精度损失）</summary>
        public double Efficiency => PowerAvg > MinPowerThreshold ? (double)Score / PowerAvg : 0;

        private double _capability;

        /// <summary>能力发挥 = 当前分数 / 所有测试点最高分（0~1），在全部测试完成后计算</summary>
        public double Capability
        {
            get => _capability;
            set
            {
                if (value < 0.0 || value > 1.0)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Capability must be between 0 and 1");
                _capability = value;
            }
        }

    }
}
