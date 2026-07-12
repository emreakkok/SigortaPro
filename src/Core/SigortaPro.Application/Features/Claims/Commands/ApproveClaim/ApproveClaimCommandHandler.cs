using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Claims.Commands.ApproveClaim;

public sealed class ApproveClaimCommandHandler : ICommandHandler<ApproveClaimCommand, ClaimSummaryDto>
{
    private readonly IClaimRepository _claimRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveClaimCommandHandler> _logger;

    public ApproveClaimCommandHandler(
        IClaimRepository claimRepository,
        IUnitOfWork unitOfWork,
        ILogger<ApproveClaimCommandHandler> logger)
    {
        _claimRepository = claimRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClaimSummaryDto> Handle(ApproveClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetTrackedByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        // UnderReview değilse DomainException → UnhandledExceptionBehavior 409'a çevirir (ADR-013).
        claim.Approve(request.ApprovedAmount, request.ReviewNote);

        _claimRepository.Update(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Hasar onaylandı. ClaimId: {ClaimId}, OnaylananTutar: {ApprovedAmount}",
            claim.Id, claim.ApprovedAmount);

        return ClaimMappings.ToSummaryDto(claim);
    }
}
