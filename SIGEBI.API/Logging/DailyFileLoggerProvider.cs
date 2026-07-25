using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SIGEBI.API.Logging;

public sealed class DailyFileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly ConcurrentDictionary<string, DailyFileLogger> _loggers = new();

    public DailyFileLoggerProvider(string directory)
    {
        _directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(_directory);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, category => new DailyFileLogger(_directory, category));

    public void Dispose() => _loggers.Clear();

    private sealed class DailyFileLogger(string directory, string category) : ILogger
    {
        private static readonly object FileLock = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var line =
                $"{DateTimeOffset.Now:O} [{logLevel}] {category}: {formatter(state, exception)}" +
                (exception is null ? string.Empty : $"{Environment.NewLine}{exception}") +
                Environment.NewLine;
            var path = Path.Combine(directory, $"sigebi-{DateTime.UtcNow:yyyyMMdd}.log");

            lock (FileLock)
                File.AppendAllText(path, line);
        }
    }
}
