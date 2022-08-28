using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        private readonly IntPtr _pThrottleOn;
        private readonly IntPtr _pThrottleOff;
        private readonly int _szControlBlock;

        public const string UwpFrameHostApp = "ApplicationFrameHost.exe";

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
        /// <param name="hProcess"></param>
        /// <param name="enable"></param>
        private void ToggleEfficiencyMode(IntPtr hProcess, bool enable)
        {
            var logger = AppContainer.Logger();

            try
            {
                if (hProcess == IntPtr.Zero)
                {
                    return;
                }

                var processId = Win32Api.GetProcessId(hProcess);
                var appName = GetProcessNameFromHandle(hProcess);

                if (appName == UnknownProcessName)
                {
                    logger.Warning($"ToggleEfficiencyMode: 获取进程名称失败。pid: {processId}");
                    return;
                }

                if (_bypassProcessList.Contains(appName.ToLower()))
                {
                    logger.Debug($"ToggleEfficiencyMode: 不处理白名单列表中的应用 {appName}");
                    return;
                }

                var r1 = Win32Api.SetProcessInformation(hProcess,
                    Win32Api.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                    enable ? _pThrottleOn : _pThrottleOff, (uint)_szControlBlock);

                var r2 = Win32Api.SetPriorityClass(hProcess,
                    enable
                        ? Win32Api.PriorityClass.BELOW_NORMAL_PRIORITY_CLASS
                        : Win32Api.PriorityClass.NORMAL_PRIORITY_CLASS);

                using (null)
                {
                    var actionText = "Boost";
                    if (enable)
                    {
                        actionText = "Throttle";
                    }


                    logger.Debug(
                        $"{actionText} {appName}. pid: {processId}, set process information and priority result: {r1 && r2}");
                }
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
        }

        /// <summary>
        /// 通过 hProcess 获取进程名称
        /// </summary>
        /// <param name="hProcess"></param>
        /// <returns></returns>
        private string GetProcessNameFromHandle(IntPtr hProcess)
        {
            try
            {
                var capacity = 2048;
                var sb = new StringBuilder(capacity);

                if (Win32Api.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
                {
                    return Path.GetFileName(sb.ToString());
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
            try
            {
                var hwnd = Win32Api.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;

                var windowThreadId = Win32Api.GetWindowThreadProcessId(hwnd, out var processId);
                if (windowThreadId == 0 || processId == 0) return;

                var processHandle = NativeOpenProcess((int)processId);
                if (processHandle == IntPtr.Zero) return;

                var appName = GetProcessNameFromHandle(processHandle);
                if (appName == UwpFrameHostApp)
                {
                    var found = false;
                    Win32Api.EnumChildWindows(hwnd, (innerHwnd, lParam) =>
                    {
                        if (found) return true;
                        if (Win32Api.GetWindowThreadProcessId(innerHwnd, out var innerProcId) <= 0) return true;
                        if (processId == innerProcId) return true;

                        var innerProcHandle = NativeOpenProcess((int)innerProcId);
                        if (innerProcHandle == IntPtr.Zero) return true;

                        // Found. Set flag, reinitialize handles and call it a day
                        found = true;
                        Win32Api.CloseHandle(processHandle);
                        processHandle = innerProcHandle;
                        processId = innerProcId;
                        appName = GetProcessNameFromHandle(processHandle);

                        return true;
                    }, IntPtr.Zero);
                }


                ToggleEfficiencyMode(processHandle, false);

                Win32Api.CloseHandle(processHandle);
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
        }

        /// <summary>
        /// 使用统一的 process access 参数调用 Win32Api.OpenProcess
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        private IntPtr NativeOpenProcess(int processId)
        {
            const uint processAccess = (uint)(
                Win32Api.ProcessAccessFlags.QueryLimitedInformation |
                Win32Api.ProcessAccessFlags.SetInformation
            );
            return Win32Api.OpenProcess(processAccess, false, (uint)processId);
        }

        /// <summary>
        /// 将除了 PendingProcPid 和 BypassProcessList 外的所有进程，都切换到【效率模式】
        /// </summary>
        public void ThrottleAllUserBackgroundProcesses()
        {
            AppContainer.Logger().Debug("Throttle All User Background Processes");
            _toggleAllBgProcessesMode(true);
        }

        /// <summary>
        /// 将除了 PendingProcPid 和 BypassProcessList 外的所有进程，都切换到【普通模式】
        /// </summary>
        public void BoostAllUserBackgroundProcesses()
        {
            AppContainer.Logger().Debug("Boost All User Background Processes");
            _toggleAllBgProcessesMode(false);
        }

        private void _toggleAllBgProcessesMode(bool enable)
        {
            try
            {
                var runningProcesses = Process.GetProcesses();
                var currentSessionId = Process.GetCurrentProcess().SessionId;

                var sameAsThisSession = runningProcesses.Where(p => p.SessionId == currentSessionId);

                foreach (var proc in sameAsThisSession)
                {
                    try
                    {
                        var hProcess = NativeOpenProcess(proc.Id);
                        ToggleEfficiencyMode(hProcess, enable);
                        Win32Api.CloseHandle(hProcess);
                    }
                    catch (Exception e)
                    {
                        AppContainer.Logger().LogException(e);
                    }
                }
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
        }
    }
}