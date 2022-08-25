using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using RyzenTuner.Common.Container;
using RyzenTuner.Common.EnergyStar.Interop;

namespace RyzenTuner.Common.EnergyStar
{
    public class EnergyManager
    {
        private const string UnknownProcessName = "Unknown-K7Ncy4PUIQBNyGTl.exe";

        private HashSet<string> _bypassProcessList = new()
        {
            // Edge has energy awareness
            "msedge.exe",
            "WebViewHost.exe",

            // UWP Frame has special handling, should not be throttled
            "ApplicationFrameHost.exe",

            // Fire extinguisher should not catch fire
            "taskmgr.exe",
            "procmon.exe",
            "procmon64.exe",

            // Widgets
            "Widgets.exe",

            // System shell
            "dwm.exe",
            "explorer.exe",
            "ShellExperienceHost.exe",
            "StartMenuExperienceHost.exe",
            "SearchHost.exe",
            "sihost.exe",
            "fontdrvhost.exe",

            // IME
            "ChsIME.exe",
            "ctfmon.exe",

            // System Service - they have their awareness
            "csrss.exe",
            "smss.exe",
            "svchost.exe",

            // WUDF
            "WUDFRd.exe",

            // Vmware Workstation
            "vmware-vmx.exe",

            // 编辑器
            "Brackets.exe",
            "Code.exe",
            "atom.exe",
            "sublime_text.exe",
            "notepad++.exe",

            // IDE
            "clion.exe",
            "clion64.exe",
            "idea.exe",
            "idea64.exe",
            "phpstorm.exe",
            "phpstorm64.exe",
            "pycharm.exe",
            "pycharm64.exe",
            "rubymine.exe",
            "rubymine64.exe",
            "webstorm.exe",
            "webstorm64.exe",
            "rider.exe",
            "rider64.exe",
            "goland.exe",
            "goland64.exe",
            "datagrip.exe",
            "datagrip64.exe",

            // 其他开发相关
            "bash.exe",
            "zsh.exe",
        };

        // Special handling needs for UWP to get the child window process
        // UWP Application Frame Host
        private const string UwpFrameHostApp = "ApplicationFrameHost.exe";

        private uint _pendingProcPid;
        private string _pendingProcName = "";

        private readonly IntPtr _pThrottleOn;
        private readonly IntPtr _pThrottleOff;
        private readonly int _szControlBlock;

        public EnergyManager()
        {
            // TODO：Setting 类没有实现，暂时屏蔽
            // var settings = Settings.Load();
            // BypassProcessList.UnionWith(settings.Exemptions.Select(x => x.ToLowerInvariant()));

            // 将 _bypassProcessList 的元素全部替代为小写字符串
            foreach (var processName in _bypassProcessList.ToArray())
            {
                var lowerProcessName = processName.ToLower();
                if (processName == lowerProcessName)
                {
                    continue;
                }

                _bypassProcessList.Remove(processName);
                _bypassProcessList.Add(lowerProcessName);
            }

            _szControlBlock = Marshal.SizeOf<Win32Api.ProcessPowerThrottlingState>();
            _pThrottleOn = Marshal.AllocHGlobal(_szControlBlock);
            _pThrottleOff = Marshal.AllocHGlobal(_szControlBlock);

            // 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
            // EcoQoS：打开 ExecutionSpeed 节流功能
            var throttleState = new Win32Api.ProcessPowerThrottlingState
            {
                Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
                ControlMask = Win32Api.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = Win32Api.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            };

            // HighQoS：关闭 ExecutionSpeed 节流
            var unThrottleState = new Win32Api.ProcessPowerThrottlingState
            {
                Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
                ControlMask = Win32Api.ProcessorPowerThrottlingFlags.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = Win32Api.ProcessorPowerThrottlingFlags.None,
            };

            // 如果想要让系统管理所有的功率节流，ControlMask 和 StateMask 设置为 Win32Api.ProcessorPowerThrottlingFlags.None
            // var defaultThrottleState = new Win32Api.ProcessPowerThrottlingState
            // {
            //     Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
            //     ControlMask = Win32Api.ProcessorPowerThrottlingFlags.None,
            //     StateMask = Win32Api.ProcessorPowerThrottlingFlags.None,
            // };

            Marshal.StructureToPtr(throttleState, _pThrottleOn, false);
            Marshal.StructureToPtr(unThrottleState, _pThrottleOff, false);
        }

        /// <summary>
        /// 如果 enable 为 true，则切换到【效率模式】（低优先级；低频率；高效核）；否则，则是普通模式。
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="enable"></param>
        private void ToggleEfficiencyMode(IntPtr hProcess, bool enable)
        {
            try
            {
                Win32Api.SetProcessInformation(hProcess, Win32Api.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                    enable ? _pThrottleOn : _pThrottleOff, (uint)_szControlBlock);
                Win32Api.SetPriorityClass(hProcess,
                    enable ? Win32Api.PriorityClass.IDLE_PRIORITY_CLASS : Win32Api.PriorityClass.NORMAL_PRIORITY_CLASS);
            }
            catch (Exception e)
            {
                AppContainer.Logger().Warning(e.Message);
            }
        }

        private Process? GetForegroundProcess()
        {
            try
            {
                var hwnd = Win32Api.GetForegroundWindow();

                // The foreground window can be NULL in certain circumstances, 
                // such as when a window is losing activation.
                if (hwnd == IntPtr.Zero)
                {
                    return null;
                }

                Win32Api.GetWindowThreadProcessId(hwnd, out var pid);

                return Process.GetProcessById((int)pid);
            }
            catch (Exception e)
            {
                AppContainer.Logger().Warning(e.Message);
                return null;
            }
        }

        public string GetForegroundProcessName()
        {
            try
            {
                var p = GetForegroundProcess();
                if (p != null && p.MainModule != null)
                {
                    return Path.GetFileName(p.MainModule.FileName);
                }

                return UnknownProcessName;
            }
            catch (Exception e)
            {
                AppContainer.Logger().Warning(e.Message);
                return UnknownProcessName;
            }
        }

        // private string GetProcessNameFromHandle(IntPtr hProcess)
        // {
        //     try
        //     {
        //         var capacity = 1024;
        //         var sb = new StringBuilder(capacity);
        //
        //         if (Win32Api.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
        //         {
        //             return Path.GetFileName(sb.ToString());
        //         }
        //
        //         return "";
        //     }
        //     catch (Exception e)
        //     {
        //         AppContainer.Logger().Warning(e.Message);
        //         return "";
        //     }
        // }

        private string GetProcessNameFromHandleV2(IntPtr hProcess)
        {
            try
            {
                if (hProcess == IntPtr.Zero)
                {
                    return UnknownProcessName;
                }

                Win32Api.GetWindowThreadProcessId(hProcess, out var pid);
                var p = Process.GetProcessById((int)pid);

                if (p != null && p.MainModule != null)
                {
                    return Path.GetFileName(p.MainModule.FileName);
                }

                return UnknownProcessName;
            }
            catch (Exception e)
            {
                AppContainer.Logger().Warning(e.Message);
                return UnknownProcessName;
            }
        }


        /// <summary>
        /// 处理当前的前台进程
        /// </summary>
        public void HandleForeground()
        {
            try
            {
                var p = GetForegroundProcess();
                if (p == null)
                {
                    return;
                }

                _handleForeground(p.Handle);
            }
            catch (Exception e)
            {
                AppContainer.Logger().Warning(e.Message);
            }
        }

        /// <summary>
        /// 处理前台进程
        /// </summary>
        /// <param name="hwnd"></param>
        private void _handleForeground(IntPtr hwnd)
        {
            var logger = AppContainer.Logger();
            var windowThreadId = Win32Api.GetWindowThreadProcessId(hwnd, out var procId);

            // This is invalid, likely a process is dead, or idk
            if (windowThreadId == 0 || procId == 0) return;

            var procHandle = Win32Api.OpenProcess(
                (uint)(Win32Api.ProcessAccessFlags.QueryLimitedInformation |
                       Win32Api.ProcessAccessFlags.SetInformation), false, procId);
            if (procHandle == IntPtr.Zero) return;

            // Get the process
            var appName = GetProcessNameFromHandleV2(procHandle);

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
                        appName = GetProcessNameFromHandleV2(procHandle);
                    }

                    return true;
                }, IntPtr.Zero);
            }

            // Boost the current foreground app, and then impose EcoQoS for previous foreground app
            var bypass = _bypassProcessList.Contains(appName.ToLower());
            if (!bypass)
            {
                logger.Debug($"Boost {appName}");
                ToggleEfficiencyMode(procHandle, false);
            }

            if (_pendingProcPid != 0)
            {
                logger.Debug($"Throttle {_pendingProcName}");

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

        /// <summary>
        /// 将除了 PendingProcPid 和 BypassProcessList 外的所有进程，都切换到【效率模式】
        /// </summary>
        public void ThrottleAllUserBackgroundProcesses()
        {
            try
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

                    if (_bypassProcessList.Contains($"{proc.ProcessName}.exe".ToLower()))
                    {
                        continue;
                    }

                    var hProcess = Win32Api.OpenProcess((uint)Win32Api.ProcessAccessFlags.SetInformation, false,
                        (uint)proc.Id);
                    ToggleEfficiencyMode(hProcess, true);
                    Win32Api.CloseHandle(hProcess);
                }
            }
            catch (Exception e)
            {
                AppContainer.Logger().Warning(e.Message);
            }
        }
    }
}