using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.Commands.UpdatePricingDraft;

// TASLAK versiyonu düzenler. Domain, yalnızca taslağın düzenlenmesine izin verir (aktif/arşiv
// versiyonda UpdateDraft DomainException fırlatır) → geçmiş primler yapısal olarak korunur.
public sealed class UpdatePricingDraftCommandHandler
    : ICommandHandler<UpdatePricingDraftCommand, PricingVersionDto>
{
    private readonly IPricingVersionRepository _pricingVersionRepository;
    private readonly IPricingBaselineProvider _baselineProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePricingDraftCommandHandler> _logger;

    public UpdatePricingDraftCommandHandler(
        IPricingVersionRepository pricingVersionRepository,
        IPricingBaselineProvider baselineProvider,
        IUnitOfWork unitOfWork,
        ILogger<UpdatePricingDraftCommandHandler> logger)
    {
        _pricingVersionRepository = pricingVersionRepository;
        _baselineProvider = baselineProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PricingVersionDto> Handle(
        UpdatePricingDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _pricingVersionRepository.GetTrackedWithRatesByIdAsync(request.VersionId, cancellationToken)
            ?? throw new NotFoundException(nameof(PricingVersion), request.VersionId);

        // Girdiler tekilleştirilir (son değer kazanır); domain zaten branş tekilliğini ayrıca doğrular.
        var rates = new Dictionary<InsuranceBranch, decimal>();
        foreach (var rate in request.Rates)
        {
            rates[rate.Branch] = rate.BasePremium;
        }

        var packageFactors = new Dictionary<CoveragePackage, decimal>();
        foreach (var factor in request.PackagePremiumFactors)
        {
            packageFactors[factor.Package] = factor.PremiumFactor;
        }

        var cityCoefficients = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var city in request.CityRiskCoefficients)
        {
            cityCoefficients[city.City.Trim()] = city.Coefficient;
        }

        var ruleSet = new PricingRuleSet(
            packageFactors,
            cityCoefficients,
            request.DefaultCityRiskCoefficient,
            request.RenewalDiscountFactor,
            DriverAgeFactors: request.DriverAgeFactors,
            VehicleAgeFactors: request.VehicleAgeFactors,
            EnginePowerFactors: request.EnginePowerFactors,
            VehicleUsageFactors: request.VehicleUsageFactors,
            BonusMalusFactors: request.BonusMalusFactors,
            BuildingAgeFactors: request.BuildingAgeFactors,
            SquareMetersFactors: request.SquareMetersFactors,
            EarthquakeZoneFactors: request.EarthquakeZoneFactors,
            HealthAgeFactors: request.HealthAgeFactors,
            SmokerSurcharge: request.SmokerSurcharge);

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        // Domain guard: yalnızca taslak → aktif/arşiv düzenlenemez (DomainException). Eksik branş → DomainException.
        draft.UpdateDraft(request.Name, request.EffectiveFrom, request.EffectiveTo, note, ruleSet, rates);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Fiyatlandırma taslağı güncellendi. Versiyon: {VersionNumber}", draft.VersionNumber);

        // "Önceki değer" kıyası aktif versiyona (yoksa yerleşik baseline) göredir.
        var active = await _pricingVersionRepository.GetActiveAsync(cancellationToken);
        var previous = active is not null
            ? active.Rates.ToDictionary(rate => rate.Branch, rate => rate.BasePremium)
            : new Dictionary<InsuranceBranch, decimal>(_baselineProvider.BaselineBasePremiums);

        return PricingMappings.ToDto(draft, previous, _baselineProvider);
    }
}
