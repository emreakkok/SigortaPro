using FluentAssertions;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Tests;

public class QuoteTests
{
    [Fact]
    public void Constructor_Should_ThrowDomainException_When_KaskoWithoutVehicleId()
    {
        var act = () => new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Kasko, vehicleId: null, propertyId: null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_Should_ThrowDomainException_When_KonutWithoutPropertyId()
    {
        var act = () => new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Konut, vehicleId: null, propertyId: null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_Should_SetStatusDraft_When_RequiredRiskObjectProvided()
    {
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Kasko, vehicleId: Guid.NewGuid(), propertyId: null);

        quote.Status.Should().Be(QuoteStatus.Draft);
    }

    [Fact]
    public void MarkAsPriced_Should_TransitionToPriced_When_StatusIsDraft()
    {
        var quote = CreateDraftQuote();
        var validUntil = DateTime.UtcNow.AddDays(7);

        quote.MarkAsPriced(1500m, validUntil);

        quote.Status.Should().Be(QuoteStatus.Priced);
        quote.TotalPremium.Should().Be(1500m);
        quote.ValidUntil.Should().Be(validUntil);
    }

    [Fact]
    public void MarkAsPriced_Should_ThrowDomainException_When_StatusIsNotDraft()
    {
        var quote = CreateDraftQuote();
        quote.MarkAsPriced(1500m, DateTime.UtcNow.AddDays(7));

        var act = () => quote.MarkAsPriced(2000m, DateTime.UtcNow.AddDays(7));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Approve_Should_TransitionToApproved_When_StatusIsPriced()
    {
        var quote = CreateDraftQuote();
        quote.MarkAsPriced(1500m, DateTime.UtcNow.AddDays(7));

        quote.Approve();

        quote.Status.Should().Be(QuoteStatus.Approved);
    }

    [Fact]
    public void Approve_Should_ThrowDomainException_When_StatusIsNotPriced()
    {
        var quote = CreateDraftQuote();

        var act = quote.Approve;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Purchase_Should_TransitionToPurchased_When_StatusIsApproved()
    {
        var quote = CreateDraftQuote();
        quote.MarkAsPriced(1500m, DateTime.UtcNow.AddDays(7));
        quote.Approve();

        quote.Purchase();

        quote.Status.Should().Be(QuoteStatus.Purchased);
    }

    [Fact]
    public void Purchase_Should_ThrowDomainException_When_StatusIsNotApproved()
    {
        var quote = CreateDraftQuote();

        var act = quote.Purchase;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_Should_ThrowDomainException_When_StatusIsPurchased()
    {
        var quote = CreateDraftQuote();
        quote.MarkAsPriced(1500m, DateTime.UtcNow.AddDays(7));
        quote.Approve();
        quote.Purchase();

        var act = quote.Reject;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Expire_Should_TransitionToExpired_When_ValidUntilHasPassed()
    {
        var quote = CreateDraftQuote();
        var validUntil = DateTime.UtcNow.AddDays(-1);
        quote.MarkAsPriced(1500m, validUntil);

        quote.Expire(DateTime.UtcNow);

        quote.Status.Should().Be(QuoteStatus.Expired);
    }

    [Fact]
    public void Expire_Should_ThrowDomainException_When_ValidUntilHasNotPassed()
    {
        var quote = CreateDraftQuote();
        quote.MarkAsPriced(1500m, DateTime.UtcNow.AddDays(7));

        var act = () => quote.Expire(DateTime.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SelectCoveragePackage_Should_SetPackage_When_StatusIsDraft()
    {
        var quote = CreateDraftQuote();

        quote.SelectCoveragePackage(CoveragePackage.Premium);

        quote.CoveragePackage.Should().Be(CoveragePackage.Premium);
    }

    [Fact]
    public void SelectCoveragePackage_Should_DefaultToStandart_When_NotSelected()
    {
        var quote = CreateDraftQuote();

        quote.CoveragePackage.Should().Be(CoveragePackage.Standart);
    }

    [Fact]
    public void SelectCoveragePackage_Should_ThrowDomainException_When_StatusIsNotDraft()
    {
        var quote = CreateDraftQuote();
        quote.MarkAsPriced(1500m, DateTime.UtcNow.AddDays(7));

        var act = () => quote.SelectCoveragePackage(CoveragePackage.Premium);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ClaimHistoryFactor_Should_DefaultToOne_When_NotApplied()
    {
        var quote = CreateDraftQuote();

        quote.ClaimHistoryFactor.Should().Be(1.00m);
    }




    private static Quote CreateDraftQuote()
    {
        return new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Kasko, vehicleId: Guid.NewGuid(), propertyId: null);
    }
}
