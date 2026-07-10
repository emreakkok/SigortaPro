using MediatR;
using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Domain.Common;

namespace SigortaPro.Application.Common.Behaviors;

public sealed class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

    public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DomainException ex)
        {
            // ADR-013: Domain'in DomainException'ı, Application sınırında BusinessRuleException'a (409) çevrilir.
            _logger.LogWarning(ex, "Domain kuralı ihlali: {RequestName}", typeof(TRequest).Name);
            throw new BusinessRuleException(ex.Message);
        }
        catch (SigortaProException)
        {
            // NotFoundException, ValidationException vb. zaten anlamlı HTTP durum koduna sahiptir; olduğu gibi yükselir.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen hata: {RequestName}", typeof(TRequest).Name);
            throw;
        }
    }
}
