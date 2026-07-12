using FluentValidation.TestHelper;
using SigortaPro.Application.Features.Customers.Commands.AddProperty;

namespace SigortaPro.Application.Tests.Features.Customers;

public class AddPropertyCommandValidatorTests
{
    private readonly AddPropertyCommandValidator _validator = new();

    private static AddPropertyCommand Valid() => new(
        City: "İstanbul",
        District: "Kadıköy",
        Neighborhood: "Caferağa",
        PostalCode: "34710",
        BuildingAge: 10,
        SquareMeters: 120,
        EarthquakeZone: 1);

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_Should_HaveError_When_EarthquakeZoneIsOutOfRange(int zone)
    {
        var command = Valid() with { EarthquakeZone = zone };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.EarthquakeZone);
    }

    [Fact]
    public void Validate_Should_HaveError_When_SquareMetersIsNotPositive()
    {
        var command = Valid() with { SquareMeters = 0 };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.SquareMeters);
    }

    [Fact]
    public void Validate_Should_HaveError_When_CityIsEmpty()
    {
        var command = Valid() with { City = "" };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.City);
    }
}
