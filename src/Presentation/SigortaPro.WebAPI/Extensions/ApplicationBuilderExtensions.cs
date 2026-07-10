using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using SigortaPro.WebAPI.Middleware;

namespace SigortaPro.WebAPI.Extensions;

// WebAPI HTTP pipeline kurulum yardımcıları. Çapraz kesit middleware'lerinin sırasını merkezî ve okunur tutar.
public static class ApplicationBuilderExtensions
{
    // Çapraz kesit middleware'lerini doğru sırada ekler:
    // korelasyon → güvenlik header'ları → istek loglama → global exception handling.
    public static IApplicationBuilder UseSigortaProCrossCutting(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseSerilogRequestLoggingWithCorrelation();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        return app;
    }

    // Serilog istek özeti logunu, korelasyon kimliği ve kullanıcı bilgisiyle zenginleştirerek ekler (ADR-011).
    private static void UseSerilogRequestLoggingWithCorrelation(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                if (httpContext.Items.TryGetValue(WebApiConstants.CorrelationIdItemKey, out var correlationId))
                {
                    diagnosticContext.Set("CorrelationId", correlationId?.ToString());
                }

                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            };
        });
    }

    // /health uç noktasını JSON yanıt formatıyla eşler.
    public static void MapSigortaProHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponseAsync,
        });
    }

    private static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
            }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
