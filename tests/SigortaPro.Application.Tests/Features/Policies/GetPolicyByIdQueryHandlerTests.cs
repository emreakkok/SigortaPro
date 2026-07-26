using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Policies.Queries.GetPolicyById;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Application.Tests.Common;

namespace SigortaPro.Application.Tests.Features.Policies;

public class GetPolicyByIdQueryHandlerTests
{
    private readonly IPolicyRepository _policyRepository = Substitute.For<IPolicyRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IPricingEngine _pricingEngine = Substitute.For<IPricingEngine>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetPolicyByIdQueryHandler _handler;

    private readonly DateTime _now = new(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);

    public GetPolicyByIdQueryHandlerTests()
    {
        _handler = new GetPolicyByIdQueryHandler(
            _policyRepository, _customerRepository, _pricingEngine, PricingTestDoubles.BaselineResolver(), _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_PolicyDoesNotExist()
    {
        _policyRepository.GetReadDetailByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Policy?)null);

        var act = () => _handler.Handle(new GetPolicyByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_PolicyBelongsToAnotherCustomer()
    {
        // Poliçe başka bir müşteriye ait.
        var policy = new Policy("POL-2026-000009", Guid.NewGuid(), Guid.NewGuid(), _now, _now.AddYears(1), 100m);
        _policyRepository.GetReadDetailByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);

        // Çağıran, poliçenin sahibi olmayan bir müşteri.
        var callerAppUserId = Guid.NewGuid();
        var caller = CustomerTestData.CreateCustomer(callerAppUserId, Guid.NewGuid());
        _currentUserService.IsInRole(Arg.Any<string>()).Returns(false);
        _currentUserService.UserId.Returns(callerAppUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(callerAppUserId, Arg.Any<CancellationToken>()).Returns(caller);

        var act = () => _handler.Handle(new GetPolicyByIdQuery(policy.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        // Sahiplik reddi, teminat/fiyat yeniden hesaplamasından önce gerçekleşir.
        _pricingEngine.DidNotReceive().CalculatePremium(Arg.Any<PricingRequest>());
    }
}
