using System;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Common.Processor
{
    public class AmdProcessor
    {
        public enum PowerType
        {
            // long
            Slow = 0,
            Stapm = 1,
            Fast = 2,
        }
        
        private readonly IntPtr _ry;
        private readonly bool _canChangeTdp;

        public AmdProcessor()
        {
            try
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
            catch (DllNotFoundException ex)
            {
                throw new Exception($"Failed to load libryzenadj.dll: {ex.Message}", ex);
            }
            catch (BadImageFormatException ex)
            {
                throw new Exception($"libryzenadj.dll architecture mismatch (32/64-bit?)", ex);
            }
            catch (Exception ex) 
            {
                throw new Exception($"init ryzenadj failed: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 返回 stamp limit 数据。
        ///
        /// 备注：
        /// 该方法在部分 CPU 下会返回 NULL
        /// 
        /// 说明：
        /// 测试处理器： AMD Ryzen™ 7 PRO 6850HS
        /// 测试 RyzenAdj 方法：get_tctl_temp、get_fast_limit、get_slow_limit、get_stapm_limit
        /// 返回结果：NaN
        /// 
        /// </summary>
        /// <returns></returns>
        public float GetStampLimit()
        {
            return RyzenAdj.get_stapm_limit(_ry);
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

            // 例如：15W : 15000 mW
            limit *= 1000;
            
            var result = type switch
            {
                PowerType.Fast => RyzenAdj.set_fast_limit(_ry, (uint)limit),
                PowerType.Slow => RyzenAdj.set_slow_limit(_ry, (uint)limit),
                PowerType.Stapm => RyzenAdj.set_stapm_limit(_ry, (uint)limit),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            
            AppContainer.Logger().Debug($"AMDProcessor.SetTdpLimit: type {type}, limit: {(uint)limit}, result: {result}");

            return result == (int)ErrCode.AdjErrNone;
        }

        public bool SetFastPPT(double limit)
        {
            return SetTdpLimit(PowerType.Fast, limit);
        }
        
        public bool SetSlowPPT(double limit)
        {
            return SetTdpLimit(PowerType.Slow, limit);
        }
        
        public bool SetStampPPT(double limit)
        {
            return SetTdpLimit(PowerType.Stapm, limit);
        }
    }
}