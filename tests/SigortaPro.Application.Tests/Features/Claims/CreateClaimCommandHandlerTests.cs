using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.Commands.CreateClaim;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Claims;

public class CreateClaimCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IPolicyRepository _policyRepository = Substitute.For<IPolicyRepository>();
    private readonly IClaimRepository _claimRepository = Substitute.For<IClaimRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateClaimCommandHandler _handler;

    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Customer _customer;
    private readonly DateTime _now = new(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);

    public CreateClaimCommandHandlerTests()
    {
        _customer = CustomerTestData.CreateCustomer(_appUserId, Guid.NewGuid());
        _currentUserService.UserId.Returns(_appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>()).Returns(_customer);
        _dateTimeProvider.UtcNow.Returns(_now);

        _handler = new CreateClaimCommandHandler(
            _customerRepository, _policyRepository, _claimRepository, _dateTimeProvider,
            _currentUserService, _unitOfWork, Substitute.For<ILogger<CreateClaimCommandHandler>>());
    }

    private Policy ActivePolicyOwnedByCustomer() =>
        ClaimTestData.CreateActivePolicy(_customer.Id, _now.AddMonths(-1), _now.AddYears(1));

    private CreateClaimCommand CommandFor(Guid policyId, DateTime? incidentDate = null) => new(
        PolicyId: policyId,
        IncidentDate: incidentDate ?? _now.AddDays(-2),
        Description: "Ön tamponda hasar oluştu.",
        EstimatedAmount: 5000m);

    [Fact]
    public async Task Handle_Should_CreateSubmittedClaim_When_PolicyActiveAndIncidentInPeriod()
    {
        var policy = ActivePolicyOwnedByCustomer();
        _policyRepository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _handler.Handle(CommandFor(policy.Id), CancellationToken.None);

        result.Status.Should().Be(ClaimStatus.Submitted);
        result.PolicyNumber.Should().Be(policy.PolicyNumber);
        result.CustomerId.Should().Be(_customer.Id);
        await _claimRepository.Received(1).AddAsync(
            Arg.Is<Claim>(c => c.PolicyId == policy.Id && c.CustomerId == _customer.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowBusinessRule_When_PolicyNotActive()
    {
        var policy = ActivePolicyOwnedByCustomer();
        policy.Cancel(); // Active → Cancelled
        _policyRepository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);

        var act = () => _handler.Handle(CommandFor(policy.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await _claimRepository.DidNotReceive().AddAsync(Arg.Any<Claim>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowBusinessRule_When_IncidentOutsidePolicyPeriod()
    {
        var policy = ActivePolicyOwnedByCustomer();
        _policyRepository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);

        // Olay tarihi poliçe başlangıcından önce.
        var act = () => _handler.Handle(CommandFor(policy.Id, _now.AddMonths(-3)), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await _claimRepository.DidNotReceive().AddAsync(Arg.Any<Claim>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowBusinessRule_When_IncidentDateInFuture()
    {
        var policy = ActivePolicyOwnedByCustomer();
        _policyRepository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);

        // Poliçe dönemi içinde ama gelecekte bir tarih.
        var act = () => _handler.Handle(CommandFor(policy.Id, _now.AddDays(1)), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await _claimRepository.DidNotReceive().AddAsync(Arg.Any<Claim>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_PolicyBelongsToAnotherCustomer()
    {
        var policy = ClaimTestData.CreateActivePolicy(Guid.NewGuid(), _now.AddMonths(-1), _now.AddYears(1));
        _policyRepository.GetByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);

        var act = () => _handler.Handle(CommandFor(policy.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _claimRepository.DidNotReceive().AddAsync(Arg.Any<Claim>(), Arg.Any<CancellationToken>());
    }
}
