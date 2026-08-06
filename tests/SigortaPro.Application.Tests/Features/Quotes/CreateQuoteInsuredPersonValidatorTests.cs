using FluentAssertions;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Quotes;

// "başkası adına" sigortalı beyanının doğrulama kuralları.
public class CreateQuoteInsuredPersonValidatorTests
{
    private readonly CreateQuoteCommandValidator _validator = new();

    private static InsuredPersonInput ValidInsured() => new(
        "Ayşe", "Yılmaz", "10000000146",
        new DateTime(1955, 5, 1, 0, 0, 0, DateTimeKind.Utc), "+905321112233", "Anne");

    [Fact]
    public void Validate_Should_Pass_When_HealthQuoteWithValidInsured()
    {
        var command = new CreateQuoteCommand(
            InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, ValidInsured(), IsSmoker: false);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_InsuredProvidedForNonHealthBranch()
    {
        var command = new CreateQuoteCommand(
            InsuranceBranch.Kasko, Guid.NewGuid(), null, CoveragePackage.Standart, ValidInsured());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateQuoteCommand.Branch));
    }

    [Fact]
    public void Validate_Should_Fail_When_InsuredTcknIsInvalid()
    {
        var command = new CreateQuoteCommand(
            InsuranceBranch.Saglik, null, null, CoveragePackage.Standart,
            ValidInsured() with { Tckn = "12345678901" }, IsSmoker: false);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_InsuredPhoneFormatIsInvalid()
    {
        var command = new CreateQuoteCommand(
            InsuranceBranch.Saglik, null, null, CoveragePackage.Standart,
            ValidInsured() with { PhoneNumber = "05321112233" }, IsSmoker: false);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Pass_When_HealthQuoteWithoutInsured()
    {
        // Kendisi için sağlık teklifi — sigortalı beyanı opsiyoneldir; sigara beyanı ise zorunludur.
        var command = new CreateQuoteCommand(
            InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, IsSmoker: true);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }
}
