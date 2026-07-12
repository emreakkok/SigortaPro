using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.Commands.StartClaimReview;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Claims;

public class StartClaimReviewCommandHandlerTests
{
    private readonly IClaimRepository _claimRepository = Substitute.For<IClaimRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly StartClaimReviewCommandHandler _handler;

    private readonly DateTime _incidentDate = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    public StartClaimReviewCommandHandlerTests()
    {
        _handler = new StartClaimReviewCommandHandler(
            _claimRepository, _unitOfWork, Substitute.For<ILogger<StartClaimReviewCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_TransitionToUnderReview_When_ClaimIsSubmitted()
    {
        var claim = ClaimTestData.SubmittedClaim(Guid.NewGuid(), Guid.NewGuid(), _incidentDate);
        _claimRepository.GetTrackedByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);

        var result = await _handler.Handle(new StartClaimReviewCommand(claim.Id), CancellationToken.None);

        claim.Status.Should().Be(ClaimStatus.UnderReview);
        result.Status.Should().Be(ClaimStatus.UnderReview);
        _claimRepository.Received(1).Update(claim);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_ClaimDoesNotExist()
    {
        var act = () => _handler.Handle(new StartClaimReviewCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowDomainException_When_ClaimNotSubmitted()
    {
        var claim = ClaimTestData.UnderReviewClaim(Guid.NewGuid(), Guid.NewGuid(), _incidentDate);
        _claimRepository.GetTrackedByIdAsync(claim.Id, Arg.Any<CancellationToken>()).Returns(claim);

        var act = () => _handler.Handle(new StartClaimReviewCommand(claim.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
