using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteComparison;

public sealed class GetQuoteComparisonQueryHandler : IQueryHandler<GetQuoteComparisonQuery, QuoteComparisonDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IInsuranceProductRepository _productRepository;
    private readonly IReadRepository<Vehicle> _vehicleRepository;
    private readonly IReadRepository<Property> _propertyRepository;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingRateResolver _pricingRateResolver;
    private readonly IQuotePricingInputBuilder _pricingInputBuilder;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public GetQuoteComparisonQueryHandler(
        ICustomerRepository customerRepository,
        IInsuranceProductRepository productRepository,
        IReadRepository<Vehicle> vehicleRepository,
        IReadRepository<Property> propertyRepository,
        IPricingEngine pricingEngine,
        IPricingRateResolver pricingRateResolver,
        IQuotePricingInputBuilder pricingInputBuilder,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _vehicleRepository = vehicleRepository;
        _propertyRepository = propertyRepository;
        _pricingEngine = pricingEngine;
        _pricingRateResolver = pricingRateResolver;
        _pricingInputBuilder = pricingInputBuilder;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task<QuoteComparisonDto> Handle(GetQuoteComparisonQuery request, CancellationToken cancellationToken)
    {
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var product = await _productRepository.GetActiveByBranchAsync(request.Branch, cancellationToken)
            ?? throw new NotFoundException(nameof(InsuranceProduct), request.Branch);

        var (vehicle, property) = await QuoteRiskObjectResolver.ResolveAsync(
            request.Branch, request.VehicleId, request.PropertyId, customer.Id,
            _vehicleRepository, _propertyRepository, cancellationToken);

        var now = _dateTimeProvider.UtcNow;

        // ADR-048: Karşılaştırma bir ÖNİZLEMEDİR (henüz teklif yok) → o an yürürlükteki tarife kullanılır.
        // Müşteri teklifi oluşturduğunda aynı tarife teklifte sabitlenir; gösterilen fiyatla tutarlıdır.
        var effectivePricing = await _pricingRateResolver.ResolveEffectiveAsync(now, cancellationToken);

        // ADR-056: Girdi, teklif oluşturmayla AYNI builder'dan kurulur (aynı yaş/il/araç primitifleri,
        // aynı sigara beyanı, adresten AYNI şekilde türetilen deprem bölgesi). Önizleme snapshot'ı
        // KALICILAŞTIRILMAZ — yalnızca fiyatı oluşturmayla birebir aynı hesaplamak için kullanılır.
        var snapshot = await _pricingInputBuilder.BuildAsync(
            request.Branch, customer, vehicle, property, now,
            insuredBirthDate: request.InsuredBirthDate,
            isSmoker: request.IsSmoker,
            cancellationToken: cancellationToken);

        var packages = CoveragePackageFactors.ComparablePackages
            .Select(package =>
            {
                var pricing = QuotePricingFactory.Compute(
                    _pricingEngine, request.Branch, customer, vehicle, property,
                    product.Coverages, package, now,
                    insuredBirthDate: request.InsuredBirthDate,
                    rates: effectivePricing.Rates,
                    snapshot: snapshot);

                return new QuotePackageDto(
                    package,
                    pricing.RiskScore,
                    pricing.TotalPremium,
                    pricing.Coverages,
                    pricing.Breakdown);
            })
            .ToList();

        var riskObject = QuoteMappings.BuildRiskObject(vehicle, property);

        return new QuoteComparisonDto(request.Branch, product.Name, riskObject, packages);
    }
}
