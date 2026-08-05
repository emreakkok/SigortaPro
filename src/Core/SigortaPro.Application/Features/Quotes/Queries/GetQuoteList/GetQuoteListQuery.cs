using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteList;

// Teklif listesi: müşteri kendi tekliflerini, acente personeli tümünü görür. Durum/branş filtresi opsiyonel.
// Search: müşteri adı/soyadı/tam adı veya telefon (format bağımsız). Müşteri kapsamında güvenli çalışır
// (customerId filtresi arama ile birlikte uygulanır → müşteri başkasının teklifini arayamaz).
// CreatedByMe (acente destekli, additive): personel çağrısında true ise yalnızca OTURUM SAHİBİ personelin
// müşteri adına oluşturduğu teklifler döner ("benim oluşturduklarım"). Müşteri çağrısında yok sayılır (müşteri
// zaten yalnızca kendi tekliflerini görür). null/false → mevcut davranış (personel tümünü, müşteri kendisini).
public sealed record GetQuoteListQuery(
    int Page = 1,
    int PageSize = 20,
    QuoteStatus? Status = null,
    InsuranceBranch? Branch = null,
    string? Search = null,
    bool CreatedByMe = false) : IQuery<PagedResult<QuoteSummaryDto>>;
