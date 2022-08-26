using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace RyzenTuner.Common.EnergyStar.Interop
{
    internal class Win32Api
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
            [In] PROCESS_INFORMATION_CLASS ProcessInformationClass, IntPtr ProcessInformation,
            uint ProcessInformationSize);

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

        // two message box releated constants
        public const int MB_OK = 0x00000000;
        public const int MB_ICONERROR = 0x00000010;

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        /// <summary>
        /// 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/ne-processthreadsapi-process_information_class
        /// </summary>
        public enum PROCESS_INFORMATION_CLASS
        {
            ProcessMemoryPriority,
            ProcessMemoryExhaustionInfo,
            ProcessAppMemoryInfo,
            ProcessInPrivateInfo,

            // The process information is represented by a PROCESS_POWER_THROTTLING_STATE structure.
            // Allows applications to configure how the system should throttle the target process’s activity when managing power.
            ProcessPowerThrottling,
            ProcessReservedValue1,
            ProcessTelemetryCoverageInfo,
            ProcessProtectionLevelInfo,
            ProcessLeapSecondInfo,
            ProcessInformationClassMax,
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
            PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1,
        }

        /// <summary>
        /// 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setpriorityclass
        /// </summary>
        public enum PriorityClass : uint
        {
            // 【实时】具有最高优先级的进程。该进程的线程抢占所有其他进程的线程，包括执行重要任务的操作系统进程
            REALTIME_PRIORITY_CLASS = 0x100,

            // 【高】执行必须立即执行的时间关键任务的流程
            // 在使用高优先级类时要格外小心，因为高优先级类应用程序几乎可以使用所有可用的CPU时间。
            HIGH_PRIORITY_CLASS = 0x80,

            // 【高于正常】优先级高于普通优先级但低于高优先级的进程
            ABOVE_NORMAL_PRIORITY_CLASS = 0x8000,

            // 【正常】Process with no special scheduling needs
            NORMAL_PRIORITY_CLASS = 0x00000020,

            // 【低于正常】优先级高于空闲优先级但低于正常优先级的进程
            BELOW_NORMAL_PRIORITY_CLASS = 0x4000,

            // 【低】仅在系统空闲时才运行线程的进程。进程的线程会被运行在更高优先级类中的任何进程的线程抢占。
            // 屏幕保护程序就是一个例子。空闲优先级类由子进程继承
            IDLE_PRIORITY_CLASS = 0x00000040,

            // 开始后台处理模式。系统降低了进程(及其线程)的资源调度优先级，以便它可以在不显著影响前台活动的情况下执行后台工作
            // PROCESS_MODE_BACKGROUND_BEGIN 不适合用于低优先级的后台工作
            // 参考：https://stackoverflow.com/questions/13631644/setthreadpriority-and-setpriorityclass
            PROCESS_MODE_BACKGROUND_BEGIN = 0x100000, // Windows Vista/2008 and higher

            // 结束后台处理模式。系统恢复进程(及其线程)进入后台处理模式前的资源调度优先级
            PROCESS_MODE_BACKGROUND_END = 0x200000, //   Windows Vista/2008 and higher
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