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
            var executablePath = Application.ExecutablePath;
            var workingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory;
            var currentUserSid = WindowsIdentity.GetCurrent().User?.Value;

            if (string.IsNullOrWhiteSpace(currentUserSid))
            {
                throw new InvalidOperationException("Unable to resolve the current Windows user SID.");
            }

            var taskXml = BuildTaskXml(executablePath, workingDirectory, currentUserSid);
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"RyzenTuner.Startup.{Guid.NewGuid():N}.xml");

            try
            {
                File.WriteAllText(tempFilePath, taskXml, new UTF8Encoding(false));
                RunSchtasks($"/Create /TN {Quote(TaskName)} /XML {Quote(tempFilePath)} /F");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
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

        private static string QueryTaskXml()
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
            var document = XDocument.Parse(taskXml);
            var taskNamespace = document.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var command = document.Descendants(taskNamespace + "Command").FirstOrDefault()?.Value;
            var arguments = document.Descendants(taskNamespace + "Arguments").FirstOrDefault()?.Value;

            return string.Equals(NormalizePath(command), NormalizePath(Application.ExecutablePath), StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(arguments?.Trim(), StartupArgument, StringComparison.Ordinal);
        }

        private static string BuildTaskXml(string executablePath, string workingDirectory, string currentUserSid)
        {
            var escapedExecutablePath = SecurityElementEscape(executablePath);
            var escapedWorkingDirectory = SecurityElementEscape(workingDirectory);
            var escapedUserSid = SecurityElementEscape(currentUserSid);
            var escapedAuthor = SecurityElementEscape(WindowsIdentity.GetCurrent().Name ?? "RyzenTuner");

            return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                   $"<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
                   "  <RegistrationInfo>\r\n" +
                   $"    <Date>{DateTime.Now:s}</Date>\r\n" +
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
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.Unicode,
                StandardErrorEncoding = Encoding.Unicode,
            };

            process.Start();
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            var result = new SchtasksResult(process.ExitCode, standardOutput, standardError);
            if (throwOnError && result.ExitCode != 0)
            {
                throw new InvalidOperationException(result.ErrorMessage);
            }

            return result;
        }

        private static bool TaskDoesNotExist(SchtasksResult result)
        {
            var output = (result.StandardOutput + "\n" + result.StandardError).ToLowerInvariant();
            return output.Contains("cannot find the file specified") ||
                   output.Contains("找不到指定的文件") ||
                   output.Contains("the system cannot find the file specified") ||
                   output.Contains("不存在") ||
                   output.Contains("cannot find the task") ||
                   output.Contains("无法找到");
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().Trim('"');
        }

        private static string Quote(string value)
        {
            return $"\"{value}\"";
        }

        private static string SecurityElementEscape(string value)
        {
            return System.Security.SecurityElement.Escape(value) ?? string.Empty;
        }

        private readonly struct SchtasksResult
        {
            public SchtasksResult(int exitCode, string standardOutput, string standardError)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput ?? string.Empty;
                StandardError = standardError ?? string.Empty;
            }

            public int ExitCode { get; }

            public string StandardOutput { get; }

            public string StandardError { get; }

            public string ErrorMessage
            {
                get
                {
                    var message = string.IsNullOrWhiteSpace(StandardError) ? StandardOutput : StandardError;
                    return string.IsNullOrWhiteSpace(message) ? "schtasks.exe failed." : message.Trim();
                }
            }
        }
    }
}
