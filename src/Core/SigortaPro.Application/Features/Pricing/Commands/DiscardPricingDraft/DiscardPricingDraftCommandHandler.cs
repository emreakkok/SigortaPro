using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.Commands.DiscardPricingDraft;

// Taslağı soft-delete eder. Aktif/arşiv versiyon iptal edilemez — böylece hiçbir teklif/poliçenin
// sabitlediği tarife kaybolmaz (geçmiş primler yeniden hesaplanabilir kalır).
public sealed class DiscardPricingDraftCommandHandler : ICommandHandler<DiscardPricingDraftCommand>
{
    private readonly IPricingVersionRepository _pricingVersionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DiscardPricingDraftCommandHandler> _logger;

    public DiscardPricingDraftCommandHandler(
        IPricingVersionRepository pricingVersionRepository,
        IUnitOfWork unitOfWork,
        ILogger<DiscardPricingDraftCommandHandler> logger)
    {
        _pricingVersionRepository = pricingVersionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DiscardPricingDraftCommand request, CancellationToken cancellationToken)
    {
        var version = await _pricingVersionRepository.GetTrackedWithRatesByIdAsync(request.VersionId, cancellationToken)
            ?? throw new NotFoundException(nameof(PricingVersion), request.VersionId);

        if (version.Status != PricingVersionStatus.Draft)
        {
            // Aktif/arşiv versiyon geçmiş tekliflerin sabitlediği tarifedir → asla iptal edilemez.
            throw new DomainException("Yalnızca taslak durumundaki tarife versiyonu iptal edilebilir.");
        }

        _pricingVersionRepository.Delete(version);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Fiyatlandırma taslağı iptal edildi. Versiyon: {VersionNumber}", version.VersionNumber);
    }
}
