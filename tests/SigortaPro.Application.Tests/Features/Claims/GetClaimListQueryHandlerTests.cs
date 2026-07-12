using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Claims.Queries.GetClaimList;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Claims;

public class GetClaimListQueryHandlerTests
{
    private readonly IClaimRepository _claimRepository = Substitute.For<IClaimRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetClaimListQueryHandler _handler;

    public GetClaimListQueryHandlerTests()
    {
        _claimRepository.SearchAsync(
                Arg.Any<Guid?>(), Arg.Any<ClaimStatus?>(), Arg.Any<Guid?>(),
                Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Claim>(Array.Empty<Claim>(), 1, 20, 0));

        _handler = new GetClaimListQueryHandler(_claimRepository, _customerRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_ListAllClaims_When_CallerIsStaff()
    {
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        await _handler.Handle(new GetClaimListQuery(), CancellationToken.None);

        // Personel tüm hasarları görür → müşteri filtresi yok (null).
        // customerId ve policyId aynı tip (Guid?) olduğundan her ikisi de matcher ile belirtilir (NSubstitute kuralı).
        await _claimRepository.Received(1).SearchAsync(
            Arg.Is<Guid?>(customerId => customerId == null), Arg.Any<ClaimStatus?>(), Arg.Any<Guid?>(),
            Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_FilterToOwnClaims_When_CallerIsCustomer()
    {
        var appUserId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, Guid.NewGuid());
        _currentUserService.IsInRole(Arg.Any<string>()).Returns(false);
        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);

        await _handler.Handle(new GetClaimListQuery(), CancellationToken.None);

        await _claimRepository.Received(1).SearchAsync(
            Arg.Is<Guid?>(customerId => customerId == customer.Id), Arg.Any<ClaimStatus?>(), Arg.Any<Guid?>(),
            Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }
}
