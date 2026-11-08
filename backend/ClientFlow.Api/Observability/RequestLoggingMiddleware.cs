using System.Diagnostics;
namespace ClientFlow.Api.Observability;

internal sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.TraceIdentifier = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;

            return Task.CompletedTask;
        });
        var started = Stopwatch.GetTimestamp();
        await next(context);
        // Route patterns contain no user input or invitation tokens. Never log bodies/headers.
        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
        logger.LogInformation("HTTP {Method} {Route} returned {StatusCode} in {ElapsedMs}ms; trace {TraceId}",
            context.Request.Method, route, context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds, context.TraceIdentifier);
    }
}
