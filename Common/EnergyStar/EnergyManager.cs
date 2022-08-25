﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using RyzenTuner.Common.Container;
using RyzenTuner.Common.EnergyStar.Interop;
using RyzenTuner.Properties;

namespace RyzenTuner.Common.EnergyStar
{
    public class EnergyManager
    {
        private const string UnknownProcessName = "Unknown-K7Ncy4PUIQBNyGTl.exe";

        private readonly HashSet<string> _bypassProcessList = new()
        {
            // Edge 浏览器会自动调度
            "msedge.exe",
            "WebViewHost.exe",

            // UWP Frame 需要特殊处理
            "ApplicationFrameHost.exe",

            // 监控相关
            "taskmgr.exe",
            "procmon.exe",
            "procmon64.exe",
            "perfmon.exe",

            // Widgets
            "Widgets.exe",

            // 系统进程
            "explorer.exe",
            "ntoskrnl.exe",
            "WerFault.exe",
            "backgroundTaskHost.exe",
            "backgroundTransferHost.exe",
            "winlogon.exe",
            "wininit.exe",
            "csrss.exe",
            "lsass.exe",
            "smss.exe",
            "services.exe",
            "taskeng.exe",
            "taskhost.exe",
            "dwm.exe",
            "conhost.exe",
            "svchost.exe",
            "sihost.exe",
            "ShellExperienceHost.exe",
            "StartMenuExperienceHost.exe",
            "SearchHost.exe",
            "fontdrvhost.exe",
            "logonui.exe",
            "LockApp.exe",
            "WUDFHost.exe", // Windows User-Mode Driver Framework Host

            // 输入法
            "ChsIME.exe",
            "ctfmon.exe",

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
            "RyzenTuner.exe",
        };

        // Special handling needs for UWP to get the child window process
        // UWP Application Frame Host
        private const string UwpFrameHostApp = "ApplicationFrameHost.exe";

        private readonly IntPtr _pThrottleOn;
        private readonly IntPtr _pThrottleOff;
        private readonly int _szControlBlock;

        public EnergyManager()
        {
            var bypassSetting =
                Settings.Default.EnergyStarBypassProcessList
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());
            _bypassProcessList.UnionWith(bypassSetting);

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
        /// <param name="processId"></param>
        /// <param name="enable"></param>
        private void ToggleEfficiencyMode(int processId, bool enable)
        {
            var logger = AppContainer.Logger();

            try
            {
                var appName = GetProcessNameFromPid(processId);
                switch (appName)
                {
                    case UnknownProcessName:
                        logger.Warning($"ToggleEfficiencyMode: 获取进程名称失败。pid: {processId}");
                        return;
                    case UwpFrameHostApp:
                        logger.Debug($"ToggleEfficiencyMode: 暂不处理 UwpFrameHostApp");
                        return;
                }

                if (_bypassProcessList.Contains(appName.ToLower()))
                {
                    logger.Debug($"ToggleEfficiencyMode: 不处理白名单列表中的应用{appName}");
                    return;
                }

                var procHandle = Win32Api.OpenProcess(
                    (uint)(Win32Api.ProcessAccessFlags.QueryLimitedInformation |
                           Win32Api.ProcessAccessFlags.SetInformation), false, (uint)processId);

                var r1 = Win32Api.SetProcessInformation(procHandle,
                    Win32Api.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                    enable ? _pThrottleOn : _pThrottleOff, (uint)_szControlBlock);

                var r2 = Win32Api.SetPriorityClass(procHandle,
                    enable ? Win32Api.PriorityClass.IDLE_PRIORITY_CLASS : Win32Api.PriorityClass.NORMAL_PRIORITY_CLASS);

                using (null)
                {
                    var actionText = "Boost";
                    if (enable)
                    {
                        actionText = "Throttle";
                    }


                    logger.Debug(
                        $"{actionText} {appName}. SetProcessInformation result: {r1}, SetPriorityClass result: {r2}");
                }

                Win32Api.CloseHandle(procHandle);
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
        }

        /// <summary>
        /// 通过进程的 ID 获取进程的名称
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        private string GetProcessNameFromPid(int pid)
        {
            try
            {
                var p = Process.GetProcessById(pid);

                if (p.MainModule != null)
                {
                    return Path.GetFileName(p.MainModule.FileName);
                }

                return UnknownProcessName;
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
                return UnknownProcessName;
            }
        }


        /// <summary>
        /// 处理前台进程
        /// </summary>
        public void HandleForeground()
        {
            var logger = AppContainer.Logger();

            try
            {
                var handleToWindow = Win32Api.GetForegroundWindow();

                if (handleToWindow == IntPtr.Zero)
                {
                    return;
                }

                var windowThreadId = Win32Api.GetWindowThreadProcessId(handleToWindow, out var processId);
                // This is invalid, likely a process is dead, or idk
                if (windowThreadId == 0 || processId == 0)
                {
                    return;
                }

                ToggleEfficiencyMode((int)processId, false);
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
        }

        /// <summary>
        /// 将除了 PendingProcPid 和 BypassProcessList 外的所有进程，都切换到【效率模式】
        /// </summary>
        public void ThrottleAllUserBackgroundProcesses()
        {
            try
            {
                AppContainer.Logger().Debug("Throttle All User Background Processes");

                var runningProcesses = Process.GetProcesses();
                var currentSessionId = Process.GetCurrentProcess().SessionId;

                var sameAsThisSession = runningProcesses.Where(p => p.SessionId == currentSessionId);

                foreach (var proc in sameAsThisSession)
                {
                    ToggleEfficiencyMode(proc.Id, true);
                }
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
        }
    }
}