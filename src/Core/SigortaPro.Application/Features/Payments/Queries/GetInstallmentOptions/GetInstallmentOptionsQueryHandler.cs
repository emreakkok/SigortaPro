using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Payments.Queries.GetInstallmentOptions;

public sealed class GetInstallmentOptionsQueryHandler
    : IQueryHandler<GetInstallmentOptionsQuery, IReadOnlyList<InstallmentOptionDto>>
{
    private readonly IReadRepository<Quote> _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;

    public GetInstallmentOptionsQueryHandler(
        IReadRepository<Quote> quoteRepository,
        ICustomerRepository customerRepository,
        IPaymentService paymentService,
        ICurrentUserService currentUserService)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _paymentService = paymentService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<InstallmentOptionDto>> Handle(
        GetInstallmentOptionsQuery request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new NotFoundException(nameof(Quote), request.QuoteId);

        await QuoteAuthorization.EnsureCanAccessAsync(
            quote.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        if (quote.Status != QuoteStatus.Approved)
        {
            throw new BusinessRuleException("Taksit seçenekleri yalnızca onaylanmış teklif için görüntülenebilir.");
        }

        return _paymentService.GetInstallmentOptions(quote.TotalPremium)
            .Select(option => new InstallmentOptionDto(option.Count, option.MonthlyAmount, option.TotalAmount))
            .ToList();
    }
}
