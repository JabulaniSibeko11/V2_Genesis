using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace V2_Genesis.Services.Logging
{
    public sealed class YearMonthFileSink : ILogEventSink, IDisposable
    {
        private readonly object _sync = new();
        private readonly string _rootPath;
        private readonly long _fileSizeLimitBytes;
        private readonly ITextFormatter _formatter;

        private StreamWriter? _writer;
        private string? _currentFilePath;

        public YearMonthFileSink(string rootPath, int fileSizeLimitMb)
        {
            _rootPath = string.IsNullOrWhiteSpace(rootPath)
                ? @"C:\Genesis Log"
                : rootPath.Trim();

            _fileSizeLimitBytes = Math.Max(fileSizeLimitMb, 1) * 1024L * 1024L;

            _formatter = new MessageTemplateTextFormatter(
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] " +
                "[{Application}] [{Environment}] [{MachineName}] " +
                "[{CorrelationId}] [{SourceContext}] " +
                "{Message:lj}{NewLine}{Exception}");
        }

        public void Emit(LogEvent logEvent)
        {
            lock (_sync)
            {
                EnsureWriter(logEvent.Timestamp.LocalDateTime);
                _formatter.Format(logEvent, _writer!);
                _writer!.Flush();
            }
        }

        private void EnsureWriter(DateTime now)
        {
            var yearFolder = now.ToString("yyyy");
            var monthFolder = now.ToString(
                "MMMM",
                System.Globalization.CultureInfo.InvariantCulture);

            var directory = Path.Combine(_rootPath, yearFolder, monthFolder);
            Directory.CreateDirectory(directory);

            var baseName = $"genesis-{now:yyyyMMdd}";
            var desiredPath = Path.Combine(directory, baseName + ".log");

            if (_writer == null
                || !string.Equals(_currentFilePath, desiredPath, StringComparison.OrdinalIgnoreCase)
                || NeedsSizeRoll(desiredPath))
            {
                OpenWriter(directory, baseName);
            }
        }

        private bool NeedsSizeRoll(string filePath)
        {
            try
            {
                return File.Exists(filePath)
                    && new FileInfo(filePath).Length >= _fileSizeLimitBytes;
            }
            catch
            {
                return false;
            }
        }

        private void OpenWriter(string directory, string baseName)
        {
            _writer?.Dispose();

            var filePath = Path.Combine(directory, baseName + ".log");

            if (File.Exists(filePath)
                && new FileInfo(filePath).Length >= _fileSizeLimitBytes)
            {
                var index = 1;

                while (true)
                {
                    var candidate = Path.Combine(directory, $"{baseName}_{index}.log");

                    if (!File.Exists(candidate)
                        || new FileInfo(candidate).Length < _fileSizeLimitBytes)
                    {
                        filePath = candidate;
                        break;
                    }

                    index++;
                }
            }

            var stream = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite);

            _writer = new StreamWriter(stream) { AutoFlush = true };
            _currentFilePath = filePath;
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _writer?.Dispose();
                _writer = null;
                _currentFilePath = null;
            }
        }
    }
}
