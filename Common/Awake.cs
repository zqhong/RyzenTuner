using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RyzenTuner.Common
{
    /**
     * 参考：
     * github.com\PowerToys\src\modules\awake\Awake\Core\NativeMethods.cs
     * https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate
     * https://www.cnblogs.com/guorongtao/p/13918094.html
     */
    public static class Awake
    {
        [Flags]
        private enum ExecutionState : uint
        {
            // 通知系统，被设置的状态应该保持有效，直到下一次使用ES_CONTINUOUS的调用和其他状态标志之一被清除
            EsContinuous = 0x80000000,

            // 通过重置显示器的空闲计时器，迫使显示器打开
            EsDisplayRequired = 0x00000002,

            // 通过重置系统空闲定时器，迫使系统处于工作状态
            EsSystemRequired = 0x00000001
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        /// <summary>
        /// 保持操作系统清醒。
        /// </summary>
        /// <param name="keepDisplayOn">是否同时保持显示器开启。</param>
        /// <returns>true 表示成功，false 表示失败。</returns>
        public static bool KeepSystemAwake(bool keepDisplayOn)
        {
            return keepDisplayOn
                ? SetAwakeState(ExecutionState.EsSystemRequired | ExecutionState.EsContinuous |
                                ExecutionState.EsDisplayRequired)
                : SetAwakeState(ExecutionState.EsSystemRequired | ExecutionState.EsContinuous);
        }

        /// <summary>
        /// 允许系统进入睡眠。
        /// </summary>
        /// <returns>true 表示成功，false 表示失败。</returns>
        public static bool AllowSystemSleep()
        {
            return SetAwakeState(ExecutionState.EsContinuous);
        }

        /// <summary>
        /// 使一个应用程序能够通知系统它正在使用中，从而防止系统在应用程序运行时进入睡眠状态或关闭显示器。
        ///
        /// 在没有ES_CONTINUOUS的情况下调用SetThreadExecutionState，只是简单地重置了空闲计时器；为了保持显示或系统处于工作状态，线程必须定期地调用SetThreadExecutionState。
        /// </summary>
        /// <param name="state">Single or multiple ExecutionState entries.</param>
        /// <returns>true if successful, false if failed</returns>
        private static bool SetAwakeState(ExecutionState state)
        {
            try
            {
                var stateResult = SetThreadExecutionState(state);
                if (stateResult == 0)
                {
                    Debug.WriteLine(
                        $"[Awake.SetAwakeState] SetThreadExecutionState returned 0 (state={state}). LastError={Marshal.GetLastWin32Error()}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Awake.SetAwakeState] Exception: {ex}");
                return false;
            }
        }
    }
}