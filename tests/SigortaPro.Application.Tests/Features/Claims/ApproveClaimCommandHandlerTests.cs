using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.Commands.ApproveClaim;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Claims;

public class ApproveClaimCommandHandlerTests
{
    private readonly IClaimRepository _claimRepository = Substitute.For<IClaimRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ApproveClaimCommandHandler _handler;

    private readonly DateTime _incidentDate = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    public ApproveClaimCommandHandlerTests()
    {
        _handler = new ApproveClaimCommandHandler(
            _claimRepository, _unitOfWork, Substitute.For<ILogger<ApproveClaimCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_ApproveClaimWithAmountAndNote_When_ClaimUnderReview()
    {
        var claim = ClaimTestData.UnderReviewClaim(Guid.NewGuid(), Guid.NewGuid(), _incidentDate);
        _claimRepository.GetTrackedByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);

        var result = await _handler.Handle(
            new ApproveClaimCommand(claim.Id, 4200m, "Onaylandı."), CancellationToken.None);

        claim.Status.Should().Be(ClaimStatus.Approved);
        claim.ApprovedAmount.Should().Be(4200m);
        result.ApprovedAmount.Should().Be(4200m);
        _claimRepository.Received(1).Update(claim);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_ClaimNotUnderReview()
    {
        var claim = ClaimTestData.SubmittedClaim(Guid.NewGuid(), Guid.NewGuid(), _incidentDate);
        _claimRepository.GetTrackedByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);

        var act = () => _handler.Handle(
            new ApproveClaimCommand(claim.Id, 4200m, "Onaylandı."), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
