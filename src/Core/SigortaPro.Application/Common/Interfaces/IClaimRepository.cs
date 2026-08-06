using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Interfaces;

// Hasar modülüne özgü sorgular. Detay/liste sorguları poliçe
// navigasyonunu (poliçe numarası için) EF Core Include ile yükler; Application EF'e bağımlı olamaz.
public interface IClaimRepository : IReadRepository<Claim>, IWriteRepository<Claim>
{
    // Salt okunur detay: poliçesiyle birlikte (poliçe numarası gösterimi için).
    Task<Claim?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // İzlemeli (tracked) hasar — durum geçişi komutları için; özet yanıtta poliçe numarası için poliçe yüklenir.
    Task<Claim?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Sayfalanmış liste: müşteri Id verilirse yalnızca o müşterinin hasarları, verilmezse tümü (personel).
    // Opsiyonel durum/poliçe filtreleriyle; poliçe numarası için Policy yüklenir.
    Task<PagedResult<Claim>> SearchAsync(
        Guid? customerId,
        ClaimStatus? status,
        Guid? policyId,
        PaginationParams paging,
        CancellationToken cancellationToken = default);

    // Müşterinin fiyatlamaya etki eden hasar geçmişi: onaylanmış/ödenmiş hasar sayısı.
    // Sayım **branşa göre kapsanır** — bir Kasko hasarı Sağlık yenilemesini pahalılaştıramaz.
    // Hasarın branşı, hasarın bağlı olduğu poliçenin teklifinden (Policy → Quote.Branch) belirlenir.
    Task<int> CountReportableClaimsByCustomerAsync(
        Guid customerId, InsuranceBranch branch, CancellationToken cancellationToken = default);
}
