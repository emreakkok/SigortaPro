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

    private static Policy CreateActivePolicy()
    {
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddYears(1);
        return new Policy("POL-2026-000001", Guid.NewGuid(), Guid.NewGuid(), startDate, endDate, 1500m);
    }
}
