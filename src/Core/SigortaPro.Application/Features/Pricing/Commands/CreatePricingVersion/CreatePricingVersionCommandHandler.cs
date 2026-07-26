using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;

// ADR-048: Yeni tarife versiyonu oluşturur. Mevcut versiyonlara DOKUNMAZ ve hiçbir teklif/poliçe
// kaydını güncellemez — geçmiş fiyatlar kayıt düzeyinde değişmez kalır.
public sealed class CreatePricingVersionCommandHandler
    : ICommandHandler<CreatePricingVersionCommand, PricingVersionDto>
{
    private readonly IPricingVersionRepository _pricingVersionRepository;
    private readonly INotificationContextResolver _contextResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreatePricingVersionCommandHandler> _logger;

    public CreatePricingVersionCommandHandler(
        IPricingVersionRepository pricingVersionRepository,
        INotificationContextResolver contextResolver,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreatePricingVersionCommandHandler> logger)
    {
        _pricingVersionRepository = pricingVersionRepository;
        _contextResolver = contextResolver;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<PricingVersionDto> Handle(
        CreatePricingVersionCommand request, CancellationToken cancellationToken)
    {
        // Değişikliği yapan admin: stabil kimlik + o andaki görünen ad snapshot'ı (ADR-047 deseni yeniden
        // kullanılır — fiyatlandırma için ayrı bir audit altyapısı kurulmaz).
        var actor = await _contextResolver.ResolveActorAsync(cancellationToken);

        var versionNumber = await _pricingVersionRepository.GetNextVersionNumberAsync(cancellationToken);

        var version = new PricingVersion(
            versionNumber,
            request.EffectiveFrom,
            string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            actor.UserId,
            actor.DisplayName);

        foreach (var rate in request.Rates)
        {
            version.SetRate(rate.Branch, rate.BasePremium);
        }

        // Kısmi tarife yayınlanamaz — aksi halde bazı branşlar sessizce baseline'a düşerdi.
        version.EnsureCoversAllBranches();

        await _pricingVersionRepository.AddAsync(version, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Yeni fiyatlandırma versiyonu oluşturuldu. Versiyon: {VersionNumber}, Geçerlilik: {EffectiveFrom}, Oluşturan: {Actor}",
            version.VersionNumber, version.EffectiveFrom, actor.DisplayName);

        var now = _dateTimeProvider.UtcNow;
        return new PricingVersionDto(
            version.Id,
            version.VersionNumber,
            version.EffectiveFrom,
            version.Note,
            version.CreatedByName,
            version.CreatedAt,
            IsCurrent: version.EffectiveFrom <= now,
            IsScheduled: version.EffectiveFrom > now,
            IsBaseline: false,
            version.Rates
                .OrderBy(rate => rate.Branch)
                .Select(rate => new PricingBranchRateDto(rate.Branch, rate.BasePremium, null))
                .ToList());
    }
}
