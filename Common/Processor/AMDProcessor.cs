using System;
using RyzenTuner.Common.Container;

namespace RyzenTuner.Common.Processor
{
    public class AmdProcessor : IDisposable
    {
        public enum PowerType
        {
            // long
            Slow = 0,
            Stapm = 1,
            Fast = 2,
        }
        
        private IntPtr _ry;
        private readonly bool _canChangeTdp;
        private readonly RyzenFamily _family;

        public bool CanChangeTdp => _canChangeTdp;
        public RyzenFamily CpuFamily => _family;

        public void Dispose()
        {
            CleanupRy();
        }

        public AmdProcessor()
        {
            try
            {
                _ry = RyzenAdj.init_ryzenadj();
                if (_ry == IntPtr.Zero)
                {
                    throw new Exception(Properties.Strings.TextRyzenAdjInitFailed);
                }

                _ = RyzenAdj.get_table_ver(_ry);

                _family = RyzenAdj.get_cpu_family(_ry);

                switch (_family)
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
                    case RyzenFamily.FamMendocino:
                    case RyzenFamily.FamPhoenix:
                    case RyzenFamily.FamHawkPoint:
                    case RyzenFamily.FamDragonRange:
                    case RyzenFamily.FamKrackanPoint:
                    case RyzenFamily.FamStrixPoint:
                    case RyzenFamily.FamStrixHalo:
                    case RyzenFamily.FamFireRange:
                        _canChangeTdp = true;
                        break;
                }

            }
            catch (DllNotFoundException ex)
            {
                CleanupRy();
                throw new Exception(Properties.Strings.TextLibRyzenAdjLoadFailed.Replace("{message}", ex.Message), ex);
            }
            catch (BadImageFormatException ex)
            {
                CleanupRy();
                throw new Exception(Properties.Strings.TextLibRyzenAdjArchitectureMismatch, ex);
            }
            catch (EntryPointNotFoundException ex)
            {
                CleanupRy();
                throw new Exception(Properties.Strings.TextLibRyzenAdjTooOld, ex);
            }
            catch (Exception ex)
            {
                CleanupRy();
                throw new Exception(Properties.Strings.TextRyzenAdjInitFailedWithMessage.Replace("{message}", ex.Message), ex);
            }
        }

        private void CleanupRy()
        {
            if (_ry != IntPtr.Zero)
            {
                RyzenAdj.cleanup_ryzenadj(_ry);
                _ry = IntPtr.Zero;
            }
        }
        
        public string CpuFamilyName => _family switch
        {
            RyzenFamily.FamUnknown => "Unknown",
            RyzenFamily.FamRaven => "Raven (2000/3000 series)",
            RyzenFamily.FamPicasso => "Picasso (3000 series)",
            RyzenFamily.FamRenoir => "Renoir (4000 series)",
            RyzenFamily.FamCezanne => "Cezanne (5000 series)",
            RyzenFamily.FamDali => "Dali",
            RyzenFamily.FamLucienne => "Lucienne (5000 series)",
            RyzenFamily.FamVangogh => "Vangogh",
            RyzenFamily.FamRembrandt => "Rembrandt (6000 series)",
            RyzenFamily.FamMendocino => "Mendocino",
            RyzenFamily.FamPhoenix => "Phoenix (7000 series)",
            RyzenFamily.FamHawkPoint => "Hawk Point (8000 series)",
            RyzenFamily.FamDragonRange => "Dragon Range (7045 series)",
            RyzenFamily.FamKrackanPoint => "Krackan Point",
            RyzenFamily.FamStrixPoint => "Strix Point (AI 300 series)",
            RyzenFamily.FamStrixHalo => "Strix Halo",
            RyzenFamily.FamFireRange => "Fire Range (9000 series)",
            _ => "Unknown",
        };

        /// <summary>
        /// 返回默认的 Stapm Limit（读取 SMU 寄存器的设定值）
        ///
        /// 备注：
        /// 该方法在部分 CPU 下会返回 NaN
        /// </summary>
        public float GetStampLimit()
        {
            return RyzenAdj.get_stapm_limit(_ry);
        }

        /// <summary>
        /// 返回 Fast Limit（读取 SMU 寄存器的设定值）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetFastLimit()
        {
            return RyzenAdj.get_fast_limit(_ry);
        }

        /// <summary>
        /// 返回 Slow Limit（读取 SMU 寄存器的设定值）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetSlowLimit()
        {
            return RyzenAdj.get_slow_limit(_ry);
        }

        /// <summary>
        /// 返回 Tctl Temp 限制（读取 SMU 寄存器的设定值）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetTctlTempLimit()
        {
            return RyzenAdj.get_tctl_temp(_ry);
        }

        /// <summary>
        /// 刷新 SMU 内部表数据，使后续 get_* 调用读取到最新值。
        /// 等效于 ryzenadj --info 中的 refresh_table 步骤。
        /// </summary>
        public void RefreshTable()
        {
            RyzenAdj.refresh_table(_ry);
        }

        /// <summary>
        /// 返回 Fast PPT 当前实时值（SMU 测量的动态功耗）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetFastValue()
        {
            return RyzenAdj.get_fast_value(_ry);
        }

        /// <summary>
        /// 返回 Slow PPT 当前实时值（SMU 测量的动态功耗）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetSlowValue()
        {
            return RyzenAdj.get_slow_value(_ry);
        }

        /// <summary>
        /// 返回 Stapm Limit 当前实时值（SMU 测量的动态功耗）
        /// 部分 CPU 下会返回 NaN
        /// </summary>
        public float GetStampValue()
        {
            return RyzenAdj.get_stapm_value(_ry);
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
