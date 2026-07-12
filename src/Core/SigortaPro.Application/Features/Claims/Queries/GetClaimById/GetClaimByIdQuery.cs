using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;

namespace SigortaPro.Application.Features.Claims.Queries.GetClaimById;

// Hasar detayı. Sahibi müşteri veya acente personeli erişebilir.
public sealed record GetClaimByIdQuery(Guid ClaimId) : IQuery<ClaimDto>;
