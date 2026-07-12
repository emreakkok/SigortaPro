using FluentValidation.TestHelper;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Quotes;

public class CreateQuoteCommandValidatorTests
{
    private readonly CreateQuoteCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_HaveError_When_VehicleBranchWithoutVehicleId()
    {
        var command = new CreateQuoteCommand(InsuranceBranch.Kasko, null, null, CoveragePackage.Standart);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.VehicleId);
    }

    [Fact]
    public void Validate_Should_HaveError_When_PropertyBranchWithoutPropertyId()
    {
        var command = new CreateQuoteCommand(InsuranceBranch.Konut, null, null, CoveragePackage.Standart);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.PropertyId);
    }

    [Fact]
    public void Validate_Should_Pass_When_HealthBranchWithoutRiskObject()
    {
        var command = new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_When_KaskoWithVehicle()
    {
        var command = new CreateQuoteCommand(InsuranceBranch.Kasko, Guid.NewGuid(), null, CoveragePackage.Premium);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
