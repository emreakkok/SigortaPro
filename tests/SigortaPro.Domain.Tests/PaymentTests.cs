using FluentAssertions;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Tests;

public class PaymentTests
{
    [Fact]
    public void MarkSuccessful_Should_TransitionToSuccessful_When_StatusIsPending()
    {
        var payment = CreatePendingPayment();

        payment.MarkSuccessful("PROV-REF-123");

        payment.Status.Should().Be(PaymentStatus.Successful);
        payment.ProviderReferenceCode.Should().Be("PROV-REF-123");
    }

    [Fact]
    public void MarkFailed_Should_TransitionToFailed_When_StatusIsPending()
    {
        var payment = CreatePendingPayment();

        payment.MarkFailed("Yetersiz bakiye.");

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("Yetersiz bakiye.");
    }

    [Fact]
    public void MarkSuccessful_Should_ThrowDomainException_When_StatusIsNotPending()
    {
        var payment = CreatePendingPayment();
        payment.MarkSuccessful(null);

        var act = () => payment.MarkSuccessful(null);

        act.Should().Throw<DomainException>();
    }

    private static Payment CreatePendingPayment()
    {
        return new Payment(Guid.NewGuid(), Guid.NewGuid(), 1500m, 1, "**** **** **** 1234", DateTime.UtcNow);
    }
}
