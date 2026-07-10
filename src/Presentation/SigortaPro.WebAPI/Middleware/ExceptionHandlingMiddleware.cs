using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.WebAPI.Extensions;

namespace SigortaPro.WebAPI.Middleware;

// ARCHITECTURE_RULES.md §7.1: Tüm isteklerde çalışan global exception handling.
// Custom exception'ları uygun HTTP status koduna ve RFC 7807 ProblemDetails formatına (CODING_STANDARDS.md §5.4) çevirir.
// Bilinmeyen hatalar 500'e düşer; detay yalnızca geliştirme ortamında açığa çıkar.
public sealed class ExceptionHandlingMiddleware
{
    private const string ProblemJsonContentType = "application/problem+json";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // İstemci isteği iptal etti; bu bir sunucu hatası değildir. 499 (Client Closed Request) ile sessizce kapatılır.
            _logger.LogDebug("İstek istemci tarafından iptal edildi: {Path}", context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = MapToProblemDetails(context, exception);

        if (context.Response.HasStarted)
        {
            // Yanıt gövdesi yazılmaya başlandıysa müdahale edemeyiz; yalnızca loglarız.
            _logger.LogError(exception, "Yanıt başladıktan sonra hata oluştu, ProblemDetails yazılamıyor: {Path}", context.Request.Path);
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = ProblemJsonContentType;

        await context.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType(), options: null, contentType: ProblemJsonContentType, context.RequestAborted);
    }

    private ProblemDetails MapToProblemDetails(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var correlationId = context.Items.TryGetValue(WebApiConstants.CorrelationIdItemKey, out var value)
            ? value?.ToString()
            : null;

        ProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationException:
                _logger.LogWarning("Doğrulama hatası: {Path}", context.Request.Path);
                problemDetails = new ValidationProblemDetails(validationException.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Doğrulama hatası",
                    Type = WebApiConstants.ErrorTypeBaseUri + "validation",
                    Detail = validationException.Message,
                };
                break;

            case NotFoundException:
                _logger.LogWarning("Kaynak bulunamadı: {Path} — {Message}", context.Request.Path, exception.Message);
                problemDetails = CreateProblem(StatusCodes.Status404NotFound, "Kaynak bulunamadı", "not-found", exception.Message);
                break;

            case ForbiddenAccessException:
                _logger.LogWarning("Erişim reddedildi: {Path} — {Message}", context.Request.Path, exception.Message);
                problemDetails = CreateProblem(StatusCodes.Status403Forbidden, "Erişim reddedildi", "forbidden", exception.Message);
                break;

            case BusinessRuleException:
                _logger.LogWarning("İş kuralı ihlali: {Path} — {Message}", context.Request.Path, exception.Message);
                problemDetails = CreateProblem(StatusCodes.Status409Conflict, "İş kuralı ihlali", "business-rule", exception.Message);
                break;

            case SigortaProException:
                // Taban tipe düşen diğer uygulama hataları (ileride eklenen türler) 400 olarak yansıtılır.
                _logger.LogWarning("Uygulama hatası: {Path} — {Message}", context.Request.Path, exception.Message);
                problemDetails = CreateProblem(StatusCodes.Status400BadRequest, "Geçersiz istek", "application", exception.Message);
                break;

            default:
                _logger.LogError(exception, "Beklenmeyen sunucu hatası: {Path}", context.Request.Path);
                problemDetails = CreateProblem(
                    StatusCodes.Status500InternalServerError,
                    "Sunucu hatası",
                    "internal",
                    // Üretimde iç hata mesajı sızdırılmaz; yalnızca geliştirmede açığa çıkar.
                    _environment.IsDevelopment() ? exception.Message : "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                break;
        }

        problemDetails.Extensions["traceId"] = traceId;
        if (!string.IsNullOrEmpty(correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        return problemDetails;
    }

    private static ProblemDetails CreateProblem(int status, string title, string typeSlug, string detail) => new()
    {
        Status = status,
        Title = title,
        Type = WebApiConstants.ErrorTypeBaseUri + typeSlug,
        Detail = detail,
    };
}
