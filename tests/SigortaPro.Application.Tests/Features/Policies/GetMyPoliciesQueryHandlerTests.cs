using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Policies.Queries.GetMyPolicies;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Policies;

public class GetMyPoliciesQueryHandlerTests
{
    private readonly IPolicyRepository _policyRepository = Substitute.For<IPolicyRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetMyPoliciesQueryHandler _handler;

    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Customer _customer;

    public GetMyPoliciesQueryHandlerTests()
    {
        _customer = CustomerTestData.CreateCustomer(_appUserId, Guid.NewGuid());
        _currentUserService.UserId.Returns(_appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>()).Returns(_customer);
        _policyRepository.GetByCustomerPagedAsync(
                Arg.Any<Guid>(), Arg.Any<PolicyStatus?>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Policy>(Array.Empty<Policy>(), 1, 20, 0));

        _handler = new GetMyPoliciesQueryHandler(_policyRepository, _customerRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_FilterToOwnPolicies_When_CallerIsCustomer()
    {
        await _handler.Handle(new GetMyPoliciesQuery(Status: PolicyStatus.Active), CancellationToken.None);

        // Yalnızca oturum sahibi müşterinin poliçeleri; durum filtresi repository'ye aktarılır.
        await _policyRepository.Received(1).GetByCustomerPagedAsync(
            _customer.Id, PolicyStatus.Active, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_NoAuthenticatedUser()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var act = () => _handler.Handle(new GetMyPoliciesQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_CustomerRecordMissing()
    {
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>())
            .Returns((Customer?)null);

        var act = () => _handler.Handle(new GetMyPoliciesQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
