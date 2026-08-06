using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteById;

public sealed class GetQuoteByIdQueryHandler : IQueryHandler<GetQuoteByIdQuery, QuoteDto>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingRateResolver _pricingRateResolver;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetQuoteByIdQueryHandler(
        IQuoteRepository quoteRepository,
        ICustomerRepository customerRepository,
        IPricingEngine pricingEngine,
        IPricingRateResolver pricingRateResolver,
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _pricingEngine = pricingEngine;
        _pricingRateResolver = pricingRateResolver;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<QuoteDto> Handle(GetQuoteByIdQuery request, CancellationToken cancellationToken)
    {
        var quote = await _quoteRepository.GetDetailByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new NotFoundException(nameof(Quote), request.QuoteId);

        await QuoteAuthorization.EnsureCanAccessAsync(
            quote.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        // Acente destekli teklifin üreten personel adı YALNIZCA personel yüzeyine çözülür (müşteriye "Acente"
        // olarak gösterilir; personel kimliği/adı müşteriye sızmaz — KVKK/gizlilik). Self-service ise null.
        string? createdByStaffName = null;
        if (quote.CreatedByStaffUserId is Guid staffUserId && QuoteAuthorization.IsStaff(_currentUserService))
        {
            var staff = await _identityService.GetByIdAsync(staffUserId, cancellationToken);
            createdByStaffName = staff?.FullName ?? staff?.Email;
        }

        // Prim dökümü, saklanan seçim + veri üzerinden oluşturma anındaki referansla (CreatedAt) yeniden
        // hesaplanır; deterministik motor sayesinde oluşturma anındaki değerlerle birebir aynıdır.
        // baz primler teklifin SABİTLEDİĞİ tarife versiyonundan okunur — admin tarifeyi
        // değiştirse bile bu teklifin primi/dökümü değişmez.
        var rates = await _pricingRateResolver.ResolveForQuoteAsync(quote.PricingVersionId, cancellationToken);

        var pricing = QuotePricingFactory.Compute(
            _pricingEngine,
            quote.Branch,
            quote.Customer!,
            quote.Vehicle,
            quote.Property,
            quote.InsuranceProduct!.Coverages,
            quote.CoveragePackage,
            quote.CreatedAt,
            quote.ClaimHistoryFactor,
            insuredBirthDate: quote.InsuredPerson?.BirthDate,
            rates: rates,
            // yeniden hesap teklifin DONDURULMUŞ girdilerinden yapılır (canlı entity'den değil).
            snapshot: quote.PricingSnapshot,
            // Yenileme indirimi teklifte donduruldu → yeniden hesapta aynı değer (deterministik).
            renewalDiscountFactor: quote.RenewalDiscountFactor);

        return QuoteMappings.ToDto(quote, quote.InsuranceProduct!, quote.Vehicle, quote.Property, pricing, createdByStaffName);
    }
}
