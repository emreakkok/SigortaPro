using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Claims.Commands.PayClaim;

public sealed class PayClaimCommandHandler : ICommandHandler<PayClaimCommand, ClaimSummaryDto>
{
    private readonly IClaimRepository _claimRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PayClaimCommandHandler> _logger;

    public PayClaimCommandHandler(
        IClaimRepository claimRepository,
        IUnitOfWork unitOfWork,
        ILogger<PayClaimCommandHandler> logger)
    {
        _claimRepository = claimRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClaimSummaryDto> Handle(PayClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetTrackedByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        // Approved değilse DomainException → UnhandledExceptionBehavior 409'a çevirir.
        claim.MarkPaid();

        _claimRepository.Update(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hasar ödemesi tamamlandı. ClaimId: {ClaimId}", claim.Id);

        return ClaimMappings.ToSummaryDto(claim);
    }
}
