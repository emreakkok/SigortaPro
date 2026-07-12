using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Renewals.DTOs;

namespace SigortaPro.Application.Features.Renewals.Queries.GetMyRenewals;

// Oturum sahibi müşterinin yenileme teklifleri (en yeni önce, sayfalanmış).
public sealed record GetMyRenewalsQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<RenewalDto>>;
