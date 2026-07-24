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
        /// <summary>
        /// 测试类型（单核 / 多核）
        /// </summary>
        public BenchmarkTestType TestType { get; set; }

        private float _startTdp;
        private float _stepTdp = 5f;
        private float _endTdp;
        private int _durationSeconds = 60;
        private int _restSeconds = 5;

        /// <summary>
        /// 起始功耗（W）
        /// </summary>
        public float StartTdp
        {
            get => _startTdp;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "StartTdp must be >= 0");
                _startTdp = value;
            }
        }

        /// <summary>
        /// 步进（W）
        /// </summary>
        public float StepTdp
        {
            get => _stepTdp;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "StepTdp must be > 0");
                _stepTdp = value;
            }
        }

        /// <summary>
        /// 结束功耗（W）
        /// </summary>
        public float EndTdp
        {
            get => _endTdp;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "EndTdp must be >= 0");
                _endTdp = value;
            }
        }

        /// <summary>
        /// 每档测试时间（秒）
        /// </summary>
        public int DurationSeconds
        {
            get => _durationSeconds;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "DurationSeconds must be > 0");
                _durationSeconds = value;
            }
        }

        /// <summary>
        /// 切换功率后每档测试前的休息时间（秒）
        /// </summary>
        public int RestSeconds
        {
            get => _restSeconds;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "RestSeconds must be >= 0");
                _restSeconds = value;
            }
        }

        /// <summary>
        /// 测试点总数
        /// </summary>
        public int TestPointCount
        {
            get
            {
                // 捕获本地快照避免 torn read，与 GetTdpAtIndex 保持一致。
                // 实际使用中 BenchmarkEngine 在启动跑分时一次性读取此值，
                // 跑分期间应避免修改配置，因此不影响正确性。
                var startTdp = _startTdp;
                var stepTdp = _stepTdp;
                var endTdp = _endTdp;
                return ComputeTestPointCount(startTdp, stepTdp, endTdp);
            }
        }

        /// <summary>
        /// 计算测试点总数（使用 decimal 运算 + Math.Floor 避免 float 精度导致的 off-by-one 错误）
        ///
        /// Math.Floor 向下取整以确保不会生成超过 EndTdp 的测试点。
        /// 例如：startTdp=0, endTdp=3.5, stepTdp=1 → (3.5-0)/1=3.5 → Floor(3.5)=3 → 4 个测试点（0,1,2,3）。
        /// 若使用 Math.Round 则 banker's rounding 会在中点值处向上取整，导致生成越界的测试点（4 > 3.5）。
        /// </summary>
        private static int ComputeTestPointCount(float startTdp, float stepTdp, float endTdp)
        {
            if (stepTdp <= 0 || endTdp < startTdp)
                return 0;

            return (int)Math.Floor(((decimal)endTdp - (decimal)startTdp) / (decimal)stepTdp) + 1;
        }

        /// <summary>
        /// 获取指定索引的 TDP 值
        /// </summary>
        public float GetTdpAtIndex(int index)
        {
            // 捕获本地快照，避免 UI 线程修改导致 TOCTOU 竞争
            var startTdp = _startTdp;
            var stepTdp = _stepTdp;
            var endTdp = _endTdp;

            var count = ComputeTestPointCount(startTdp, stepTdp, endTdp);
            if (count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    "No test points available (invalid StartTdp/EndTdp/StepTdp configuration)");
            }

            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"Index must be between 0 and {count - 1}");

            return (float)((decimal)startTdp + (decimal)index * (decimal)stepTdp);
        }
    }

    /// <summary>
    /// 单档测试结果
    /// </summary>
    public class BenchmarkTestPoint
    {
        private const float MinPowerThreshold = 0.01f;

        /// <summary>设定功耗（W）</summary>
        private float _setTdp;

        public float SetTdp
        {
            get => _setTdp;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "SetTdp must be >= 0 and a finite value");
                _setTdp = value;
            }
        }

        /// <summary>跑分成绩（迭代次数）</summary>
        private long _score;

        public long Score
        {
            get => _score;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Score must be >= 0");
                _score = value;
            }
        }

        /// <summary>缩放后的分数（用于显示，原始 Score 保留给 Efficiency 计算）</summary>
        private long _scaledScore;

        public long ScaledScore
        {
            get => _scaledScore;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "ScaledScore must be >= 0");
                _scaledScore = value;
            }
        }

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
        public double Efficiency
        {
            get
            {
                // 捕获局部变量保证一致性读取，避免并发写入导致 PowerAvg 被读取两次不同值
                var avg = PowerAvg;
                var score = Score;
                return avg > MinPowerThreshold ? (double)score / avg : 0;
            }
        }

        private double _capability;

        /// <summary>能力发挥 = 当前分数 / 所有测试点最高分（0~1），在全部测试完成后计算</summary>
        public double Capability
        {
            get => _capability;
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0 || value > 1.0)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Capability must be a finite value between 0 and 1");
                _capability = value;
            }
        }

    }
}
