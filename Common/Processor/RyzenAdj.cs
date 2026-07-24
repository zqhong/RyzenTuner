using System;
using System.Runtime.InteropServices;

namespace RyzenTuner.Common.Processor
{
    /// <summary>
    /// AMD Ryzen CPU 家族枚举。
    /// 参考：https://github.com/FlyGoat/RyzenAdj/blob/master/lib/ryzenadj.h
    /// 注意：必须使用显式值并与原生 enum ryzen_family 保持同步。
    /// 原生枚举按 0 开始自动递增（FAM_UNKNOWN = -1 除外）。
    /// 如果原生库在现有成员之间插入新家族，此处必须同步更新数值。
    /// </summary>
    public enum RyzenFamily
    {
        /// <summary>未知 CPU 家族（尚未完成初始化）。</summary>
        FamUnknown = -1,
        /// <summary>Raven Ridge (Ryzen 2000 series APU, GFX 8)。</summary>
        FamRaven = 0,
        /// <summary>Picasso (Ryzen 3000 series APU, GFX 9)。</summary>
        FamPicasso = 1,
        /// <summary>Renoir (Ryzen 4000 series APU, GFX 9)。</summary>
        FamRenoir = 2,
        /// <summary>Cezanne (Ryzen 5000 series APU, GFX 9)。</summary>
        FamCezanne = 3,
        /// <summary>Dali / Pollock (Ryzen 3000系列的入门级APU)。</summary>
        FamDali = 4,
        /// <summary>Lucienne (Ryzen 5000 series APU, Zen 2)。</summary>
        FamLucienne = 5,
        /// <summary>Vangogh (Steam Deck 使用的 APU, Zen 2 + RDNA 2)。</summary>
        FamVangogh = 6,
        /// <summary>Rembrandt (Ryzen 6000 series APU, RDNA 2)。</summary>
        FamRembrandt = 7,
        /// <summary>Mendocino (Ryzen 7020 series APU, Zen 2)。</summary>
        FamMendocino = 8,
        /// <summary>Phoenix (Ryzen 7040 series APU, Zen 4 + RDNA 3)。</summary>
        FamPhoenix = 9,
        /// <summary>Hawk Point (Ryzen 8040 series APU, Zen 4 + RDNA 3)。</summary>
        FamHawkPoint = 10,
        /// <summary>Dragon Range (Ryzen 7045 series HX, Zen 4 + RDNA 2)。</summary>
        FamDragonRange = 11,
        /// <summary>Krackan Point (新一代 APU)。</summary>
        FamKrackanPoint = 12,
        /// <summary>Strix Point (Ryzen AI 300 series, Zen 5 + RDNA 3.5)。</summary>
        FamStrixPoint = 13,
        /// <summary>Strix Halo (面向高端笔记本、Zen 5 + RDNA 3.5)。</summary>
        FamStrixHalo = 14,
        /// <summary>Fire Range (Ryzen 9000 series HX, Zen 5)。</summary>
        FamFireRange = 15,
    }

    /// <summary>
    /// RyzenAdj 错误码。
    /// 参考：https://github.com/FlyGoat/RyzenAdj/blob/master/lib/ryzenadj.h
    /// </summary>
    public enum ErrCode
    {
        /// <summary>成功，无错误。</summary>
        AdjErrNone = 0,
        /// <summary>CPU 家族不受当前版本的 ryzenadj 支持。</summary>
        AdjErrFamilyUnsupported = -1,
        /// <summary>SMU 命令超时 — CPU 未在规定时间内响应 SMU 请求。</summary>
        AdjErrSmuTimeout = -2,
        /// <summary>当前 CPU 家族不支持该 SMU 命令。</summary>
        AdjErrSmuUnsupported = -3,
        /// <summary>SMU 拒绝了该命令（参数超出范围等）。</summary>
        AdjErrSmuRejected = -4,
        /// <summary>内存访问错误 — 无法读取/写入 SMU 寄存器空间。</summary>
        AdjErrMemoryAccess = -5,
    }

    /// <summary>
    /// libryzenadj.dll P/Invoke 声明。
    /// </summary>
    /// <remarks>
    /// 参考：https://github.com/Valkirie/ControllerService/blob/main/ControllerCommon/Processor/AMD/RyzenAdj.cs
    /// 依赖：inpoutx64.dll, libryzenadj.dll, WinRing0x64.dll, WinRing0x64.sys
    /// <para/>
    /// 线程安全：libryzenadj 内部状态非线程安全。调用方必须保证所有 P/Invoke 调用
    /// 使用相同的 <c>IntPtr ry</c> 句柄时做同步（例如锁保护）。
    /// 所有 <c>IntPtr ry</c> 参数必须是由 <see cref="init_ryzenadj"/> 返回的有效句柄，
    /// 不得为 <see cref="IntPtr.Zero"/>，否则会导致原生代码访问违例。
    /// </remarks>
    public static class RyzenAdj
    {
        /// <summary>
        /// 初始化 ryzenadj 库，返回 SMU 通信句柄。
        /// 等效于原生 API init_ryzenadj()。
        /// </summary>
        /// <returns>SMU 通信句柄。若初始化失败（例如驱动未加载），返回 <see cref="IntPtr.Zero"/>。</returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern IntPtr init_ryzenadj();

        /// <summary>
        /// 释放 ryzenadj 句柄并清理内部状态。
        /// 等效于原生 API cleanup_ryzenadj()。
        /// </summary>
        /// <param name="ry">由 <see cref="init_ryzenadj"/> 返回的有效句柄。</param>
        /// <remarks>
        /// 注意：与标准 C free() 不同，传入 <see cref="IntPtr.Zero"/> 的行为是未定义的。
        /// 调用方必须在调用前检查句柄非零。
        /// </remarks>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern void cleanup_ryzenadj(IntPtr ry);

        /// <summary>
        /// 获取 CPU 家族类型。
        /// 等效于原生 API get_cpu_family()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <returns>
        /// <see cref="RyzenFamily"/> 枚举值。若返回 <see cref="RyzenFamily.FamUnknown"/>，
        /// 表示 CPU 不受支持或句柄无效。
        /// </returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern RyzenFamily get_cpu_family(IntPtr ry);

        /// <summary>
        /// 获取 SMU 表版本号（用于兼容性判断）。
        /// 等效于原生 API get_table_ver()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <returns>SMU 表版本号（格式为打包 BCD，例如 0x00460004）。</returns>
        /// <remarks>
        /// 首次调用会触发 SMU 内部表数据填充（warming up），
        /// <see cref="RyzenTuner.Common.Processor.AmdProcessor"/> 构造函数中有意调用此方法来初始化 SMU 状态。
        /// </remarks>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern uint get_table_ver(IntPtr ry);

        /// <summary>
        /// 刷新 SMU 内部表，使后续 get_* 读取到最新的 SMU 寄存器值。
        /// 等效于原生 API refresh_table()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <returns>成功返回 0（<see cref="ErrCode.AdjErrNone"/>），失败返回负值错误码。</returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern ErrCode refresh_table(IntPtr ry);

        /// <summary>
        /// 设置 STAPM（Smart Thermal Average Power Management）限制值。
        /// 等效于原生 API set_stapm_limit()。
        /// STAPM 是 SoC 层面的持续功耗限制，影响 CPU 和核显的整体功耗上限。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <param name="value">STAPM 限制值（单位：毫瓦 mW，1W = 1000mW）。</param>
        /// <returns>成功返回 <see cref="ErrCode.AdjErrNone"/>，失败返回其他错误码。</returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern ErrCode set_stapm_limit(IntPtr ry, uint value);

        /// <summary>
        /// 设置 Fast PPT（Package Power Tracking）限制值 — 短时突发功耗上限。
        /// 等效于原生 API set_fast_limit()。
        /// Fast PPT 是短时突发功耗上限，仅允许在短时间内维持。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <param name="value">Fast PPT 限制值（单位：毫瓦 mW，1W = 1000mW）。</param>
        /// <returns>成功返回 <see cref="ErrCode.AdjErrNone"/>，失败返回其他错误码。</returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern ErrCode set_fast_limit(IntPtr ry, uint value);

        /// <summary>
        /// 设置 Slow PPT（Package Power Tracking）限制值 — 持续功耗上限。
        /// 等效于原生 API set_slow_limit()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <param name="value">Slow PPT 限制值（单位：毫瓦 mW，1W = 1000mW）。</param>
        /// <returns>成功返回 <see cref="ErrCode.AdjErrNone"/>，失败返回其他错误码。</returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern ErrCode set_slow_limit(IntPtr ry, uint value);

        /// <summary>
        /// 设置 Tctl/Tdie 温度上限。当 CPU 温度达到该值时，SMU 会主动降频以控制温度。
        /// 等效于原生 API set_tctl_temp()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <param name="value">温度上限值（单位：摄氏度 °C，1°C = 1 单位，例如 95 = 95°C）。</param>
        /// <returns>成功返回 <see cref="ErrCode.AdjErrNone"/>，失败返回其他错误码。</returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern ErrCode set_tctl_temp(IntPtr ry, uint value);

        /// <summary>
        /// 设置 APU 皮肤温度限制值。
        /// 等效于原生 API set_apu_skin_temp_limit()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <param name="value">温度上限值（单位：摄氏度 °C。注意：library 内部会乘以 256 转为 Q8.8 格式写入 SMU）。</param>
        /// <returns>成功返回 <see cref="ErrCode.AdjErrNone"/>，失败返回其他错误码。</returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern ErrCode set_apu_skin_temp_limit(IntPtr ry, uint value);

        /// <summary>
        /// 获取当前 STAPM 限制设定值（从 SMU 寄存器读取）。
        /// 等效于原生 API get_stapm_limit()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <returns>
        /// STAPM 限制值（单位：毫瓦 mW）。如果读取失败或在不受支持的 CPU 上，返回 <see cref="float.NaN"/>。
        /// </returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern float get_stapm_limit(IntPtr ry);

        /// <summary>
        /// 获取 Fast PPT 限制设定值（从 SMU 寄存器读取）。
        /// 等效于原生 API get_fast_limit()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <returns>
        /// Fast PPT 限制值（单位：毫瓦 mW）。如果读取失败或在不受支持的 CPU 上，返回 <see cref="float.NaN"/>。
        /// </returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern float get_fast_limit(IntPtr ry);

        /// <summary>
        /// 获取 Slow PPT 限制设定值（从 SMU 寄存器读取）。
        /// 等效于原生 API get_slow_limit()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <returns>
        /// Slow PPT 限制值（单位：毫瓦 mW）。如果读取失败或在不受支持的 CPU 上，返回 <see cref="float.NaN"/>。
        /// </returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern float get_slow_limit(IntPtr ry);

        /// <summary>
        /// 获取 Tctl/Tdie 当前温度测量值（从 SMU 寄存器读取）。
        /// 等效于原生 API get_tctl_temp()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <returns>
        /// 当前温度值（单位：摄氏度 °C）。如果读取失败或在不受支持的 CPU 上，返回 <see cref="float.NaN"/>。
        /// </returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern float get_tctl_temp(IntPtr ry);

        /// <summary>
        /// 获取 APU 皮肤温度限制设定值（从 SMU 寄存器读取）。
        /// 等效于原生 API get_apu_skin_temp_limit()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <returns>
        /// APU 皮肤温度限制值（SMU 原始值，单位：摄氏度 × 256。使用时应除以 256 还原为摄氏度）。
        /// 如果读取失败或在不受支持的 CPU 上，返回 <see cref="float.NaN"/>。
        /// </returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern float get_apu_skin_temp_limit(IntPtr ry);

        /// <summary>
        /// 获取 APU 皮肤温度当前测量值（从 SMU 寄存器读取）。
        /// 等效于原生 API get_apu_skin_temp_value()。
        /// </summary>
        /// <param name="ry">SMU 通信句柄。</param>
        /// <returns>
        /// APU 皮肤温度当前值（SMU 原始值，单位：摄氏度 × 256。使用时应除以 256 还原为摄氏度）。
        /// 如果读取失败或在不受支持的 CPU 上，返回 <see cref="float.NaN"/>。
        /// </returns>
        [DllImport("libryzenadj.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static extern float get_apu_skin_temp_value(IntPtr ry);
    }
}
