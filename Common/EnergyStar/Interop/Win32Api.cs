using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace RyzenTuner.Common.EnergyStar.Interop
{
    internal static class Win32Api
    {
        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] int dwFlags,
            [Out] StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessInformation([In] IntPtr hProcess,
            [In] ProcessInformationClass processInformationClass, IntPtr processInformation,
            uint processInformationSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetPriorityClass(IntPtr handle, PriorityClass priorityClass);

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

        // We don't need to bloat this app with WinForm/WPF to show a simple message box
        [DllImport("user32.dll")]
        public static extern int MessageBox(IntPtr hInstance, string lpText, string lpCaption, uint type);

        // two message box related constants
        public const int MbOk = 0x00000000;
        public const int MbIconError = 0x00000010;

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            SetInformation = 0x00000200,
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
        /// 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setpriorityclass
        /// </summary>
        public enum PriorityClass : uint
        {
            // 【正常】Process with no special scheduling needs
            NormalPriorityClass = 0x00000020,

            // 【低于正常】优先级高于空闲优先级但低于正常优先级的进程
            BelowNormalPriorityClass = 0x4000,
        }

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