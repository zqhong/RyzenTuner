using System;
using System.Runtime.InteropServices;

namespace RyzenTuner
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
            // 启用离开模式。这个值必须和ES_CONTINUOUS一起指定
            ES_AWAYMODE_REQUIRED = 0x00000040,

            // 通知系统，被设置的状态应该保持有效，直到下一次使用ES_CONTINUOUS的调用和其他状态标志之一被清除
            ES_CONTINUOUS = 0x80000000,

            // 通过重置显示器的空闲计时器，迫使显示器打开
            ES_DISPLAY_REQUIRED = 0x00000002,

            // 通过重置系统空闲定时器，迫使系统处于工作状态
            ES_SYSTEM_REQUIRED = 0x00000001,
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        /**
         * 保持操作系统清醒
         */
        public static bool KeepingSysAwake(bool keepDisplayOn = false)
        {
            bool success;
            if (keepDisplayOn)
            {
                success = SetAwakeState(ExecutionState.ES_SYSTEM_REQUIRED | ExecutionState.ES_CONTINUOUS |
                                        ExecutionState.ES_DISPLAY_REQUIRED);
            }
            else
            {
                success = SetAwakeState(ExecutionState.ES_SYSTEM_REQUIRED | ExecutionState.ES_CONTINUOUS);
            }

            return success;
        }

        /// <summary>
        /// Sets the computer awake state using the native Win32 SetThreadExecutionState API. This
        /// function is just a nice-to-have wrapper that helps avoid tracking the success or failure of
        /// the call.
        /// </summary>
        /// <param name="state">Single or multiple EXECUTION_STATE entries.</param>
        /// <returns>true if successful, false if failed</returns>
        private static bool SetAwakeState(ExecutionState state)
        {
            try
            {
                var stateResult = SetThreadExecutionState(state);
                return stateResult != 0;
            }
            catch
            {
                return false;
            }
        }
    }
}