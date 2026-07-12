using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetPolicyReport;

// Tarih aralıklı poliçe raporu (başlangıç tarihine göre). From/To dahil (inclusive); sayfalanmış.
public sealed record GetPolicyReportQuery(
    DateTime From,
    DateTime To,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<PolicyReportItemDto>>;
