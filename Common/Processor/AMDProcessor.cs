using System;

namespace RyzenTuner.Common.Processor
{
    public enum PowerType
    {
        // long
        Slow = 0,
        Stapm = 1,
        Fast = 2,
    }

    // 错误码，参考：https://github.com/FlyGoat/RyzenAdj/blob/master/lib/ryzenadj.h#L52-L56
    public enum ErrCode
    {
        AdjErrNone = 0,
        AdjErrFamUnsupported = -1,
        AdjErrSmuTimeout = -2,
        AdjErrSmuUnsupported = -3,
        AdjErrSmuRejected = -4,
        AdjErrMemoryAccess = -5,
    }

    public class AmdProcessor
    {
        private readonly IntPtr _ry;
        private readonly bool _canChangeTdp;

        public AmdProcessor()
        {
            _ry = RyzenAdj.init_ryzenadj();
            if (_ry == IntPtr.Zero)
            {
                throw new Exception("init ryzenadj failed");
            }

            var family = RyzenAdj.get_cpu_family(_ry);

            switch (family)
            {
                case RyzenFamily.FamUnknown:
                case RyzenFamily.FamEnd:
                default:
                    _canChangeTdp = false;
                    break;

                case RyzenFamily.FamRaven:
                case RyzenFamily.FamPicasso:
                case RyzenFamily.FamDali:
                case RyzenFamily.FamRenoir:
                case RyzenFamily.FamLucienne:
                case RyzenFamily.FamCezanne:
                case RyzenFamily.FamVangogh:
                case RyzenFamily.FamRembrandt:
                    _canChangeTdp = true;
                    break;
            }
        }

        /**
         * 可能返回 NaN
         */
        public float GetStampLimit()
        {
            return RyzenAdj.get_stapm_limit(_ry);
        }

        public bool SetAllTdpLimit(double limit)
        {
            var r1 = SetTdpLimit(PowerType.Slow, limit);
            var r2 = SetTdpLimit(PowerType.Stapm, limit);
            var r3 = SetTdpLimit(PowerType.Fast, limit);

            return r1 && r2 && r3;
        }

        public bool SetTctlTemp(uint temp)
        {
            var result = RyzenAdj.set_tctl_temp(_ry, temp);
            return result == (int)ErrCode.AdjErrNone;
        }

        private bool SetTdpLimit(PowerType type, double limit)
        {
            if (!_canChangeTdp)
            {
                return false;
            }

            // 15W : 15000
            limit *= 1000;

            var result = type switch
            {
                PowerType.Fast => RyzenAdj.set_fast_limit(_ry, (uint)limit),
                PowerType.Slow => RyzenAdj.set_slow_limit(_ry, (uint)limit),
                PowerType.Stapm => RyzenAdj.set_stapm_limit(_ry, (uint)limit),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            return result == (int)ErrCode.AdjErrNone;
        }
    }
}