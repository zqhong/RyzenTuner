using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RyzenTuner.Common.EnergyStar.Interop
{
    /// <summary>
    /// Win32 API P/Invoke 声明。
    /// 包含进程管理、窗口枚举、电源节流等底层系统调用。
    /// </summary>
    internal static class Win32Api
    {
        /// <summary>
        /// 获取指定进程的进程 ID。
        /// </summary>
        /// <param name="processHandle">进程句柄。</param>
        /// <returns>进程 ID。</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetProcessId([In] IntPtr processHandle);

        /// <summary>
        /// 获取指定进程的完整可执行文件路径。
        /// </summary>
        /// <param name="processHandle">进程句柄，需具有 PROCESS_QUERY_LIMITED_INFORMATION 权限。</param>
        /// <param name="flags">0 表示使用本机格式（Win32 路径），PROCESS_NAME_WIN32 (1) 表示 Win32 路径格式。</param>
        /// <param name="exeName">接收路径字符串的缓冲区。</param>
        /// <param name="size">输入时为缓冲区大小（字符数），输出时为路径长度（字符数，含 null 终止符）。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true,
            EntryPoint = "QueryFullProcessImageNameW")]
        public static extern bool QueryFullProcessImageName([In] IntPtr processHandle, uint flags,
            [Out] StringBuilder exeName, ref uint size);

        /// <summary>
        /// 获取当前前台窗口的句柄。
        /// </summary>
        /// <returns>前台窗口的 HWND，若无前台窗口则返回 IntPtr.Zero。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// 获取指定窗口的线程 ID 和创建该窗口的进程 ID。
        /// </summary>
        /// <param name="windowHandle">窗口句柄。</param>
        /// <param name="processId">输出参数，接收进程 ID。</param>
        /// <returns>创建窗口的线程 ID。</returns>
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId([In] IntPtr windowHandle, out uint processId);

        /// <summary>
        /// 打开一个已存在的进程对象。
        /// </summary>
        /// <param name="processAccess">进程访问权限标志。</param>
        /// <param name="inheritHandle">返回的句柄是否可被子进程继承。</param>
        /// <param name="processId">要打开的进程 ID。</param>
        /// <returns>进程句柄，失败返回 IntPtr.Zero。</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool inheritHandle,
            uint processId);

        /// <summary>
        /// 设置指定进程的信息（如电源节流策略）。
        /// </summary>
        /// <param name="processHandle">进程句柄，需具有 PROCESS_SET_INFORMATION 权限。</param>
        /// <param name="processInformationClass">要设置的信息类。</param>
        /// <param name="processInformation">指向信息数据的指针。</param>
        /// <param name="processInformationSize">信息数据的大小（字节）。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessInformation([In] IntPtr processHandle,
            ProcessInformationClass processInformationClass, [In] IntPtr processInformation,
            uint processInformationSize);

        /// <summary>
        /// 设置进程的优先级类。
        /// </summary>
        /// <param name="processHandle">进程句柄。</param>
        /// <param name="priorityClass">优先级类。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass([In] IntPtr processHandle, PriorityClass priorityClass);

        /// <summary>
        /// 关闭一个打开的对象句柄。
        /// </summary>
        /// <param name="handle">要关闭的句柄。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle([In] IntPtr handle);

        /// <summary>
        /// 窗口枚举回调委托，由 EnumChildWindows 调用。
        /// </summary>
        /// <param name="handle">子窗口句柄。</param>
        /// <param name="param">应用程序定义的值。</param>
        /// <returns>true 继续枚举，false 停止枚举。</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool WindowEnumProc([In] IntPtr handle, [In] IntPtr param);

        /// <summary>
        /// 枚举指定父窗口的所有子窗口。
        /// </summary>
        /// <param name="windowHandle">父窗口句柄。</param>
        /// <param name="callback">回调函数，对每个子窗口调用。</param>
        /// <param name="param">应用程序定义的值。</param>
        /// <returns>成功返回 true，失败返回 false。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumChildWindows([In] IntPtr windowHandle, WindowEnumProc callback,
            [In] IntPtr param);

        /// <summary>
        /// 进程访问权限标志，用于 OpenProcess。
        /// </summary>
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            /// <summary>设置进程信息所需的访问权限。</summary>
            SetInformation = 0x00000200,

            /// <summary>查询受限进程信息所需的访问权限。</summary>
            QueryLimitedInformation = 0x00001000,
        }

        /// <summary>
        /// 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/ne-processthreadsapi-process_information_class
        /// </summary>
        public enum ProcessInformationClass
        {
            /// <summary>The process information is represented by a PROCESS_POWER_THROTTLING_STATE structure. Allows applications to configure how the system should throttle the target process's activity when managing power.</summary>
            ProcessPowerThrottling = 4,
        }

        /// <summary>
        /// 进程电源节流标志。
        /// 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
        /// </summary>
        [Flags]
        public enum ProcessPowerThrottlingFlags : uint
        {
            None = 0x0,

            // 当一个进程设置为 PROCESS_POWER_THROTTLING_EXECUTION_SPEED 时，该进程将被分类为 EcoQoS
            // 系统通过对 EcoQoS 进程降低 CPU 频率或使用更多高能效的内核等操作来提高电源效率
            // 在 Windows 11 之前，EcoQoS 级别并不存在，进程被标记为 LowQoS
            //      LowQoS：在电池模式下，选择最有效的CPU频率和调度到高效核心。1709 以后版本可用。
            //      EcoQos：总是选择最有效的CPU频率，并调度到高效的核心。Windows 11 以后版本可用。
            // 参考：
            //      https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
            //      https://docs.microsoft.com/en-us/windows/win32/procthread/quality-of-service
            ProcessPowerThrottlingExecutionSpeed = 0x1
        }

        /// <summary>
        /// 进程优先级类。
        /// 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setpriorityclass
        /// </summary>
        public enum PriorityClass : uint
        {
            // 【正常】Process with no special scheduling needs
            NormalPriorityClass = 0x00000020,

            // 【低于正常】优先级高于空闲优先级但低于正常优先级的进程
            BelowNormalPriorityClass = 0x4000,
        }

        /// <summary>
        /// 进程电源节流状态结构。
        /// 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-process_power_throttling_state
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessPowerThrottlingState
        {
            public const uint ProcessPowerThrottlingCurrentVersion = 1;

            public uint Version;
            public ProcessPowerThrottlingFlags ControlMask;
            public ProcessPowerThrottlingFlags StateMask;
        }
    }
}
