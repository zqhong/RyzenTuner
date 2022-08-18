/*
MIT License

Copyright (c) 2016 Heiswayi Nrird

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Diagnostics;

namespace RyzenTuner.Common.Logger
{
    /**
     * Refer: https://gist.github.com/heiswayi/69ef5413c0f28b3a58d964447c275058
     */
    public class SimpleLogger
    {
        [Flags]
        public enum LogLevel
        {
            Trace = 0,
            Debug,
            Info,
            Warning,
            Error,
            Fatal
        }

        private const string FileExt = ".log";
        private readonly object _fileLock = new();
        private readonly string _datetimeFormat;
        private readonly string _logFilename;

        public LogLevel DefaultLogLevel;

        /// <summary>
        /// Initiate an instance of SimpleLogger class constructor.
        /// If log file does not exist, it will be created automatically.
        /// </summary>
        public SimpleLogger()
        {
            _datetimeFormat = "yyyy-MM-dd HH:mm:ss";
            _logFilename = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + FileExt;
            DefaultLogLevel = LogLevel.Warning;
        }

        public LogLevel ToLogLevel(string logLevel)
        {
            return logLevel switch
            {
                "Trace" => LogLevel.Trace,
                "Debug" => LogLevel.Debug,
                "Info" => LogLevel.Info,
                "Warning" => LogLevel.Warning,
                "Error" => LogLevel.Error,
                "Fatal" => LogLevel.Fatal,
                _ => throw new Exception($"不正确的 log level 类型：{logLevel}")
            };
        }

        /// <summary>
        /// Log a DEBUG message
        /// </summary>
        /// <param name="text">Message</param>
        public void Debug(string text)
        {
            WriteFormattedLog(LogLevel.Debug, text);
        }

        /// <summary>
        /// Log an ERROR message
        /// </summary>
        /// <param name="text">Message</param>
        public void Error(string text)
        {
            WriteFormattedLog(LogLevel.Error, text);
        }

        /// <summary>
        /// Log a FATAL ERROR message
        /// </summary>
        /// <param name="text">Message</param>
        public void Fatal(string text)
        {
            WriteFormattedLog(LogLevel.Fatal, text);
        }

        /// <summary>
        /// Log an INFO message
        /// </summary>
        /// <param name="text">Message</param>
        public void Info(string text)
        {
            WriteFormattedLog(LogLevel.Info, text);
        }

        /// <summary>
        /// Log a TRACE message
        /// </summary>
        /// <param name="text">Message</param>
        public void Trace(string text)
        {
            WriteFormattedLog(LogLevel.Trace, text);
        }

        /// <summary>
        /// Log a WARNING message
        /// </summary>
        /// <param name="text">Message</param>
        public void Warning(string text)
        {
            WriteFormattedLog(LogLevel.Warning, text);
        }

        public void LogException(Exception e)
        {
            // Get stack trace for the exception with source file information
            var st = new StackTrace(e, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();

            Warning($"Exception\n Message: {e.Message}\n StackTrace: {st}, Line: {line}");
        }

        private void WriteLine(string text, bool append = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            lock (_fileLock)
            {
                using (System.IO.StreamWriter writer =
                       new System.IO.StreamWriter(_logFilename, append, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(text);
                }
            }
        }

        private void WriteFormattedLog(LogLevel level, string text)
        {
            if (level < DefaultLogLevel)
            {
                return;
            }

            var pretext = level switch
            {
                LogLevel.Trace => DateTime.Now.ToString(_datetimeFormat) + " [TRACE]   ",
                LogLevel.Info => DateTime.Now.ToString(_datetimeFormat) + " [INFO]    ",
                LogLevel.Debug => DateTime.Now.ToString(_datetimeFormat) + " [DEBUG]   ",
                LogLevel.Warning => DateTime.Now.ToString(_datetimeFormat) + " [WARNING] ",
                LogLevel.Error => DateTime.Now.ToString(_datetimeFormat) + " [ERROR]   ",
                LogLevel.Fatal => DateTime.Now.ToString(_datetimeFormat) + " [FATAL]   ",
                _ => ""
            };

            WriteLine(pretext + text, true);
        }
    }
}