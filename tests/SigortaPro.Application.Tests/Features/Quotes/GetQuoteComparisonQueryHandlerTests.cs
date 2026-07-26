using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Quotes.Queries.GetQuoteComparison;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Application.Tests.Common;

namespace SigortaPro.Application.Tests.Features.Quotes;

public class GetQuoteComparisonQueryHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IInsuranceProductRepository _productRepository = Substitute.For<IInsuranceProductRepository>();
    private readonly IReadRepository<Vehicle> _vehicleRepository = Substitute.For<IReadRepository<Vehicle>>();
    private readonly IReadRepository<Property> _propertyRepository = Substitute.For<IReadRepository<Property>>();
    private readonly IPricingEngine _pricingEngine = Substitute.For<IPricingEngine>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetQuoteComparisonQueryHandler _handler;

    public GetQuoteComparisonQueryHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc));
        _pricingEngine.CalculatePremium(Arg.Any<PricingRequest>()).Returns(new PricingResult(
            InsuranceBranch.Kasko, 15000m, 15000m, RiskScore.Low,
            new List<PricingBreakdownItem> { new("Sürücü Yaşı", 1.00m, "Standart") }));

        _handler = new GetQuoteComparisonQueryHandler(
            _customerRepository, _productRepository, _vehicleRepository, _propertyRepository,
            _pricingEngine, PricingTestDoubles.BaselineResolver(), PricingTestDoubles.InputBuilder(),
            _dateTimeProvider, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_ProduceScaledPackageAlternatives_When_VehicleOwned()
    {
        var appUserId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, Guid.NewGuid());
        var vehicle = CustomerTestData.CreateVehicle(customer.Id, Guid.NewGuid());
        var product = QuoteTestData.CreateProduct(InsuranceBranch.Kasko);

        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);
        _productRepository.GetActiveByBranchAsync(InsuranceBranch.Kasko, Arg.Any<CancellationToken>()).Returns(product);
        _vehicleRepository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var query = new GetQuoteComparisonQuery(InsuranceBranch.Kasko, vehicle.Id, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Packages.Should().HaveCount(3);

        var standart = result.Packages.Single(p => p.CoveragePackage == CoveragePackage.Standart);
        var genisletilmis = result.Packages.Single(p => p.CoveragePackage == CoveragePackage.Genisletilmis);
        var premium = result.Packages.Single(p => p.CoveragePackage == CoveragePackage.Premium);

        // Prim ölçeği: Standart ×1.00, Genişletilmiş ×1.30, Premium ×1.60.
        standart.TotalPremium.Should().Be(15000m);
        genisletilmis.TotalPremium.Should().Be(19500m);
        premium.TotalPremium.Should().Be(24000m);

        // Teminat limiti ölçeği: Premium pakette ×2.00 (100000 → 200000).
        premium.Coverages.Single(c => c.Name == "Teminat A").Limit.Should().Be(200000m);
        // Risk skoru pakete göre değişmez (risk faktörlerinden gelir).
        premium.RiskScore.Should().Be(RiskScore.Low);
        // Prim dökümüne teminat paketi çarpanı satırı eklenir.
        premium.PremiumBreakdown.Should().ContainSingle(item => item.Factor == "Teminat Paketi");

        result.RiskObject.Kind.Should().Be("Araç");
    }
}
