using System.Text;
using FluentValidation.TestHelper;
using SigortaPro.Application.Features.Claims.Commands.CreateClaim;

namespace SigortaPro.Application.Tests.Features.Claims;

public class CreateClaimCommandValidatorTests
{
    private readonly CreateClaimCommandValidator _validator = new();

    private static byte[] SmallImage() => Encoding.UTF8.GetBytes("fake-image-bytes");

    private static CreateClaimCommand ValidCommand(
        decimal estimatedAmount = 5000m,
        string description = "Ön tamponda hasar oluştu.",
        IReadOnlyList<CreateClaimDocument>? documents = null) =>
        new(Guid.NewGuid(), new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), description, estimatedAmount, documents);

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_ValidDocumentsAttached()
    {
        var documents = new List<CreateClaimDocument>
        {
            new("hasar.jpg", "image/jpeg", SmallImage()),
            new("belge.pdf", "application/pdf", SmallImage()),
        };

        _validator.TestValidate(ValidCommand(documents: documents)).ShouldNotHaveAnyValidationErrors();
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
    public void Validate_Should_HaveError_When_TooManyDocumentsUploaded()
    {
        var documents = Enumerable.Range(0, 6)
            .Select(index => new CreateClaimDocument($"hasar-{index}.jpg", "image/jpeg", SmallImage()))
            .ToList();

        _validator.TestValidate(ValidCommand(documents: documents))
            .ShouldHaveValidationErrorFor(command => command.Documents);
    }

    [Fact]
    public void Validate_Should_HaveError_When_DocumentHasDisallowedContentType()
    {
        var documents = new List<CreateClaimDocument> { new("hasar.exe", "application/octet-stream", SmallImage()) };

        _validator.TestValidate(ValidCommand(documents: documents))
            .ShouldHaveValidationErrorFor("Documents[0].ContentType");
    }

    [Fact]
    public void Validate_Should_HaveError_When_DocumentExceedsSizeLimit()
    {
        var tooBig = new byte[(3 * 1024 * 1024) + 1];
        var documents = new List<CreateClaimDocument> { new("buyuk.jpg", "image/jpeg", tooBig) };

        _validator.TestValidate(ValidCommand(documents: documents))
            .ShouldHaveValidationErrorFor("Documents[0].Content");
    }
}
