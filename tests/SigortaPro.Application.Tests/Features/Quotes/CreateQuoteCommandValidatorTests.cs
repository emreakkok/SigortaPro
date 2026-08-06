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
        // Sağlıkta sigara beyanı zorunludur; risk objesi ise gerekmez.
        var command = new CreateQuoteCommand(
            InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, IsSmoker: false);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_HaveError_When_HealthQuoteHasNoSmokerDeclaration()
    {
        // Beyan alınmadan sağlık teklifi oluşturulamaz — sessizce "sigara içmiyor" varsayılmaz.
        var command = new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.IsSmoker);
    }

    [Fact]
    public void Validate_Should_HaveError_When_NonHealthQuoteSendsSmokerDeclaration()
    {
        // Sigara beyanı yalnızca Sağlıkta anlamlıdır; diğer branşlarda sessizce yok sayılmaz, reddedilir.
        var command = new CreateQuoteCommand(
            InsuranceBranch.Kasko, Guid.NewGuid(), null, CoveragePackage.Standart, IsSmoker: true);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.IsSmoker);
    }

    [Fact]
    public void Validate_Should_Pass_When_KaskoWithVehicle()
    {
        var command = new CreateQuoteCommand(InsuranceBranch.Kasko, Guid.NewGuid(), null, CoveragePackage.Premium);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
