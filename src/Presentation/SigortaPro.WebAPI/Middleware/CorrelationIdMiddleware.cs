using Serilog.Context;
using SigortaPro.WebAPI.Extensions;

namespace SigortaPro.WebAPI.Middleware;

// Her isteğe bir korelasyon kimliği atar. Gelen "X-Correlation-ID" header'ı varsa korunur, yoksa üretilir.
// Kimlik hem yanıt header'ına eklenir hem de Serilog LogContext'e basılır; böylece isteğe ait tüm loglar aynı kimlikle etiketlenir.
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[WebApiConstants.CorrelationIdItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[WebApiConstants.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(WebApiConstants.CorrelationIdItemKey, correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(WebApiConstants.CorrelationIdHeader, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming))
        {
            return incoming.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}
