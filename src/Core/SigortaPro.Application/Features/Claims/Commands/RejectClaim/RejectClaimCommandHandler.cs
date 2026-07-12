using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Claims.Commands.RejectClaim;

public sealed class RejectClaimCommandHandler : ICommandHandler<RejectClaimCommand, ClaimSummaryDto>
{
    private readonly IClaimRepository _claimRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectClaimCommandHandler> _logger;

    public RejectClaimCommandHandler(
        IClaimRepository claimRepository,
        IUnitOfWork unitOfWork,
        ILogger<RejectClaimCommandHandler> logger)
    {
        _claimRepository = claimRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClaimSummaryDto> Handle(RejectClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetTrackedByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        // UnderReview değilse DomainException → UnhandledExceptionBehavior 409'a çevirir (ADR-013).
        claim.Reject(request.ReviewNote);

        _claimRepository.Update(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Hasar reddedildi. ClaimId: {ClaimId}", claim.Id);

        return ClaimMappings.ToSummaryDto(claim);
    }
}
