using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Claims.Queries.GetClaimList;

// Hasar listesi: müşteri kendi hasarlarını, acente personeli tümünü görür. Durum/poliçe filtresi opsiyonel.
public sealed record GetClaimListQuery(
    int Page = 1,
    int PageSize = 20,
    ClaimStatus? Status = null,
    Guid? PolicyId = null) : IQuery<PagedResult<ClaimSummaryDto>>;
