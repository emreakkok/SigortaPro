using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;

namespace SigortaPro.Application.Features.Claims.Commands.PayClaim;

// Personel onaylanmış hasarın ödemesini gerçekleştirir (Approved → Paid).
public sealed record PayClaimCommand(Guid ClaimId) : ICommand<ClaimSummaryDto>;
