using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Renewals.Queries.GetMyRenewals;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Renewals;

public class GetMyRenewalsQueryHandlerTests
{
    private readonly IRenewalRepository _renewalRepository = Substitute.For<IRenewalRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetMyRenewalsQueryHandler _handler;

    public GetMyRenewalsQueryHandlerTests()
    {
        _renewalRepository.GetByCustomerAsync(Arg.Any<Guid>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Renewal>(Array.Empty<Renewal>(), 1, 20, 0));

        _handler = new GetMyRenewalsQueryHandler(_renewalRepository, _customerRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_QueryOwnRenewals_When_CustomerAuthenticated()
    {
        var appUserId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, Guid.NewGuid());
        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);

        await _handler.Handle(new GetMyRenewalsQuery(), CancellationToken.None);

        await _renewalRepository.Received(1).GetByCustomerAsync(
            customer.Id, Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_NoAuthenticatedUser()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var act = () => _handler.Handle(new GetMyRenewalsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
