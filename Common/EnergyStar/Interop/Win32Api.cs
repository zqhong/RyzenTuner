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
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetProcessId([In] IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags,
            [Out] StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle,
            uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessInformation([In] IntPtr hProcess,
            [In] ProcessInformationClass processInformationClass, IntPtr processInformation,
            uint processInformationSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass([In] IntPtr handle, PriorityClass priorityClass);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// 窗口枚举回调委托，由 EnumChildWindows 调用。
        /// </summary>
        /// <param name="hwnd">子窗口句柄。</param>
        /// <param name="lparam">应用程序定义的值。</param>
        /// <returns>true 继续枚举，false 停止枚举。</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

        // We don’t need to bloat this app with WinForm/WPF to show a simple message box
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint type);

        // two message box related constants
        public const int MbOk = 0x00000000;
        public const int MbIconError = 0x00000010;

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
            // The process information is represented by a PROCESS_POWER_THROTTLING_STATE structure.
            // Allows applications to configure how the system should throttle the target process’s activity when managing power.
            ProcessPowerThrottling,
        }

        /// <summary>
        /// 进程电源节流标志。
        /// 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
        /// </summary>
        [Flags]
        public enum ProcessorPowerThrottlingFlags : uint
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
            public ProcessorPowerThrottlingFlags ControlMask;
            public ProcessorPowerThrottlingFlags StateMask;
        }
    }
}