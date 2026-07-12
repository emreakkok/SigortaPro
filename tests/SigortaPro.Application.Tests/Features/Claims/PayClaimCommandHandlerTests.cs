using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.Commands.PayClaim;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Claims;

public class PayClaimCommandHandlerTests
{
    private readonly IClaimRepository _claimRepository = Substitute.For<IClaimRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PayClaimCommandHandler _handler;

    private readonly DateTime _incidentDate = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    public PayClaimCommandHandlerTests()
    {
        _handler = new PayClaimCommandHandler(
            _claimRepository, _unitOfWork, Substitute.For<ILogger<PayClaimCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_MarkClaimPaid_When_ClaimApproved()
    {
        var claim = ClaimTestData.ApprovedClaim(Guid.NewGuid(), Guid.NewGuid(), _incidentDate);
        _claimRepository.GetTrackedByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);

        var result = await _handler.Handle(new PayClaimCommand(claim.Id), CancellationToken.None);

        claim.Status.Should().Be(ClaimStatus.Paid);
        result.Status.Should().Be(ClaimStatus.Paid);
        _claimRepository.Received(1).Update(claim);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_ClaimNotApproved()
    {
        var claim = ClaimTestData.UnderReviewClaim(Guid.NewGuid(), Guid.NewGuid(), _incidentDate);
        _claimRepository.GetTrackedByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);

        var act = () => _handler.Handle(new PayClaimCommand(claim.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
