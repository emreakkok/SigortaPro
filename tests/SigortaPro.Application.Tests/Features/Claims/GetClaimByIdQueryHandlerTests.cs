using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.Queries.GetClaimById;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Claims;

public class GetClaimByIdQueryHandlerTests
{
    private readonly IClaimRepository _claimRepository = Substitute.For<IClaimRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetClaimByIdQueryHandler _handler;

    private readonly DateTime _incidentDate = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    public GetClaimByIdQueryHandlerTests()
    {
        _handler = new GetClaimByIdQueryHandler(_claimRepository, _customerRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_ReturnClaim_When_CallerIsStaff()
    {
        var claim = ClaimTestData.SubmittedClaim(Guid.NewGuid(), Guid.NewGuid(), _incidentDate);
        _claimRepository.GetDetailByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);
        _currentUserService.IsInRole(Roles.Personel).Returns(true);

        var result = await _handler.Handle(new GetClaimByIdQuery(claim.Id), CancellationToken.None);

        result.Id.Should().Be(claim.Id);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_ClaimDoesNotExist()
    {
        var act = () => _handler.Handle(new GetClaimByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_ClaimBelongsToAnotherCustomer()
    {
        var claim = ClaimTestData.SubmittedClaim(Guid.NewGuid(), Guid.NewGuid(), _incidentDate);
        _claimRepository.GetDetailByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);

        var appUserId = Guid.NewGuid();
        var otherCustomer = CustomerTestData.CreateCustomer(appUserId, Guid.NewGuid());
        _currentUserService.IsInRole(Arg.Any<string>()).Returns(false);
        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(otherCustomer);

        var act = () => _handler.Handle(new GetClaimByIdQuery(claim.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
