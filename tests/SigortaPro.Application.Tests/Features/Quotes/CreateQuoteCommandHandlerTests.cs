using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Application.Tests.Common;

namespace SigortaPro.Application.Tests.Features.Quotes;

public class CreateQuoteCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IInsuranceProductRepository _productRepository = Substitute.For<IInsuranceProductRepository>();
    private readonly IReadRepository<Vehicle> _vehicleRepository = Substitute.For<IReadRepository<Vehicle>>();
    private readonly IReadRepository<Property> _propertyRepository = Substitute.For<IReadRepository<Property>>();
    private readonly IQuoteRepository _quoteRepository = Substitute.For<IQuoteRepository>();
    private readonly IPricingEngine _pricingEngine = Substitute.For<IPricingEngine>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateQuoteCommandHandler _handler;

    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Customer _customer;

    public CreateQuoteCommandHandlerTests()
    {
        _customer = CustomerTestData.CreateCustomer(_appUserId, Guid.NewGuid());

        _currentUserService.UserId.Returns(_appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>()).Returns(_customer);
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc));
        _pricingEngine.CalculatePremium(Arg.Any<PricingRequest>()).Returns(new PricingResult(
            InsuranceBranch.Kasko, 15000m, 20000m, RiskScore.Medium,
            new List<PricingBreakdownItem> { new("Sürücü Yaşı", 1.30m, "Genç sürücü") }));

        _handler = new CreateQuoteCommandHandler(
            _customerRepository, _productRepository, _vehicleRepository, _propertyRepository,
            _quoteRepository, _pricingEngine, PricingTestDoubles.BaselineResolver(),
            PricingTestDoubles.InputBuilder(), _dateTimeProvider, _currentUserService, _unitOfWork,
            Substitute.For<ILogger<CreateQuoteCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_CreatePricedQuote_When_VehicleOwnedByCustomer()
    {
        var vehicle = CustomerTestData.CreateVehicle(_customer.Id, Guid.NewGuid());
        var product = QuoteTestData.CreateProduct(InsuranceBranch.Kasko);
        _productRepository.GetActiveByBranchAsync(InsuranceBranch.Kasko, Arg.Any<CancellationToken>()).Returns(product);
        _vehicleRepository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var command = new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, CoveragePackage.Standart);

        var result = await _handler.Handle(command, CancellationToken.None);

        // Standart paket → prim motorun döndüğü tutar (20000).
        result.TotalPremium.Should().Be(20000m);
        result.BasePremium.Should().Be(15000m);
        result.RiskScore.Should().Be(RiskScore.Medium);
        result.Status.Should().Be(QuoteStatus.Priced);
        result.Coverages.Should().HaveCount(2);

        await _quoteRepository.Received(1).AddAsync(
            Arg.Is<Quote>(quote =>
                quote.CustomerId == _customer.Id &&
                quote.Branch == InsuranceBranch.Kasko &&
                quote.VehicleId == vehicle.Id &&
                quote.Status == QuoteStatus.Priced &&
                quote.TotalPremium == 20000m),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_VehicleBelongsToAnotherCustomer()
    {
        var otherCustomerId = Guid.NewGuid();
        var vehicle = CustomerTestData.CreateVehicle(otherCustomerId, Guid.NewGuid());
        var product = QuoteTestData.CreateProduct(InsuranceBranch.Kasko);
        _productRepository.GetActiveByBranchAsync(InsuranceBranch.Kasko, Arg.Any<CancellationToken>()).Returns(product);
        _vehicleRepository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var command = new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, CoveragePackage.Standart);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _quoteRepository.DidNotReceive().AddAsync(Arg.Any<Quote>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_NoActiveProductForBranch()
    {
        _productRepository.GetActiveByBranchAsync(InsuranceBranch.Kasko, Arg.Any<CancellationToken>())
            .Returns((InsuranceProduct?)null);

        var command = new CreateQuoteCommand(InsuranceBranch.Kasko, Guid.NewGuid(), null, CoveragePackage.Standart);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Acente destekli teklif (agent-assisted): personel müşteri ADINA teklif oluşturur ────────────────
    [Fact]
    public async Task Handle_Should_SetCreatedByStaff_AndOwnByCustomer_When_StaffCreatesForCustomer()
    {
        var staffUserId = Guid.NewGuid();
        var targetCustomer = CustomerTestData.CreateCustomer(Guid.NewGuid(), Guid.NewGuid());
        var vehicle = CustomerTestData.CreateVehicle(targetCustomer.Id, Guid.NewGuid());
        var product = QuoteTestData.CreateProduct(InsuranceBranch.Kasko);

        // Oturum sahibi = personel; hedef müşteri route/komuttaki CustomerId ile çözülür.
        _currentUserService.UserId.Returns(staffUserId);
        _currentUserService.IsInRole(Roles.Admin).Returns(true);
        _customerRepository.GetTrackedByIdAsync(targetCustomer.Id, Arg.Any<CancellationToken>()).Returns(targetCustomer);
        _productRepository.GetActiveByBranchAsync(InsuranceBranch.Kasko, Arg.Any<CancellationToken>()).Returns(product);
        _vehicleRepository.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var command = new CreateQuoteCommand(
            InsuranceBranch.Kasko, vehicle.Id, null, CoveragePackage.Standart, CustomerId: targetCustomer.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        // Sahip müşteri, üreten personel ise oturum sahibidir.
        result.CustomerId.Should().Be(targetCustomer.Id);
        await _quoteRepository.Received(1).AddAsync(
            Arg.Is<Quote>(quote =>
                quote.CustomerId == targetCustomer.Id &&
                quote.CreatedByStaffUserId == staffUserId &&
                quote.Status == QuoteStatus.Priced),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_NonStaffProvidesCustomerId()
    {
        // Müşteri (staff değil) başka bir müşteri adına teklif oluşturmaya çalışır → reddedilir.
        _currentUserService.IsInRole(Arg.Any<string>()).Returns(false);

        var command = new CreateQuoteCommand(
            InsuranceBranch.Kasko, Guid.NewGuid(), null, CoveragePackage.Standart, CustomerId: Guid.NewGuid());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _quoteRepository.DidNotReceive().AddAsync(Arg.Any<Quote>(), Arg.Any<CancellationToken>());
    }
}
