using FluentAssertions;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Domain.Tests;

public class RenewalTests
{
    [Fact]
    public void Accept_Should_SetIsAcceptedTrue_When_NotAlreadyAccepted()
    {
        var renewal = new Renewal(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var acceptedAt = DateTime.UtcNow;

        renewal.Accept(acceptedAt);

        renewal.IsAccepted.Should().BeTrue();
        renewal.AcceptedAt.Should().Be(acceptedAt);
    }

    [Fact]
    public void Accept_Should_ThrowDomainException_When_AlreadyAccepted()
    {
        var renewal = new Renewal(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        renewal.Accept(DateTime.UtcNow);

        var act = () => renewal.Accept(DateTime.UtcNow);

        act.Should().Throw<DomainException>();
    }
}
