using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;
using System.Windows.Forms;

namespace RyzenTuner.Common
{
    internal static class StartupTaskScheduler
    {
        private const string TaskName = "RyzenTuner Startup";
        private const string StartupArgument = "-hide";
        private const string TaskDelay = "PT15S";
        private const string TaskNamespace = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        private const int SchtasksTimeoutMs = 30_000;
        private const int KillWaitMs = 5_000;
        private const int ErrorFileNotFound = 2;
        private static readonly WindowsIdentity? _currentIdentity = GetCurrentIdentity();

        // 注册进程退出和 AppDomain 卸载时释放 WindowsIdentity 的 SafeHandle
        static StartupTaskScheduler()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => _currentIdentity?.Dispose();
            AppDomain.CurrentDomain.DomainUnload += (_, _) => _currentIdentity?.Dispose();
        }

        private static WindowsIdentity? GetCurrentIdentity()
        {
            try
            {
                return WindowsIdentity.GetCurrent();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[StartupTaskScheduler] Failed to get current Windows identity: {ex.Message}");
                return null;
            }
        }

        public static bool IsEnabled()
        {
            var taskXml = QueryTaskXml();
            return taskXml != null && TaskMatchesCurrentExecutable(taskXml);
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                CreateOrUpdateTask();
                return;
            }

            DeleteTask();
        }

        private static void CreateOrUpdateTask()
        {
            if (_currentIdentity == null)
            {
                throw new InvalidOperationException("Unable to resolve the current Windows user identity.");
            }

            var executablePath = Application.ExecutablePath;
            var workingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory;
            var currentUserSid = _currentIdentity.User?.Value;

            if (string.IsNullOrWhiteSpace(currentUserSid))
            {
                throw new InvalidOperationException("Unable to resolve the current Windows user SID.");
            }

            var taskXml = BuildTaskXml(executablePath, workingDirectory, currentUserSid!);
            var guidStr = Guid.NewGuid().ToString("N");
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"RyzenTuner.Startup.{guidStr}.xml");

            try
            {
                File.WriteAllText(tempFilePath, taskXml, new UTF8Encoding(false));
                RunSchtasks($"/Create /TN {Quote(TaskName)} /XML {Quote(tempFilePath)} /F");
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"[StartupTaskScheduler] Failed to delete temp file: {ex.Message}");
                }
            }
        }

        private static void DeleteTask()
        {
            var result = RunSchtasks($"/Delete /TN {Quote(TaskName)} /F", false);
            if (result.ExitCode == 0 || TaskDoesNotExist(result))
            {
                return;
            }

            throw new InvalidOperationException(result.ErrorMessage);
        }

        private static string? QueryTaskXml()
        {
            var result = RunSchtasks($"/Query /TN {Quote(TaskName)} /XML", false);
            if (result.ExitCode != 0)
            {
                if (TaskDoesNotExist(result))
                {
                    return null;
                }

                throw new InvalidOperationException(result.ErrorMessage);
            }

            return string.IsNullOrWhiteSpace(result.StandardOutput) ? null : result.StandardOutput;
        }

        private static bool TaskMatchesCurrentExecutable(string taskXml)
        {
            try
            {
                var document = XDocument.Parse(taskXml);
                var taskNamespace = document.Root?.GetDefaultNamespace() ?? XNamespace.None;

                var command = document.Descendants(taskNamespace + "Command").FirstOrDefault()?.Value;
                var arguments = document.Descendants(taskNamespace + "Arguments").FirstOrDefault()?.Value;

                return string.Equals(TrimQuotesAndWhitespace(command), TrimQuotesAndWhitespace(Application.ExecutablePath), StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(TrimQuotesAndWhitespace(arguments), StartupArgument, StringComparison.OrdinalIgnoreCase);
            }
            catch (System.Xml.XmlException ex)
            {
                Trace.WriteLine($"[StartupTaskScheduler] Failed to parse task XML: {ex.Message}");
                return false;
            }
        }

        private static string BuildTaskXml(string executablePath, string workingDirectory, string currentUserSid)
        {
            var escapedExecutablePath = SecurityElementEscape(executablePath);
            var escapedWorkingDirectory = SecurityElementEscape(workingDirectory);
            var escapedUserSid = SecurityElementEscape(currentUserSid);
            var escapedAuthor = SecurityElementEscape(_currentIdentity?.Name ?? "RyzenTuner");

            return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                   $"<Task version=\"1.2\" xmlns=\"{TaskNamespace}\">\r\n" +
                   "  <RegistrationInfo>\r\n" +
                   $"    <Date>{DateTime.UtcNow:s}Z</Date>\r\n" +
                   $"    <Author>{escapedAuthor}</Author>\r\n" +
                   "    <Description>Launch RyzenTuner silently after user logon.</Description>\r\n" +
                   "  </RegistrationInfo>\r\n" +
                   "  <Triggers>\r\n" +
                   "    <LogonTrigger>\r\n" +
                   $"      <Delay>{TaskDelay}</Delay>\r\n" +
                   "      <Enabled>true</Enabled>\r\n" +
                   "    </LogonTrigger>\r\n" +
                   "  </Triggers>\r\n" +
                   "  <Principals>\r\n" +
                   "    <Principal id=\"Author\">\r\n" +
                   $"      <UserId>{escapedUserSid}</UserId>\r\n" +
                   "      <LogonType>InteractiveToken</LogonType>\r\n" +
                   "      <RunLevel>HighestAvailable</RunLevel>\r\n" +
                   "    </Principal>\r\n" +
                   "  </Principals>\r\n" +
                   "  <Settings>\r\n" +
                   "    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n" +
                   "    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\r\n" +
                   "    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\r\n" +
                   "    <AllowHardTerminate>false</AllowHardTerminate>\r\n" +
                   "    <StartWhenAvailable>true</StartWhenAvailable>\r\n" +
                   "    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>\r\n" +
                   "    <IdleSettings>\r\n" +
                   "      <StopOnIdleEnd>false</StopOnIdleEnd>\r\n" +
                   "      <RestartOnIdle>false</RestartOnIdle>\r\n" +
                   "    </IdleSettings>\r\n" +
                   "    <AllowStartOnDemand>true</AllowStartOnDemand>\r\n" +
                   "    <Enabled>true</Enabled>\r\n" +
                   "    <Hidden>false</Hidden>\r\n" +
                   "    <RunOnlyIfIdle>false</RunOnlyIfIdle>\r\n" +
                   "    <WakeToRun>false</WakeToRun>\r\n" +
                   "    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\r\n" +
                   "    <!-- Priority 7 = IDLE_PRIORITY_CLASS (lowest), BELOW_NORMAL = 4 in Task Scheduler schema -->\r\n" +
                   "    <Priority>7</Priority>\r\n" +
                   "  </Settings>\r\n" +
                   "  <Actions Context=\"Author\">\r\n" +
                   "    <Exec>\r\n" +
                   $"      <Command>{escapedExecutablePath}</Command>\r\n" +
                   $"      <Arguments>{StartupArgument}</Arguments>\r\n" +
                   $"      <WorkingDirectory>{escapedWorkingDirectory}</WorkingDirectory>\r\n" +
                   "    </Exec>\r\n" +
                   "  </Actions>\r\n" +
                   "</Task>\r\n";
        }

        private static SchtasksResult RunSchtasks(string arguments, bool throwOnError = true)
        {
            using var process = new Process();
            // 快照当前控制台编码，避免 TOCTOU 竞态。
            // 使用 try-catch 防御：WinForms 应用通常没有关联控制台，
            // Console.OutputEncoding 在此场景下会抛出 IOException。
            Encoding encoding;
            try
            {
                encoding = Console.OutputEncoding ?? Encoding.Default;
            }
            catch (IOException)
            {
                // 使用 UTF-8 作为回退：schtasks.exe /Query /XML 输出的 XML 声明为 UTF-8 编码，
                // 而 Encoding.Default 是系统 ANSI 代码页（如 Windows-1252），用它解码 UTF-8 会损坏 XML。
                encoding = Encoding.UTF8;
            }
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = encoding,
                StandardErrorEncoding = encoding,
            };

            try
            {
                if (!process.Start())
                {
                    // Start 返回 false 且未抛出异常时，无法读取重定向流
                    var errorMsg = $"schtasks.exe failed to start with arguments: {arguments}";
                    if (throwOnError)
                        throw new InvalidOperationException(errorMsg);

                    return new SchtasksResult(-1, string.Empty, string.Empty, errorMsg);
                }

                // 异步读取 stderr 的同时同步读取 stdout，防止两者管道缓冲区同时填满时
                // 子进程阻滞写操作而父进程阻滞 ReadToEnd 的死锁。
                // 参见: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput#remarks
                var stdErrTask = process.StandardError.ReadToEndAsync();
                var standardOutput = process.StandardOutput.ReadToEnd();
                var standardError = stdErrTask.GetAwaiter().GetResult();

                if (!process.WaitForExit(SchtasksTimeoutMs))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(KillWaitMs); // Kill 是异步的，等待进程完全退出
                    }
                    catch (Exception ex) when (ex is InvalidOperationException
                        or System.ComponentModel.Win32Exception)
                    {
                        // 进程可能在超时检查和 Kill 之间已自行退出，
                        // 或进程句柄已失效/无权限终止（极少出现）
                        Trace.WriteLine(
                            $"[StartupTaskScheduler] 终止超时 schtasks 进程异常: {ex.Message}");
                    }

                    var errorMsg = $"schtasks.exe timed out after {SchtasksTimeoutMs / 1000} seconds";
                    if (throwOnError)
                        throw new InvalidOperationException(errorMsg);

                    return new SchtasksResult(-1, string.Empty, string.Empty, errorMsg);
                }

                var errorMessage = BuildErrorMessage(standardOutput, standardError);
                var result = new SchtasksResult(process.ExitCode, standardOutput, standardError, errorMessage);

                if (throwOnError && result.ExitCode != 0)
                {
                    throw new InvalidOperationException(result.ErrorMessage);
                }

                return result;
            }
            catch (InvalidOperationException ex) when (!throwOnError)
            {
                return HandleSchtasksError(process, ex);
            }
            catch (System.ComponentModel.Win32Exception ex) when (!throwOnError)
            {
                return HandleSchtasksError(process, ex);
            }
        }

        /// <summary>
        /// 处理 schtasks 执行失败：清理进程后返回错误结果。
        /// </summary>
        private static SchtasksResult HandleSchtasksError(Process process, Exception ex)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(KillWaitMs); // Kill 后等待进程退出
                }
            }
            catch (Exception innerEx)
            {
                Trace.WriteLine(
                    $"[StartupTaskScheduler] 清理 schtasks 进程时异常: {innerEx.Message}");
            }

            return new SchtasksResult(-1, string.Empty, string.Empty, ex.Message);
        }

        private static string BuildErrorMessage(string standardOutput, string standardError)
        {
            var message = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
            return string.IsNullOrWhiteSpace(message) ? "schtasks.exe failed." : message.Trim();
        }

        /// <summary>
        /// 判断 schtasks 退出码是否表示任务不存在。
        ///
        /// ERROR_FILE_NOT_FOUND (2) 是可靠且不受区域设置影响的退出码，
        /// 足以覆盖任务不存在的场景。不再使用区域设置相关的文本模式匹配，
        /// 避免不同语言下无法正确识别的问题。
        /// </summary>
        private static bool TaskDoesNotExist(SchtasksResult result)
        {
            // Exit code 0 means success, so the task exists.
            if (result.ExitCode == 0)
                return false;

            // ERROR_FILE_NOT_FOUND is the reliable locale-independent indicator of "task not found"
            return result.ExitCode == ErrorFileNotFound;
        }

        private static string TrimQuotesAndWhitespace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value!.Trim().Trim('"');
        }

        private static string Quote(string value)
        {
            return $"\"{value}\"";
        }

        private static string SecurityElementEscape(string? value)
        {
            // SecurityElement.Escape throws ArgumentNullException when value is null
            return value == null ? string.Empty : System.Security.SecurityElement.Escape(value);
        }

        private readonly struct SchtasksResult
        {
            public SchtasksResult(int exitCode, string standardOutput, string standardError, string errorMessage)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
                ErrorMessage = errorMessage;
            }

            public int ExitCode { get; }

            public string StandardOutput { get; }

            public string StandardError { get; }

            public string ErrorMessage { get; }
        }
    }
}
