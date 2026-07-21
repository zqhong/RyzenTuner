using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RyzenTuner.Common.Container;
using RyzenTuner.Common.EnergyStar.Interop;
using RyzenTuner.Common.Settings;

namespace RyzenTuner.Common.EnergyStar
{
    public class EnergyManager
        : IDisposable
    {
        private const string UnknownProcessName = "Unknown-K7Ncy4PUIQBNyGTl.exe";
        private const string UwpFrameHostApp = "ApplicationFrameHost.exe";
        private const int MaxProcessPathChars = 2048;

        private static readonly HashSet<string> _hardcodedBypassList = new(StringComparer.OrdinalIgnoreCase)
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

        private readonly HashSet<string> _bypassProcessList = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _bypassLock = new();

        private readonly IntPtr _pThrottleOn;
        private readonly IntPtr _pThrottleOff;
        private readonly int _szControlBlock;
        private bool _disposed;

        public EnergyManager()
        {
            _bypassProcessList.UnionWith(_hardcodedBypassList);
            _bypassProcessList.UnionWith(LoadBypassSetting());

            _szControlBlock = Marshal.SizeOf<Win32Api.ProcessPowerThrottlingState>();
            _pThrottleOn = Marshal.AllocHGlobal(_szControlBlock);
            _pThrottleOff = Marshal.AllocHGlobal(_szControlBlock);

            // 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
            // EcoQoS：打开 ExecutionSpeed 节流功能
            var throttleState = new Win32Api.ProcessPowerThrottlingState
            {
                Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
                ControlMask = Win32Api.ProcessorPowerThrottlingFlags.ProcessPowerThrottlingExecutionSpeed,
                StateMask = Win32Api.ProcessorPowerThrottlingFlags.ProcessPowerThrottlingExecutionSpeed,
            };

            // HighQoS：关闭 ExecutionSpeed 节流
            var unThrottleState = new Win32Api.ProcessPowerThrottlingState
            {
                Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
                ControlMask = Win32Api.ProcessorPowerThrottlingFlags.ProcessPowerThrottlingExecutionSpeed,
                StateMask = Win32Api.ProcessorPowerThrottlingFlags.None,
            };

            // 如果想要让系统管理所有的功率节流，ControlMask 和 StateMask 设置为 Win32Api.ProcessorPowerThrottlingFlags.None
            // var defaultThrottleState = new Win32Api.ProcessPowerThrottlingState
            // {
            //     Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
            //     ControlMask = Win32Api.ProcessorPowerThrottlingFlags.None,
            //     StateMask = Win32Api.ProcessorPowerThrottlingFlags.None,
            // };

            try
            {
                Marshal.StructureToPtr(throttleState, _pThrottleOn, false);
                Marshal.StructureToPtr(unThrottleState, _pThrottleOff, false);
            }
            catch
            {
                DisposeCore();
                throw;
            }
        }

        ~EnergyManager()
        {
            DisposeCore();
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

                if (IsInBypassList(appName))
                {
                    logger.Debug($"ToggleEfficiencyMode: 不处理白名单列表中的应用 {appName}");
                    return;
                }

                var r1 = Win32Api.SetProcessInformation(hProcess,
                    Win32Api.ProcessInformationClass.ProcessPowerThrottling,
                    enable ? _pThrottleOn : _pThrottleOff, (uint)_szControlBlock);

                var r2 = Win32Api.SetPriorityClass(hProcess,
                    enable
                        ? Win32Api.PriorityClass.BelowNormalPriorityClass
                        : Win32Api.PriorityClass.NormalPriorityClass);

                var actionText = enable ? "Throttle" : "Boost";
                logger.Debug(
                    $"{actionText} {appName}. pid: {processId}, set process information and priority result: {r1 && r2}");
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
        private static string GetProcessNameFromHandle(IntPtr hProcess)
        {
            try
            {
                var capacity = MaxProcessPathChars;
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
            IntPtr processHandle = IntPtr.Zero;

            try
            {
                var hwnd = Win32Api.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;

                if (Win32Api.GetWindowThreadProcessId(hwnd, out var processId) == 0 || processId == 0) return;

                processHandle = NativeOpenProcess((int)processId);
                if (processHandle == IntPtr.Zero) return;

                var appName = GetProcessNameFromHandle(processHandle);
                if (appName == UwpFrameHostApp)
                {
                    var found = false;
                    var enumResult = Win32Api.EnumChildWindows(hwnd, (innerHwnd, _) =>
                    {
                        if (found) return true;
                        if (Win32Api.GetWindowThreadProcessId(innerHwnd, out var innerProcId) <= 0) return true;
                        if (processId == innerProcId) return true;

                        var innerProcHandle = NativeOpenProcess((int)innerProcId);
                        if (innerProcHandle == IntPtr.Zero) return true;

                        try
                        {
                            var innerAppName = GetProcessNameFromHandle(innerProcHandle);

                            // Found. Set flag, reinitialize handles and call it a day
                            found = true;
                            Win32Api.CloseHandle(processHandle);
                            processHandle = innerProcHandle;
                            processId = innerProcId;
                            appName = innerAppName;
                            innerProcHandle = IntPtr.Zero;
                        }
                        finally
                        {
                            if (innerProcHandle != IntPtr.Zero)
                            {
                                Win32Api.CloseHandle(innerProcHandle);
                            }
                        }

                        return false; // stop enumeration once found
                    }, IntPtr.Zero);

                    if (!enumResult)
                    {
                        AppContainer.Logger().Warning("EnergyStar",
                            "EnumChildWindows failed while searching for UWP child process");
                    }
                }


                ToggleEfficiencyMode(processHandle, false);
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    Win32Api.CloseHandle(processHandle);
                }
            }
        }

        /// <summary>
        /// 使用统一的 process access 参数调用 Win32Api.OpenProcess
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        private static IntPtr NativeOpenProcess(int processId)
        {
            const Win32Api.ProcessAccessFlags processAccess =
                Win32Api.ProcessAccessFlags.QueryLimitedInformation |
                Win32Api.ProcessAccessFlags.SetInformation;
            var handle = Win32Api.OpenProcess(processAccess, false, (uint)processId);
            if (handle == IntPtr.Zero)
            {
                AppContainer.Logger().Debug("EnergyStar", $"NativeOpenProcess failed for PID {processId}");
            }

            return handle;
        }

        /// <summary>
        /// 将除了 PendingProcPid 和 BypassProcessList 外的所有进程，都切换到【效率模式】
        /// </summary>
        public void ThrottleAllUserBackgroundProcesses()
        {
            AppContainer.Logger().Debug("EnergyStar", "Throttle All User Background Processes");
            ToggleAllBackgroundProcessesMode(true);
        }

        /// <summary>
        /// 将除了 PendingProcPid 和 BypassProcessList 外的所有进程，都切换到【普通模式】
        /// </summary>
        public void BoostAllUserBackgroundProcesses()
        {
            AppContainer.Logger().Debug("EnergyStar", "Boost All User Background Processes");
            ToggleAllBackgroundProcessesMode(false);
        }

        private void ToggleAllBackgroundProcessesMode(bool enable)
        {
            try
            {
                var runningProcesses = Process.GetProcesses();
                using var currentProcess = Process.GetCurrentProcess();
                var currentSessionId = currentProcess.SessionId;

                foreach (var proc in runningProcesses)
                {
                    try
                    {
                        using (proc)
                        {
                            // Skip processes that have already exited before accessing properties
                            if (proc.HasExited)
                            {
                                continue;
                            }

                            if (proc.SessionId != currentSessionId)
                            {
                                continue;
                            }

                            var hProcess = NativeOpenProcess(proc.Id);
                            if (hProcess != IntPtr.Zero)
                            {
                                try
                                {
                                    ToggleEfficiencyMode(hProcess, enable);
                                }
                                finally
                                {
                                    Win32Api.CloseHandle(hProcess);
                                }
                            }
                        }
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

        /// <summary>
        /// 从 AppSettings 重新加载 EnergyStarBypassProcessList 配置。
        /// 调用后新设置立即生效，无需重启应用。
        /// </summary>
        public void ReloadBypassList()
        {
            lock (_bypassLock)
            {
                _bypassProcessList.Clear();
                _bypassProcessList.UnionWith(_hardcodedBypassList);
                _bypassProcessList.UnionWith(LoadBypassSetting());
            }
        }

        /// <summary>
        /// 从 AppSettings 解析 EnergyStarBypassProcessList 配置项，返回进程名列表。
        /// </summary>
        private static IEnumerable<string> LoadBypassSetting()
        {
            return (AppSettings.Get("EnergyStarBypassProcessList") ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());
        }

        /// <summary>
        /// 线程安全的白名单检查（配合 ReloadBypassList 的写锁）
        /// </summary>
        private bool IsInBypassList(string appName)
        {
            lock (_bypassLock)
            {
                return _bypassProcessList.Contains(appName);
            }
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        private void DisposeCore()
        {
            if (_disposed)
            {
                return;
            }

            if (_pThrottleOn != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_pThrottleOn);
            }

            if (_pThrottleOff != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_pThrottleOff);
            }

            _disposed = true;
        }
    }
}
