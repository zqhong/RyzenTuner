using System;
using System.Runtime.InteropServices;

namespace RyzenTuner.Common.Processor
{
    /// <summary>
    /// AMD Ryzen CPU 家族枚举。
    /// 参考：https://github.com/FlyGoat/RyzenAdj/blob/master/lib/ryzenadj.h
    /// </summary>
    public enum RyzenFamily
    {
        FamUnknown = -1,
        FamRaven = 0,
        FamPicasso,
        FamRenoir,
        FamCezanne,
        FamDali,
        FamLucienne,
        FamVangogh,
        FamRembrandt,
        FamMendocino,
        FamPhoenix,
        FamHawkPoint,
        FamDragonRange,
        FamKrackanPoint,
        FamStrixPoint,
        FamStrixHalo,
        FamFireRange
    }

    /// <summary>
    /// RyzenAdj 错误码。
    /// 参考：https://github.com/FlyGoat/RyzenAdj/blob/master/lib/ryzenadj.h
    /// </summary>
    public enum ErrCode
    {
        AdjErrNone = 0,
        AdjErrFamilyUnsupported = -1,
        AdjErrSmuTimeout = -2,
        AdjErrSmuUnsupported = -3,
        AdjErrSmuRejected = -4,
        AdjErrMemoryAccess = -5,
    }

    /// <summary>
    /// libryzenadj.dll P/Invoke 声明。
    /// </summary>
    /// <remarks>
    /// 参考：https://github.com/Valkirie/ControllerService/blob/main/ControllerCommon/Processor/AMD/RyzenAdj.cs
    /// 依赖：inpoutx64.dll, libryzenadj.dll, WinRing0x64.dll, WinRing0x64.sys
    /// </remarks>
    public static class RyzenAdj
    {
        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern IntPtr init_ryzenadj();

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern void cleanup_ryzenadj(IntPtr ry);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern RyzenFamily get_cpu_family(IntPtr ry);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern uint get_table_ver(IntPtr ry);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern int refresh_table(IntPtr ry);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern int set_stapm_limit(IntPtr ry, uint value);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern int set_fast_limit(IntPtr ry, uint value);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern int set_slow_limit(IntPtr ry, uint value);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern int set_tctl_temp(IntPtr ry, uint value);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern int set_apu_skin_temp_limit(IntPtr ry, uint value);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern float get_stapm_limit(IntPtr ry);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern float get_fast_limit(IntPtr ry);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern float get_slow_limit(IntPtr ry);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern float get_tctl_temp(IntPtr ry);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern float get_apu_skin_temp_limit(IntPtr ry);

        [DllImport("libryzenadj.dll", ExactSpelling = true)]
        public static extern float get_apu_skin_temp_value(IntPtr ry);
    }
}
