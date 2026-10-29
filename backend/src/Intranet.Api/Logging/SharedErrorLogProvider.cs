using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Intranet.Api.Logging;

public sealed class SharedErrorLogProvider : ILoggerProvider
{
    private readonly string directory;
    private readonly string fallback;
    private readonly IHttpContextAccessor accessor;
    private readonly Channel<Pending> queue = Channel.CreateUnbounded<Pending>(new UnboundedChannelOptions { SingleReader = true });
    private readonly SemaphoreSlim sharedWrite = new(1, 1);
    private readonly Task worker;
    private sealed record Pending(object? Entry, TaskCompletionSource? Completion = null);
    private static readonly Regex Secrets = new(@"(?i)\b(password|pwd|token|authorization|SigningPrivateKey)\s*[:=]\s*(?:""[^""]*""|'[^']*'|[^;\s,}]+)", RegexOptions.Compiled);
    public SharedErrorLogProvider(string directory, string fallback, IHttpContextAccessor accessor)
    {
        this.directory = directory; this.fallback = fallback; this.accessor = accessor;
        worker = Task.Run(Process);
    }
    public ILogger CreateLogger(string categoryName) => new ErrorLogger(this, categoryName);
    public static string Redact(string text) => Secrets.Replace(text, "$1=[REDACTED]");
    private void Enqueue<TState>(string category, LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var context = accessor.HttpContext;
        var entry = new
        {
            timestamp = DateTimeOffset.UtcNow, server = Environment.MachineName,
            level = level.ToString(), category, eventId = eventId.Id,
            traceId = context?.TraceIdentifier, user = context?.User.Identity?.Name,
            method = context?.Request.Method, path = context?.Request.Path.Value,
            message = Redact(formatter(state, exception)),
            exception = exception is null ? null : Redact(exception.ToString())
        };
        if (!queue.Writer.TryWrite(new(entry)))
            Console.Error.WriteLine("Intranet: logger cerrado. TraceId: " + context?.TraceIdentifier);
    }
    private async Task Process()
    {
        await foreach (var pending in queue.Reader.ReadAllAsync())
        {
            if (pending.Completion is {} completion) { completion.TrySetResult(); continue; }
            try
            {
                var now = DateTimeOffset.UtcNow;
                var name = $"error-{now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.json";
                var json = JsonSerializer.Serialize(pending.Entry, new JsonSerializerOptions { WriteIndented = true });
                var saved = false;
                // Only one potentially blocked share write may remain in flight.
                if (sharedWrite.Wait(0))
                {
                    var attempt = Task.Run(async () =>
                    {
                        try { await Write(directory, name, json); return true; }
                        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { return false; }
                        finally { sharedWrite.Release(); }
                    });
                    try { saved = await attempt.WaitAsync(TimeSpan.FromSeconds(2)); }
                    catch (TimeoutException) { }
                }
                if (!saved) await Write(fallback, name, json);
            }
            catch (Exception e)
            {
                // Do not log through ILogger here: that would recurse into this provider.
                Console.Error.WriteLine("Intranet: no se pudo guardar el log de error. " + e.GetType().Name);
            }
        }
    }
    private static async Task Write(string folder, string name, string json)
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, name), json);
    }
    public async Task FlushAsync()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await queue.Writer.WriteAsync(new(null, completion));
        await completion.Task;
    }
    public void Dispose()
    {
        queue.Writer.TryComplete();
        try { worker.Wait(TimeSpan.FromSeconds(3)); } catch (AggregateException) { }
    }
    private sealed class ErrorLogger(SharedErrorLogProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Error && level != LogLevel.None;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) owner.Enqueue(category, logLevel, eventId, state, exception, formatter);
        }
    }
}

