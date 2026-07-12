using FluentValidation.TestHelper;
using SigortaPro.Application.Features.Claims.Commands.CreateClaim;

namespace SigortaPro.Application.Tests.Features.Claims;

public class CreateClaimCommandValidatorTests
{
    private readonly CreateClaimCommandValidator _validator = new();

    private static CreateClaimCommand ValidCommand(
        decimal estimatedAmount = 5000m,
        string description = "Ön tamponda hasar oluştu.",
        IReadOnlyList<string>? photoFileNames = null) =>
        new(Guid.NewGuid(), new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), description, estimatedAmount, photoFileNames);

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_HaveError_When_EstimatedAmountIsNotPositive()
    {
        _validator.TestValidate(ValidCommand(estimatedAmount: 0m))
            .ShouldHaveValidationErrorFor(command => command.EstimatedAmount);
    }

    [Fact]
    public void Validate_Should_HaveError_When_DescriptionIsEmpty()
    {
        _validator.TestValidate(ValidCommand(description: ""))
            .ShouldHaveValidationErrorFor(command => command.Description);
    }

    [Fact]
    public void Validate_Should_HaveError_When_TooManyPhotosUploaded()
    {
        var photos = Enumerable.Range(0, 11).Select(index => $"hasar-{index}.jpg").ToList();

        _validator.TestValidate(ValidCommand(photoFileNames: photos))
            .ShouldHaveValidationErrorFor(command => command.PhotoFileNames);
    }

    [Fact]
    public void Validate_Should_HaveError_When_PhotoHasDisallowedExtension()
    {
        var photos = new List<string> { "hasar.exe" };

        _validator.TestValidate(ValidCommand(photoFileNames: photos))
            .ShouldHaveValidationErrorFor("PhotoFileNames[0]");
    }
}
