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
using System.IO;
using System.Text;

namespace RyzenTuner.Common.Logger
{
    /**
     * Refer: https://gist.github.com/heiswayi/69ef5413c0f28b3a58d964447c275058
     */
    public class SimpleLogger : IDisposable
    {
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
        private StreamWriter? _writer;
        private bool _disposed;

        public LogLevel DefaultLogLevel { get; set; }

        /// <summary>
        /// Initiate an instance of SimpleLogger class constructor.
        /// If log file does not exist, it will be created automatically.
        /// </summary>
        public SimpleLogger()
        {
            _datetimeFormat = "yyyy-MM-dd HH:mm:ss";
            var logDir = AppDomain.CurrentDomain.BaseDirectory;
            _logFilename = Path.Combine(logDir,
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + FileExt);
            DefaultLogLevel = LogLevel.Warning;

            // Prepend BOM for UTF-8 and keep writer open for the lifetime of the logger
            _writer = new StreamWriter(_logFilename, true, Encoding.UTF8)
            {
                AutoFlush = true,
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            lock (_fileLock)
            {
                _writer?.Dispose();
                _writer = null;
            }
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
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, $"Unknown log level: {logLevel}")
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
            if (e == null)
                return;

            // Get stack trace for the exception with source file information
            var st = new StackTrace(e, true);
            // Get the top stack frame (may be null if stack trace is empty)
            var frame = st.GetFrame(0);
            var line = frame?.GetFileLineNumber() ?? 0;

            Warning($"Exception: {e.Message}\nLine: {line}\nStackTrace: {st}");
        }

        private void WriteLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            lock (_fileLock)
            {
                if (_writer == null)
                    return;

                _writer.WriteLine(text);
            }
        }

        private void WriteFormattedLog(LogLevel level, string text)
        {
            if (level < DefaultLogLevel)
            {
                return;
            }

            var timestamp = DateTime.Now.ToString(_datetimeFormat);
            var label = level switch
            {
                LogLevel.Trace => "[TRACE]",
                LogLevel.Info => "[INFO]",
                LogLevel.Debug => "[DEBUG]",
                LogLevel.Warning => "[WARNING]",
                LogLevel.Error => "[ERROR]",
                LogLevel.Fatal => "[FATAL]",
                _ => ""
            };

            WriteLine($"{timestamp} {label,-9} {text}");
        }
    }
}