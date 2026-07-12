using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;

namespace SigortaPro.Application.Features.Claims.Commands.StartClaimReview;

// Personel bildirilen hasarı incelemeye alır (Submitted → UnderReview).
public sealed record StartClaimReviewCommand(Guid ClaimId) : ICommand<ClaimSummaryDto>;
