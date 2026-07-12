using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Payments.DTOs;

namespace SigortaPro.Application.Features.Payments.Queries.GetPaymentHistory;

// Oturum sahibi müşterinin ödeme geçmişi (en yeni önce, sayfalanmış).
public sealed record GetPaymentHistoryQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<PaymentHistoryItemDto>>;
