using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LessonOrganizer.Api.IntegrationTests.Logging;

public sealed record CapturedLog(string Category, LogLevel Level, string Text);

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLog> _logs = new();

    public IReadOnlyCollection<CapturedLog> Logs => _logs.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _logs);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<CapturedLog> logs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var text = $"{formatter(state, exception)} {state} {exception}";
            logs.Enqueue(new CapturedLog(category, logLevel, text));
        }
    }
}
