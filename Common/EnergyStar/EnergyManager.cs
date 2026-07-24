using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        private const int MaxProcessPathChars = 32767;

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

        private IntPtr _throttleOnPtr;
        private IntPtr _throttleOffPtr;
        private readonly object _nativeLock = new();
        private readonly int _controlBlockSize;
        private int _disposed;
        // 由 HandleForeground 设置，供 ToggleAllBackgroundProcessesMode 使用
        // 包含解析 UWP 子进程后的真实前台进程 ID，解决 UWP 前台应用被误节流的问题
        private volatile uint _resolvedForegroundPid;
        [ThreadStatic]
        private static StringBuilder? _cachedSb;

        public EnergyManager()
        {
            // 构造函数中无需加锁：对象尚未发布给其他线程
            _bypassProcessList.UnionWith(_hardcodedBypassList);
            _bypassProcessList.UnionWith(LoadBypassSetting());

            _controlBlockSize = Marshal.SizeOf<Win32Api.ProcessPowerThrottlingState>();

            // 非托管内存分配：依次分配两块内存，利用 try-catch 保证首次分配成功但第二次失败时
            // 不会泄漏已分配的内存。若在 try 块外直接分配，第二个 AllocHGlobal 抛异常时
            // 第一个分配就会泄漏。DisposeCore() 统一清理已分配的 HGlobal。
            try
            {
                _throttleOnPtr = Marshal.AllocHGlobal(_controlBlockSize);
                _throttleOffPtr = Marshal.AllocHGlobal(_controlBlockSize);
            }
            catch (Exception)
            {
                // 使用 DisposeCore() 统一清理：释放所有已分配的 HGlobal 后置零字段，
                // 并标记 _disposed = 1 阻止 ~EnergyManager 终结器对已释放指针执行 double-free。
                DisposeCore();
                throw;
            }

            // 参考：https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
            // EcoQoS：打开 ExecutionSpeed 节流功能
            var throttleState = new Win32Api.ProcessPowerThrottlingState
            {
                Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
                ControlMask = Win32Api.ProcessPowerThrottlingFlags.ProcessPowerThrottlingExecutionSpeed,
                StateMask = Win32Api.ProcessPowerThrottlingFlags.ProcessPowerThrottlingExecutionSpeed,
            };

            // HighQoS：关闭 ExecutionSpeed 节流
            var unThrottleState = new Win32Api.ProcessPowerThrottlingState
            {
                Version = Win32Api.ProcessPowerThrottlingState.ProcessPowerThrottlingCurrentVersion,
                ControlMask = Win32Api.ProcessPowerThrottlingFlags.ProcessPowerThrottlingExecutionSpeed,
                StateMask = Win32Api.ProcessPowerThrottlingFlags.None,
            };

            try
            {
                Marshal.StructureToPtr(throttleState, _throttleOnPtr, false);
                Marshal.StructureToPtr(unThrottleState, _throttleOffPtr, false);
            }
            catch (Exception)
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
        /// <param name="processHandle"></param>
        /// <param name="enable"></param>
        /// <param name="preResolvedName"></param>
        private void ToggleEfficiencyMode(IntPtr processHandle, bool enable, string? preResolvedName = null)
        {
            if (processHandle == IntPtr.Zero)
                return;

            try
            {
                var processId = Win32Api.GetProcessId(processHandle);
                var appName = preResolvedName ?? GetProcessNameFromHandle(processHandle);

                if (appName == UnknownProcessName)
                {
                    AppContainer.Logger().Warning("EnergyStar", $"ToggleEfficiencyMode: 获取进程名称失败。pid: {processId}");
                    return;
                }

                // 在锁内执行 SetProcessInformation，防止与 DisposeCore 的 FreeHGlobal 发生 TOCTOU 竞态
                // （指针在使用期间被释放导致 use-after-free）
                // 同时在锁内重新检查白名单，避免与 ReloadBypassList 的 TOCTOU 竞态
                lock (_nativeLock)
                {
                    if (_disposed != 0)
                        return;

                    if (IsInBypassList(appName))
                    {
                        AppContainer.Logger().Debug("EnergyStar",
                            $"ToggleEfficiencyMode: 不处理白名单列表中的应用 {appName}");
                        return;
                    }

                    try
                    {
                        var throttleResult = Win32Api.SetProcessInformation(processHandle,
                            Win32Api.ProcessInformationClass.ProcessPowerThrottling,
                            enable ? _throttleOnPtr : _throttleOffPtr, (uint)_controlBlockSize);

                        var priorityResult = Win32Api.SetPriorityClass(processHandle,
                            enable
                                ? Win32Api.PriorityClass.BelowNormalPriorityClass
                                : Win32Api.PriorityClass.NormalPriorityClass);

                        var actionText = enable ? "Throttle" : "Boost";
                        AppContainer.Logger().Debug("EnergyStar",
                            $"{actionText} {appName}. pid: {processId}, set process information and priority result: {throttleResult && priorityResult}");
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
        /// 通过进程句柄获取进程名称
        /// </summary>
        /// <param name="processHandle"></param>
        /// <returns></returns>
        private static string GetProcessNameFromHandle(IntPtr processHandle)
        {
            try
            {
                var sb = _cachedSb;
                if (sb == null)
                {
                    sb = new StringBuilder(MaxProcessPathChars);
                    _cachedSb = sb;
                }
                sb.Clear();

                uint capacity = MaxProcessPathChars;

                if (Win32Api.QueryFullProcessImageName(processHandle, 0, sb, ref capacity))
                {
                    return Path.GetFileName(sb.ToString());
                }

                var win32Error = Marshal.GetLastWin32Error();
                AppContainer.Logger().Warning("EnergyStar",
                    $"QueryFullProcessImageName failed for process handle 0x{processHandle.ToInt64():X}, win32Error={win32Error}");
                return UnknownProcessName;
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
                return UnknownProcessName;
            }
        }

        /// <summary>
        /// 处理前台进程：检测前台窗口，如果属于 UWP 框架则解析真实子进程，否则直接标记为前台（取消节流）。
        /// </summary>
        public void HandleForeground()
        {
            uint resolvedPid = 0;
            IntPtr processHandle = IntPtr.Zero;
            string appName = "";

            try
            {
                var windowHandle = Win32Api.GetForegroundWindow();
                if (windowHandle == IntPtr.Zero) return;

                if (Win32Api.GetWindowThreadProcessId(windowHandle, out var processId) == 0 || processId == 0) return;

                processHandle = NativeOpenProcess(processId);
                if (processHandle == IntPtr.Zero) return;

                appName = GetProcessNameFromHandle(processHandle);

                // 尝试解析 UWP 子进程
                TryResolveUwpChild(windowHandle, ref processHandle, ref processId, ref appName);

                // 存储最终解析后的前台进程 ID（可能是 UWP 子进程），
                // 供 ToggleAllBackgroundProcessesMode 跳过正确的前台进程
                resolvedPid = processId;

                ToggleEfficiencyMode(processHandle, false, appName);
            }
            catch (Exception e)
            {
                AppContainer.Logger().LogException(e);
            }
            finally
            {
                // 在 finally 中写入字段，确保异常时不会提前置零导致误节流
                _resolvedForegroundPid = resolvedPid;

                if (processHandle != IntPtr.Zero)
                {
                    Win32Api.CloseHandle(processHandle);
                }
            }
        }

        /// <summary>
        /// 如果前台窗口属于 ApplicationFrameHost，尝试枚举子窗口找到真正的 UWP 进程。
        /// </summary>
        private static void TryResolveUwpChild(IntPtr windowHandle, ref IntPtr processHandle,
            ref uint processId, ref string appName)
        {
            if (!string.Equals(appName, UwpFrameHostApp, StringComparison.OrdinalIgnoreCase))
                return;

            var (found, enumFailed) = TryFindUwpChildWindow(windowHandle, processId,
                ref processHandle, ref processId, ref appName);

            if (found)
                return;

            AppContainer.Logger().Warning("EnergyStar",
                enumFailed
                    ? $"EnumChildWindows failed for ApplicationFrameHost window"
                    : "No UWP child process found under ApplicationFrameHost window");
        }

        /// <summary>
        /// 枚举 ApplicationFrameHost 的子窗口，查找真正的 UWP 子进程。
        /// </summary>
        /// <returns>(Found, EnumFailed) — Found 为 true 表示找到 UWP 子进程；
        /// EnumFailed 表示 EnumChildWindows API 调用失败（仅当 Found 为 false 时有意义）。</returns>
        private static (bool Found, bool EnumFailed) TryFindUwpChildWindow(IntPtr windowHandle, uint frameHostPid,
            ref IntPtr capturedHandle, ref uint capturedPid, ref string capturedAppName)
        {
            // 将 ref 参数复制为局部变量供 lambda 捕获（C# 不允许 lambda 捕获 ref 参数）
            var localHandle = capturedHandle;
            var localPid = capturedPid;
            var localAppName = capturedAppName;
            var found = false;

            var enumResult = Win32Api.EnumChildWindows(windowHandle, (innerWindowHandle, _) =>
            {
                try
                {
                    // 若已找到有效子进程（因异常导致回调未正确返回 false 而再次被调用），
                    // 立即返回 false 停止枚举，避免对已关闭的 localHandle 执行 double-close。
                    if (found)
                        return false;

                    if (Win32Api.GetWindowThreadProcessId(innerWindowHandle, out var innerProcId) == 0)
                        return true;
                    if (frameHostPid == innerProcId)
                        return true;

                    var innerProcHandle = NativeOpenProcess(innerProcId);
                    if (innerProcHandle == IntPtr.Zero) return true;

                    try
                    {
                        var innerAppName = GetProcessNameFromHandle(innerProcHandle);

                        // Unknown process name is not resolvable — continue enumeration
                        if (innerAppName == UnknownProcessName)
                            return true;

                        // Found. Reinitialize handles and stop enumeration
                        found = true;
                        if (!Win32Api.CloseHandle(localHandle))
                        {
                            AppContainer.Logger().Warning("EnergyStar",
                                $"CloseHandle failed for ApplicationFrameHost, error={Marshal.GetLastWin32Error()}");
                        }
                        localHandle = innerProcHandle;
                        localPid = innerProcId;
                        localAppName = innerAppName;
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
                }
                catch (Exception ex)
                {
                    AppContainer.Logger().LogException(ex);
                    return true; // continue enumeration on error
                }
            }, IntPtr.Zero);

            // 将局部变量的结果写回 ref 参数
            if (found)
            {
                capturedHandle = localHandle;
                capturedPid = localPid;
                capturedAppName = localAppName;
                return (true, false);
            }

            if (!enumResult)
            {
                var enumError = Marshal.GetLastWin32Error();
                AppContainer.Logger().Warning("EnergyStar",
                    $"EnumChildWindows failed, error={enumError}");
                return (false, true);
            }

            return (false, false);
        }

        /// <summary>
        /// 使用统一的 process access 参数调用 Win32Api.OpenProcess
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        private static IntPtr NativeOpenProcess(uint processId)
        {
            const Win32Api.ProcessAccessFlags processAccess =
                Win32Api.ProcessAccessFlags.QueryLimitedInformation |
                Win32Api.ProcessAccessFlags.SetInformation;
            var handle = Win32Api.OpenProcess(processAccess, false, processId);
            if (handle == IntPtr.Zero)
            {
                var win32Error = Marshal.GetLastWin32Error();
                AppContainer.Logger().Debug("EnergyStar",
                    $"NativeOpenProcess failed for PID {processId}, win32Error={win32Error}");
            }

            return handle;
        }

        /// <summary>
        /// 将除了前台进程和 BypassProcessList 外的所有进程，都切换到【效率模式】
        /// </summary>
        public void ThrottleAllUserBackgroundProcesses()
        {
            AppContainer.Logger().Debug("EnergyStar", "Throttle All User Background Processes");
            ToggleAllBackgroundProcessesMode(true);
        }

        /// <summary>
        /// 将除了前台进程和 BypassProcessList 外的所有进程，都切换到【普通模式】
        /// </summary>
        public void BoostAllUserBackgroundProcesses()
        {
            AppContainer.Logger().Debug("EnergyStar", "Boost All User Background Processes");
            ToggleAllBackgroundProcessesMode(false);
        }

        /// <summary>
        /// 获取前台窗口的进程 ID。
        /// </summary>
        private static uint GetForegroundProcessId()
        {
            var windowHandle = Win32Api.GetForegroundWindow();
            if (windowHandle == IntPtr.Zero) return 0;
            // 检查 GetWindowThreadProcessId 返回值：0 表示失败，此时返回 0 让调用方跳过前台保护
            if (Win32Api.GetWindowThreadProcessId(windowHandle, out var pid) == 0) return 0;
            return pid;
        }

        private void ToggleAllBackgroundProcessesMode(bool enable)
        {
            try
            {
                using var currentProcess = Process.GetCurrentProcess();
                var runningProcesses = Process.GetProcesses();
                var currentSessionId = currentProcess.SessionId;
                // 使用 HandleForeground 解析后的前台进程 ID（含 UWP 子进程解析），
                // 避免 UWP 前台应用被误节流。
                // 若 HandleForeground 尚未运行（_resolvedForegroundPid == 0），
                // 回退到 GetForegroundProcessId() 获取窗口所有者 PID。
                var foregroundPid = _resolvedForegroundPid;
                if (foregroundPid == 0)
                {
                    foregroundPid = GetForegroundProcessId();
                }

                foreach (var proc in runningProcesses)
                {
                    try
                    {
                        using (proc)
                        {
                            if (proc.HasExited) continue;
                            if (proc.SessionId != currentSessionId) continue;
                            if (foregroundPid != 0 && (uint)proc.Id == foregroundPid) continue;

                            // 使用 ProcessName 预检查白名单，避免为白名单进程打开句柄
                            // ProcessName 不含扩展名和路径，拼接 ".exe" 后与 _bypassProcessList 匹配
                            var processName = proc.ProcessName + ".exe";
                            if (IsInBypassList(processName)) continue;

                            var processHandle = NativeOpenProcess((uint)proc.Id);
                            if (processHandle != IntPtr.Zero)
                            {
                                try
                                {
                                    // 传入预解析的进程名，避免 ToggleEfficiencyMode 中重复调用 QueryFullProcessImageName
                                    ToggleEfficiencyMode(processHandle, enable, processName);
                                }
                                finally
                                {
                                    Win32Api.CloseHandle(processHandle);
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
            // 先构建新列表，若 LoadBypassSetting() 抛出异常，_bypassProcessList 保持原状态不变
            var newList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ReloadBypassList 仅获取 _bypassLock（不获取 _nativeLock），与约定顺序 _bypassLock → _nativeLock 不冲突。
            // 详见 ToggleEfficiencyMode 中的锁顺序注释。
            lock (_bypassLock)
            {
                newList.UnionWith(_hardcodedBypassList);
                newList.UnionWith(LoadBypassSetting());

                _bypassProcessList.Clear();
                _bypassProcessList.UnionWith(newList);
            }
        }

        /// <summary>
        /// 从 AppSettings 解析 EnergyStarBypassProcessList 配置项，返回进程名列表。
        /// </summary>
        private static IEnumerable<string> LoadBypassSetting()
        {
            return (AppSettings.Get("EnergyStarBypassProcessList") ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0);
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
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            // DisposeCore 仅获取 _nativeLock（不获取 _bypassLock），与约定顺序 _bypassLock → _nativeLock 不冲突。
            // 详见 ToggleEfficiencyMode 中的锁顺序注释。
            IntPtr throttleOn, throttleOff;
            lock (_nativeLock)
            {
                throttleOn = _throttleOnPtr;
                throttleOff = _throttleOffPtr;
                _throttleOnPtr = IntPtr.Zero;
                _throttleOffPtr = IntPtr.Zero;
            }

            if (throttleOn != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(throttleOn);
            }

            if (throttleOff != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(throttleOff);
            }
        }
    }
}
