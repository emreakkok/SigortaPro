using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteList;

// Teklif listesi: müşteri kendi tekliflerini, acente personeli tümünü görür. Durum/branş filtresi opsiyonel.
public sealed record GetQuoteListQuery(
    int Page = 1,
    int PageSize = 20,
    QuoteStatus? Status = null,
    InsuranceBranch? Branch = null) : IQuery<PagedResult<QuoteSummaryDto>>;
