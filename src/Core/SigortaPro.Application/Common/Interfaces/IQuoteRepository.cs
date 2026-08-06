using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Interfaces;

// Teklif modülüne özgü sorgular. Detay/liste sorguları risk
// objesi ve ürün/teminat navigasyonlarını EF Core Include ile yükler; Application EF'e bağımlı olamaz.
public interface IQuoteRepository : IReadRepository<Quote>, IWriteRepository<Quote>
{
    // Salt okunur detay: müşteri, risk objesi (araç/konut), ürün ve teminatlarıyla birlikte.
    Task<Quote?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // İzlemeli (tracked) teklif — durum geçişi komutları için; sahiplik kontrolü için müşteriyi de yükler.
    Task<Quote?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Sayfalanmış liste: müşteri Id verilirse yalnızca o müşterinin teklifleri, verilmezse tümü (admin).
    // Opsiyonel durum/branş filtreleriyle; ürün adı için InsuranceProduct, müşteri kimliği için Customer yüklenir.
    // search: müşteri adı/soyadı/tam adı veya telefon (format bağımsız — PhoneNumberSearch ile normalize).
    // createdByStaffUserId: dolu ise yalnızca o personelin müşteri adına oluşturduğu teklifler ("acente destekli")
    // döner — personelin "benim oluşturduklarım" görünümü için (null = filtre uygulanmaz).
    Task<PagedResult<Quote>> SearchAsync(
        Guid? customerId,
        QuoteStatus? status,
        InsuranceBranch? branch,
        string? search,
        PaginationParams paging,
        Guid? createdByStaffUserId = null,
        CancellationToken cancellationToken = default);

    // İzlemeli: geçerlilik süresi dolmuş ancak henüz sonlanmamış (Draft/Priced/Approved) teklifler —
    // arkaplan servisi bunları Expired'a çeker.
    Task<IReadOnlyList<Quote>> GetExpirableAsync(DateTime asOf, CancellationToken cancellationToken = default);
}
