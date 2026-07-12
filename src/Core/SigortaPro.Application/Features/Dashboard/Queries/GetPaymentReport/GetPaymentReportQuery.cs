using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetPaymentReport;

// Tarih aralıklı ödeme raporu (işlem tarihine göre). From/To dahil (inclusive); sayfalanmış.
public sealed record GetPaymentReportQuery(
    DateTime From,
    DateTime To,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<PaymentReportItemDto>>;
