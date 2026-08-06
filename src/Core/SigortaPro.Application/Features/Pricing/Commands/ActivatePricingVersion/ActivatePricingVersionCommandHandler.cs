using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.Commands.ActivatePricingVersion;

// Taslak → Aktif. Önceki aktif versiyon ARŞİVLENİR (tek aktif invariant'ı). Bu işlem hiçbir mevcut
// teklif/poliçe kaydını GÜNCELLEMEZ — onlar kendi sabitledikleri versiyonla hesaplanmaya devam eder.
public sealed class ActivatePricingVersionCommandHandler
    : ICommandHandler<ActivatePricingVersionCommand, PricingVersionDto>
{
    private readonly IPricingVersionRepository _pricingVersionRepository;
    private readonly IPricingBaselineProvider _baselineProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ActivatePricingVersionCommandHandler> _logger;

    public ActivatePricingVersionCommandHandler(
        IPricingVersionRepository pricingVersionRepository,
        IPricingBaselineProvider baselineProvider,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILogger<ActivatePricingVersionCommandHandler> logger)
    {
        _pricingVersionRepository = pricingVersionRepository;
        _baselineProvider = baselineProvider;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<PricingVersionDto> Handle(
        ActivatePricingVersionCommand request, CancellationToken cancellationToken)
    {
        var draft = await _pricingVersionRepository.GetTrackedWithRatesByIdAsync(request.VersionId, cancellationToken)
            ?? throw new NotFoundException(nameof(PricingVersion), request.VersionId);

        // Önceki aktif → arşiv (varsa). Tek SaveChanges → atomik geçiş; aynı anda iki aktif versiyon oluşmaz.
        var currentActive = await _pricingVersionRepository.GetActiveAsync(cancellationToken);
        IReadOnlyDictionary<InsuranceBranch, decimal> previous;
        if (currentActive is not null)
        {
            previous = currentActive.Rates.ToDictionary(rate => rate.Branch, rate => rate.BasePremium);

            var trackedActive = await _pricingVersionRepository
                .GetTrackedWithRatesByIdAsync(currentActive.Id, cancellationToken);
            trackedActive?.Archive();
        }
        else
        {
            previous = new Dictionary<InsuranceBranch, decimal>(_baselineProvider.BaselineBasePremiums);
        }

        // Domain guard: yalnızca taslak aktifleştirilebilir + tüm branşları içermeli (DomainException).
        draft.Activate(_dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Fiyatlandırma versiyonu AKTİFLEŞTİRİLDİ. Versiyon: {VersionNumber}. Bundan sonraki teklifler bu tarifeyi kullanır.",
            draft.VersionNumber);

        return PricingMappings.ToDto(draft, previous, _baselineProvider);
    }
}
