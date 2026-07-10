using FluentAssertions;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Tests;

public class ClaimTests
{
    [Fact]
    public void Constructor_Should_SetStatusSubmitted_When_Created()
    {
        var claim = CreateSubmittedClaim();

        claim.Status.Should().Be(ClaimStatus.Submitted);
    }

    [Fact]
    public void StartReview_Should_TransitionToUnderReview_When_StatusIsSubmitted()
    {
        var claim = CreateSubmittedClaim();

        claim.StartReview();

        claim.Status.Should().Be(ClaimStatus.UnderReview);
    }

    [Fact]
    public void StartReview_Should_ThrowDomainException_When_StatusIsNotSubmitted()
    {
        var claim = CreateSubmittedClaim();
        claim.StartReview();

        var act = claim.StartReview;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Approve_Should_TransitionToApproved_When_StatusIsUnderReview()
    {
        var claim = CreateSubmittedClaim();
        claim.StartReview();

        claim.Approve(5000m, "Hasar tespiti onaylandı.");

        claim.Status.Should().Be(ClaimStatus.Approved);
        claim.ApprovedAmount.Should().Be(5000m);
    }

    [Fact]
    public void Reject_Should_TransitionToRejected_When_StatusIsUnderReview()
    {
        var claim = CreateSubmittedClaim();
        claim.StartReview();

        claim.Reject("Poliçe kapsamı dışında.");

        claim.Status.Should().Be(ClaimStatus.Rejected);
    }

    [Fact]
    public void MarkPaid_Should_TransitionToPaid_When_StatusIsApproved()
    {
        var claim = CreateSubmittedClaim();
        claim.StartReview();
        claim.Approve(5000m, null);

        claim.MarkPaid();

        claim.Status.Should().Be(ClaimStatus.Paid);
    }

    [Fact]
    public void MarkPaid_Should_ThrowDomainException_When_StatusIsNotApproved()
    {
        var claim = CreateSubmittedClaim();

        var act = claim.MarkPaid;

        act.Should().Throw<DomainException>();
    }

    private static Claim CreateSubmittedClaim()
    {
        return new Claim(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), "Ön camda çatlak oluştu.", 3000m);
    }
}
