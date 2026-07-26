using FluentAssertions;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Tests;

public class PolicyTests
{
    [Fact]
    public void Constructor_Should_ThrowDomainException_When_EndDateIsBeforeStartDate()
    {
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(-1);

        var act = () => new Policy("POL-2026-000001", Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, 1500m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_Should_SetStatusActive_When_ValidDatesProvided()
    {
        var policy = CreateActivePolicy();

        policy.Status.Should().Be(PolicyStatus.Active);
    }

    [Fact]
    public void Cancel_Should_TransitionToCancelled_When_StatusIsActive()
    {
        var policy = CreateActivePolicy();

        policy.Cancel();

        policy.Status.Should().Be(PolicyStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Should_ThrowDomainException_When_StatusIsNotActive()
    {
        var policy = CreateActivePolicy();
        policy.Cancel();

        var act = policy.Cancel;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ExpireIfPastEndDate_Should_TransitionToExpired_When_NowIsPastEndDate()
    {
        var policy = CreateActivePolicy();

        policy.ExpireIfPastEndDate(policy.EndDate.AddDays(1));

        policy.Status.Should().Be(PolicyStatus.Expired);
    }

    [Fact]
    public void ExpireIfPastEndDate_Should_NotChangeStatus_When_NowIsBeforeEndDate()
    {
        var policy = CreateActivePolicy();

        policy.ExpireIfPastEndDate(policy.StartDate);

        policy.Status.Should().Be(PolicyStatus.Active);
    }

    // --- Teminat penceresi (saat hassasiyetli): StartDate/EndDate satın alma ANINI taşır ---

    // Poliçe 22.07.2026 08:00'de başlar, 22.07.2027 08:00'de biter (1 yıllık gerçek satın alma modeli).
    private static Policy PolicyStartingAtEightAm() =>
        new(
            "POL-2026-000001", Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 22, 8, 0, 0, DateTimeKind.Utc),
            1500m);

    [Fact]
    public void CoversIncidentAt_Should_ReturnTrue_When_IncidentIsSameDayAfterStartTime()
    {
        // Kabul kriteri: aynı gün 08:00 başlangıç + 10:00 hasar geçerlidir.
        var policy = PolicyStartingAtEightAm();

        policy.CoversIncidentAt(new DateTime(2026, 7, 22, 10, 0, 0, DateTimeKind.Utc))
            .Should().BeTrue();
    }

    [Fact]
    public void CoversIncidentAt_Should_ReturnFalse_When_IncidentIsSameDayBeforeStartTime()
    {
        // 08:00 başlangıç + 07:59 hasar geçersizdir (poliçe aktif olmadan önce gerçekleşti).
        var policy = PolicyStartingAtEightAm();

        policy.CoversIncidentAt(new DateTime(2026, 7, 22, 7, 59, 0, DateTimeKind.Utc))
            .Should().BeFalse();
    }

    [Fact]
    public void CoversIncidentAt_Should_ReturnTrue_When_IncidentIsExactlyAtStartInstant()
    {
        // Sınır dahil: başlangıç anındaki olay geçerlidir.
        var policy = PolicyStartingAtEightAm();

        policy.CoversIncidentAt(policy.StartDate).Should().BeTrue();
    }

    [Fact]
    public void CoversIncidentAt_Should_ReturnTrue_When_IncidentIsExactlyAtEndInstant()
    {
        // Sınır dahil: bitiş anındaki olay geçerlidir.
        var policy = PolicyStartingAtEightAm();

        policy.CoversIncidentAt(policy.EndDate).Should().BeTrue();
    }

    [Fact]
    public void CoversIncidentAt_Should_ReturnFalse_When_IncidentIsAfterEndInstant()
    {
        // Bitişten bir saniye sonra geçersizdir.
        var policy = PolicyStartingAtEightAm();

        policy.CoversIncidentAt(policy.EndDate.AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void CoversIncidentAt_Should_ReturnFalse_When_IncidentIsPreviousDay()
    {
        var policy = PolicyStartingAtEightAm();

        policy.CoversIncidentAt(new DateTime(2026, 7, 21, 23, 59, 0, DateTimeKind.Utc))
            .Should().BeFalse();
    }

    [Fact]
    public void CoversIncidentAt_Should_ReturnTrue_When_IncidentIsNextDay()
    {
        var policy = PolicyStartingAtEightAm();

        policy.CoversIncidentAt(new DateTime(2026, 7, 23, 14, 0, 0, DateTimeKind.Utc))
            .Should().BeTrue();
    }

    private static Policy CreateActivePolicy()
    {
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddYears(1);
        return new Policy("POL-2026-000001", Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, 1500m);
    }
}
