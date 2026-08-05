using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;

// ADR-048: TASLAK versiyon oluşturur. Mevcut aktif versiyonun değerlerini kopyalayarak seed eder → admin
// güncel tarifeden başlar. Hiçbir mevcut versiyona/teklife DOKUNMAZ (canlı fiyatlar değişmez).
public sealed class CreatePricingVersionCommandHandler
    : ICommandHandler<CreatePricingVersionCommand, PricingVersionDto>
{
    private readonly IPricingVersionRepository _pricingVersionRepository;
    private readonly IPricingBaselineProvider _baselineProvider;
    private readonly INotificationContextResolver _contextResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreatePricingVersionCommandHandler> _logger;

    public CreatePricingVersionCommandHandler(
        IPricingVersionRepository pricingVersionRepository,
        IPricingBaselineProvider baselineProvider,
        INotificationContextResolver contextResolver,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreatePricingVersionCommandHandler> logger)
    {
        _pricingVersionRepository = pricingVersionRepository;
        _baselineProvider = baselineProvider;
        _contextResolver = contextResolver;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<PricingVersionDto> Handle(
        CreatePricingVersionCommand request, CancellationToken cancellationToken)
    {
        // Aynı anda tek taslak: açık taslak varsa yenisini oluşturmaz, mevcudu döner (idempotent).
        var existingDraft = await _pricingVersionRepository.GetDraftAsync(cancellationToken);
        if (existingDraft is not null)
        {
            return PricingMappings.ToDto(existingDraft, PreviousBasePremiums(existingDraft), _baselineProvider);
        }

        var active = await _pricingVersionRepository.GetActiveAsync(cancellationToken);

        // Seed: aktif versiyonun değerleri; aktif yoksa yerleşik baseline. Aktif versiyonun kural setinde
        // henüz bir faktör grubu yoksa (eski kayıt) baseline ile TAMAMLANIR → taslak her zaman tam set taşır.
        var seedBasePremiums = active is not null
            ? active.Rates.ToDictionary(rate => rate.Branch, rate => rate.BasePremium)
            : new Dictionary<Domain.Enums.InsuranceBranch, decimal>(_baselineProvider.BaselineBasePremiums);
        var seedRuleSet = active?.RuleSet is { } activeRuleSet
            ? PricingMappings.Complete(activeRuleSet, _baselineProvider)
            : PricingMappings.BuildBaselineRuleSet(_baselineProvider);

        var actor = await _contextResolver.ResolveActorAsync(cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        var versionNumber = await _pricingVersionRepository.GetNextVersionNumberAsync(cancellationToken);
        var name = request.Name.Trim();

        var draft = new PricingVersion(versionNumber, name, now, note: null, actor.UserId, actor.DisplayName);
        draft.UpdateDraft(name, now, effectiveTo: null, note: null, seedRuleSet, seedBasePremiums);

        await _pricingVersionRepository.AddAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Yeni fiyatlandırma TASLAĞI oluşturuldu. Versiyon: {VersionNumber}, Ad: {Name}, Oluşturan: {Actor}",
            draft.VersionNumber, name, actor.DisplayName);

        return PricingMappings.ToDto(draft, seedBasePremiums, _baselineProvider);
    }

    // Taslağın "önceki değer" kıyası, seed edildiği aktif/baseline değerleridir.
    private IReadOnlyDictionary<Domain.Enums.InsuranceBranch, decimal> PreviousBasePremiums(PricingVersion draft) =>
        draft.Rates.ToDictionary(rate => rate.Branch, rate => rate.BasePremium);
}
