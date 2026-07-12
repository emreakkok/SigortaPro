using FluentAssertions;
using FluentValidation.TestHelper;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;

namespace SigortaPro.Application.Tests.Features.Customers;

public class AddVehicleCommandValidatorTests
{
    private readonly AddVehicleCommandValidator _validator = new();

    private static AddVehicleCommand Valid() => new(
        PlateNumber: "34 ABC 123",
        Brand: "Toyota",
        Model: "Corolla",
        ManufactureYear: 2022,
        EnginePowerHp: 132);

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("ABC 123")]     // il kodu yok
    [InlineData("99 ABC 123")]  // geçersiz il kodu
    [InlineData("")]
    public void Validate_Should_HaveError_When_PlateIsInvalid(string plate)
    {
        var command = Valid() with { PlateNumber = plate };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.PlateNumber);
    }

    [Fact]
    public void Validate_Should_HaveError_When_ManufactureYearIsTooOld()
    {
        var command = Valid() with { ManufactureYear = 1900 };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.ManufactureYear);
    }

    [Fact]
    public void Validate_Should_HaveError_When_EnginePowerIsNotPositive()
    {
        var command = Valid() with { EnginePowerHp = 0 };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.EnginePowerHp);
    }

    [Fact]
    public void Validate_Should_AcceptNextModelYear()
    {
        var command = Valid() with { ManufactureYear = DateTime.UtcNow.Year + 1 };

        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(c => c.ManufactureYear);
    }
}
