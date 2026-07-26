using FluentAssertions;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Tests.Entities;

public class QuoteInsuredPersonTests
{
    private static InsuredPerson SampleInsured() => new(
        "Ayşe", "Yılmaz", "10000000146",
        new DateTime(1955, 5, 1, 0, 0, 0, DateTimeKind.Utc), "+905321112233", "Anne");

    [Fact]
    public void SetInsuredPerson_Should_AssignInsured_When_HealthDraftQuote()
    {
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Saglik, null, null);

        quote.SetInsuredPerson(SampleInsured());

        quote.InsuredPerson.Should().NotBeNull();
        quote.InsuredPerson!.FullName.Should().Be("Ayşe Yılmaz");
        quote.InsuredPerson.Relationship.Should().Be("Anne");
    }

    [Fact]
    public void SetInsuredPerson_Should_Throw_When_BranchIsNotHealth()
    {
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);

        var act = () => quote.SetInsuredPerson(SampleInsured());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetInsuredPerson_Should_Throw_When_QuoteIsNotDraft()
    {
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Saglik, null, null);
        quote.MarkAsPriced(1000m, DateTime.UtcNow.AddDays(7));

        var act = () => quote.SetInsuredPerson(SampleInsured());

        act.Should().Throw<DomainException>();
    }
}
