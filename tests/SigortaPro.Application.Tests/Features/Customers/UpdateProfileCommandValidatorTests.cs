using FluentValidation.TestHelper;
using SigortaPro.Application.Features.Customers.Commands.UpdateProfile;

namespace SigortaPro.Application.Tests.Features.Customers;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    private static UpdateProfileCommand Valid() => new(
        FirstName: "Ayşe",
        LastName: "Kaya",
        PhoneNumber: "+905321234567",
        City: "Ankara",
        District: "Çankaya",
        Neighborhood: "Kızılay",
        PostalCode: "06420");

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("05321234567")]  // +90 prefiksi yok
    [InlineData("+9053212345")]  // eksik hane
    [InlineData("")]
    public void Validate_Should_HaveError_When_PhoneIsInvalid(string phone)
    {
        var command = Valid() with { PhoneNumber = phone };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.PhoneNumber);
    }

    [Fact]
    public void Validate_Should_HaveError_When_FirstNameIsEmpty()
    {
        var command = Valid() with { FirstName = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.FirstName);
    }
}
