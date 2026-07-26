using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.Queries.GetPricingVersions;

public sealed class GetPricingVersionsQueryHandler
    : IQueryHandler<GetPricingVersionsQuery, IReadOnlyList<PricingVersionDto>>
{
    private const string BaselineNote =
        "Sistemin yerleşik baz tarifesi. İlk özel tarife yayınlanana kadar yeni teklifler bu fiyatları kullanır.";

    private readonly IPricingVersionRepository _pricingVersionRepository;
    private readonly IPricingBaselineProvider _baselineProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetPricingVersionsQueryHandler(
        IPricingVersionRepository pricingVersionRepository,
        IPricingBaselineProvider baselineProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _pricingVersionRepository = pricingVersionRepository;
        _baselineProvider = baselineProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<PricingVersionDto>> Handle(
        GetPricingVersionsQuery request, CancellationToken cancellationToken)
    {
        var versions = await _pricingVersionRepository.GetHistoryAsync(cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        var baselineRates = _baselineProvider.BaselineBasePremiums;

        // Kronolojik sıra: "bir önceki değer" karşılaştırması ve yürürlük tespiti bunun üzerinden yapılır.
        var chronological = versions
            .OrderBy(version => version.EffectiveFrom)
            .ThenBy(version => version.VersionNumber)
            .ToList();

        // Yürürlükteki gerçek versiyon: geçerlilik tarihi geçmiş olanların en yenisi. Hiçbiri yürürlükte
        // değilse (hiç yayınlanmamış veya yalnızca gelecek tarihli varsa) yerleşik baz tarife yürürlüktedir.
        var currentRealId = chronological.LastOrDefault(version => version.EffectiveFrom <= now)?.Id;

        // ADR-049: Yerleşik baz tarife her zaman zincirin başında (v0) yer alır → admin, hiç tarife
        // yayınlanmamışken bile yürürlükteki gerçek baz primleri görür ("Varsayılan" etiketi kalkar).
        var result = new List<PricingVersionDto>(chronological.Count + 1)
        {
            new(
                Id: Guid.Empty,
                VersionNumber: 0,
                EffectiveFrom: DateTime.MinValue,
                Note: BaselineNote,
                CreatedByName: null,
                CreatedAt: DateTime.MinValue,
                IsCurrent: currentRealId is null,
                IsScheduled: false,
                IsBaseline: true,
                Rates: MapBaselineRates(baselineRates)),
        };

        // Gerçek versiyonlar. "Önceki değer" bir önceki tarifedir; ilk versiyon için bu, yerleşik baz tarifedir.
        IReadOnlyDictionary<InsuranceBranch, decimal> previousRates = baselineRates;
        foreach (var version in chronological)
        {
            result.Add(new PricingVersionDto(
                version.Id,
                version.VersionNumber,
                version.EffectiveFrom,
                version.Note,
                version.CreatedByName,
                version.CreatedAt,
                IsCurrent: version.Id == currentRealId,
                IsScheduled: version.EffectiveFrom > now,
                IsBaseline: false,
                Rates: MapRates(version, previousRates)));

            previousRates = version.Rates.ToDictionary(rate => rate.Branch, rate => rate.BasePremium);
        }

        // Ekranda en yeni önce gösterilir (yerleşik baz tarife en altta kalır).
        result.Reverse();
        return result;
    }

    private static List<PricingBranchRateDto> MapBaselineRates(
        IReadOnlyDictionary<InsuranceBranch, decimal> baselineRates) =>
        baselineRates
            .OrderBy(rate => rate.Key)
            .Select(rate => new PricingBranchRateDto(rate.Key, rate.Value, PreviousBasePremium: null))
            .ToList();

    private static List<PricingBranchRateDto> MapRates(
        PricingVersion version, IReadOnlyDictionary<InsuranceBranch, decimal> previousRates) =>
        version.Rates
            .OrderBy(rate => rate.Branch)
            .Select(rate => new PricingBranchRateDto(
                rate.Branch,
                rate.BasePremium,
                previousRates.TryGetValue(rate.Branch, out var previous) ? previous : null))
            .ToList();
}
