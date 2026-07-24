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
using System.Globalization;
using System.IO;
using System.Text;

namespace RyzenTuner.Common.Logger
{
    /// <summary>
    /// Refer: https://gist.github.com/heiswayi/69ef5413c0f28b3a58d964447c275058
    ///
    /// Note: This logger has been superseded by SqliteLogger and is retained
    /// only as a file-based fallback reference.
    /// </summary>
    [Obsolete("Superseded by SqliteLogger. Retained only as a file-based fallback reference.")]
    public class SimpleLogger : IDisposable
    {
        private const string FileExt = ".log";
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const int LabelAlignmentWidth = 9;
        private readonly object _fileLock = new();
        private readonly string _logFilename;
        private StreamWriter? _writer;
        private volatile bool _disposed;

        public LogLevel DefaultLogLevel { get; set; } = LogLevel.Warning;

        /// <summary>
        /// Initiate an instance of SimpleLogger class constructor.
        /// If log file does not exist, it will be created automatically.
        /// </summary>
        public SimpleLogger()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RyzenTuner",
                "logs");

            _logFilename = Path.Combine(logDir,
                (System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "RyzenTuner") + FileExt);

            // Ensure the log directory exists
            try
            {
                var dir = Path.GetDirectoryName(_logFilename);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[SimpleLogger] Failed to create log directory: {ex.Message}");
            }

            // Prepend BOM for UTF-8 and keep writer open for the lifetime of the logger
            // 不启用 AutoFlush，由 Dispose() 统一刷入磁盘，避免每次写入都触发 FlushFileBuffers
            try
            {
                _writer = new StreamWriter(_logFilename, true, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[SimpleLogger] Failed to create log file '{_logFilename}': {ex.Message}");
                _writer = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            StreamWriter? writerToDispose;
            lock (_fileLock)
            {
                if (_disposed)
                    return;
                _disposed = true;
                writerToDispose = _writer;
                _writer = null;
            }

            // Dispose outside the lock to avoid holding it during I/O,
            // and to ensure WriteLine under the lock always sees _writer == null.
            try
            {
                writerToDispose?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[SimpleLogger] Dispose failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Forces any buffered log data to be written to the underlying file.
        /// </summary>
        public void Flush()
        {
            lock (_fileLock)
            {
                try
                {
                    _writer?.Flush();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"[SimpleLogger] Flush failed: {ex.Message}");
                }
            }
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

        public void LogException(Exception? e)
        {
            if (e == null)
                return;

            Error($"Exception: {e}");
        }

        private void WriteLine(string text)
        {
            lock (_fileLock)
            {
                if (_writer == null)
                    return;

                try
                {
                    _writer.WriteLine(text);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"[SimpleLogger] WriteLine failed: {ex.Message}");
                }
            }
        }

        private void WriteFormattedLog(LogLevel level, string text)
        {
            if (_disposed || level < DefaultLogLevel || string.IsNullOrEmpty(text))
            {
                return;
            }

            var timestamp = DateTime.UtcNow.ToString(DateTimeFormat, CultureInfo.InvariantCulture) + "Z";
            var label = level switch
            {
                LogLevel.Trace => "[TRACE]",
                LogLevel.Info => "[INFO]",
                LogLevel.Debug => "[DEBUG]",
                LogLevel.Warning => "[WARNING]",
                LogLevel.Error => "[ERROR]",
                LogLevel.Fatal => "[FATAL]",
                _ => $"[{level}]"
            };

            WriteLine($"{timestamp} {label,-LabelAlignmentWidth} {text}");
        }
    }
}