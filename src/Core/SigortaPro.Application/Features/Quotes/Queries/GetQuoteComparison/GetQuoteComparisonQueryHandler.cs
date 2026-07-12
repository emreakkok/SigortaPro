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
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public GetQuoteComparisonQueryHandler(
        ICustomerRepository customerRepository,
        IInsuranceProductRepository productRepository,
        IReadRepository<Vehicle> vehicleRepository,
        IReadRepository<Property> propertyRepository,
        IPricingEngine pricingEngine,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _vehicleRepository = vehicleRepository;
        _propertyRepository = propertyRepository;
        _pricingEngine = pricingEngine;
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

        var packages = CoveragePackageFactors.ComparablePackages
            .Select(package =>
            {
                var pricing = QuotePricingFactory.Compute(
                    _pricingEngine, request.Branch, customer, vehicle, property,
                    product.Coverages, package, now);

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
