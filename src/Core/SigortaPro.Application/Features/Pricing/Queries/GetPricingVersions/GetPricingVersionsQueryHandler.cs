using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.Queries.GetPricingVersions;

public sealed class GetPricingVersionsQueryHandler
    : IQueryHandler<GetPricingVersionsQuery, IReadOnlyList<PricingVersionDto>>
{
    private const string BaselineNote =
        "Sistemin yerleşik baz tarifesi. İlk özel tarife aktifleştirilene kadar yeni teklifler bu fiyatları kullanır.";

    private readonly IPricingVersionRepository _pricingVersionRepository;
    private readonly IPricingBaselineProvider _baselineProvider;

    public GetPricingVersionsQueryHandler(
        IPricingVersionRepository pricingVersionRepository,
        IPricingBaselineProvider baselineProvider)
    {
        _pricingVersionRepository = pricingVersionRepository;
        _baselineProvider = baselineProvider;
    }

    public async Task<IReadOnlyList<PricingVersionDto>> Handle(
        GetPricingVersionsQuery request, CancellationToken cancellationToken)
    {
        var versions = await _pricingVersionRepository.GetHistoryAsync(cancellationToken);
        var baselineRates = _baselineProvider.BaselineBasePremiums;

        // Kronolojik sıra: "bir önceki değer" karşılaştırması bunun üzerinden yapılır (değişim göstergesi için).
        var chronological = versions
            .OrderBy(version => version.CreatedAt)
            .ThenBy(version => version.VersionNumber)
            .ToList();

        var hasActive = chronological.Any(version => version.Status == PricingVersionStatus.Active);

        // Yerleşik baz tarife her zaman zincirin başında (v0) yer alır → admin, hiç tarife
        // aktifleştirilmemişken bile yürürlükteki gerçek baz primleri görür.
        var result = new List<PricingVersionDto>(chronological.Count + 1)
        {
            new(
                Id: Guid.Empty,
                VersionNumber: 0,
                Name: null,
                Status: PricingVersionStatus.Archived,
                EffectiveFrom: DateTime.MinValue,
                EffectiveTo: null,
                ActivatedAt: null,
                Note: BaselineNote,
                CreatedByName: null,
                CreatedAt: DateTime.MinValue,
                IsCurrent: !hasActive,
                IsBaseline: true,
                Rates: baselineRates
                    .OrderBy(rate => rate.Key)
                    .Select(rate => new PricingBranchRateDto(rate.Key, rate.Value, PreviousBasePremium: null))
                    .ToList(),
                RuleSet: PricingMappings.ToRuleSetDto(null, _baselineProvider)),
        };

        // Gerçek versiyonlar. "Önceki değer" bir önceki (kronolojik) tarifedir; ilki için yerleşik baz tarife.
        IReadOnlyDictionary<InsuranceBranch, decimal> previousRates = baselineRates;
        foreach (var version in chronological)
        {
            result.Add(PricingMappings.ToDto(version, previousRates, _baselineProvider));
            previousRates = version.Rates.ToDictionary(rate => rate.Branch, rate => rate.BasePremium);
        }

        // Ekranda en yeni önce gösterilir (yerleşik baz tarife en altta kalır).
        result.Reverse();
        return result;
    }
}
