using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RyzenTuner.Common.EnergyStar.Interop;

namespace RyzenTuner.Common.EnergyStar
{
    public class EnergyManager
    {
        private static readonly HashSet<string> BypassProcessList = new HashSet<string>
        {
            // Visual Studio
            "devenv.exe",
        };

        // Special handling needs for UWP to get the child window process
        private const string UwpFrameHostApp = "ApplicationFrameHost.exe";

        private static uint _pendingProcPid = 0;
        private static string _pendingProcName = "";

        private static readonly IntPtr PThrottleOn;
        private static readonly IntPtr PThrottleOff;
        private static readonly int SzControlBlock = 0;

        static EnergyManager()
        {
            // TODO：Setting 类没有实现，暂时屏蔽
            // var settings = Settings.Load();
            // BypassProcessList.UnionWith(settings.Exemptions.Select(x => x.ToLowerInvariant()));

            SzControlBlock = Marshal.SizeOf<Win32Api.ProcessPowerThrottlingState>();
            PThrottleOn = Marshal.AllocHGlobal(SzControlBlock);
            PThrottleOff = Marshal.AllocHGlobal(SzControlBlock);

            var throttleState = new Win32Api.ProcessPowerThrottlingState
            {
                Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
                ControlMask = Win32Api.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = Win32Api.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            };

            var unthrottleState = new Win32Api.ProcessPowerThrottlingState
            {
                Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
                ControlMask = Win32Api.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = Win32Api.ProcessorPowerThrottlingFlags.None,
            };

            Marshal.StructureToPtr(throttleState, PThrottleOn, false);
            Marshal.StructureToPtr(unthrottleState, PThrottleOff, false);
        }

        private static void ToggleEfficiencyMode(IntPtr hProcess, bool enable)
        {
            Win32Api.SetProcessInformation(hProcess, Win32Api.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                enable ? PThrottleOn : PThrottleOff, (uint)SzControlBlock);
            Win32Api.SetPriorityClass(hProcess,
                enable ? Win32Api.PriorityClass.IDLE_PRIORITY_CLASS : Win32Api.PriorityClass.NORMAL_PRIORITY_CLASS);
        }

        private static string GetProcessNameFromHandle(IntPtr hProcess)
        {
            var capacity = 1024;
            var sb = new StringBuilder(capacity);

            if (Win32Api.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
            {
                return Path.GetFileName(sb.ToString());
            }

            return "";
        }

        public static void HandleForegroundEvent(IntPtr hwnd)
        {
            var windowThreadId = Win32Api.GetWindowThreadProcessId(hwnd, out var procId);
            
            // This is invalid, likely a process is dead, or idk
            if (windowThreadId == 0 || procId == 0) return;

            var procHandle = Win32Api.OpenProcess(
                (uint)(Win32Api.ProcessAccessFlags.QueryLimitedInformation |
                       Win32Api.ProcessAccessFlags.SetInformation), false, procId);
            if (procHandle == IntPtr.Zero) return;

            // Get the process
            var appName = GetProcessNameFromHandle(procHandle);

            // UWP needs to be handled in a special case
            if (appName == UwpFrameHostApp)
            {
                var found = false;
                Win32Api.EnumChildWindows(hwnd, (innerHwnd, lparam) =>
                {
                    if (found) return true;
                    if (Win32Api.GetWindowThreadProcessId(innerHwnd, out uint innerProcId) > 0)
                    {
                        if (procId == innerProcId) return true;

                        var innerProcHandle = Win32Api.OpenProcess(
                            (uint)(Win32Api.ProcessAccessFlags.QueryLimitedInformation |
                                   Win32Api.ProcessAccessFlags.SetInformation), false, innerProcId);
                        if (innerProcHandle == IntPtr.Zero) return true;

                        // Found. Set flag, reinitialize handles and call it a day
                        found = true;
                        Win32Api.CloseHandle(procHandle);
                        procHandle = innerProcHandle;
                        procId = innerProcId;
                        appName = GetProcessNameFromHandle(procHandle);
                    }

                    return true;
                }, IntPtr.Zero);
            }

            // Boost the current foreground app, and then impose EcoQoS for previous foreground app
            var bypass = BypassProcessList.Contains(appName.ToLowerInvariant());
            if (!bypass)
            {
                Console.WriteLine($"Boost {appName}");
                ToggleEfficiencyMode(procHandle, false);
            }

            if (_pendingProcPid != 0)
            {
                Console.WriteLine($"Throttle {_pendingProcName}");

                var prevProcHandle = Win32Api.OpenProcess((uint)Win32Api.ProcessAccessFlags.SetInformation, false,
                    _pendingProcPid);
                if (prevProcHandle != IntPtr.Zero)
                {
                    ToggleEfficiencyMode(prevProcHandle, true);
                    Win32Api.CloseHandle(prevProcHandle);
                    _pendingProcPid = 0;
                    _pendingProcName = "";
                }
            }

            if (!bypass)
            {
                _pendingProcPid = procId;
                _pendingProcName = appName;
            }

            Win32Api.CloseHandle(procHandle);
        }

        public static void ThrottleAllUserBackgroundProcesses()
        {
            var runningProcesses = Process.GetProcesses();
            var currentSessionId = Process.GetCurrentProcess().SessionId;

            var sameAsThisSession = runningProcesses.Where(p => p.SessionId == currentSessionId);
            foreach (var proc in sameAsThisSession)
            {
                if (proc.Id == _pendingProcPid)
                {
                    continue;
                }

                if (BypassProcessList.Contains($"{proc.ProcessName}.exe".ToLowerInvariant())) continue;
                var hProcess = Win32Api.OpenProcess((uint)Win32Api.ProcessAccessFlags.SetInformation, false,
                    (uint)proc.Id);
                ToggleEfficiencyMode(hProcess, true);
                Win32Api.CloseHandle(hProcess);
            }
        }
    }
}