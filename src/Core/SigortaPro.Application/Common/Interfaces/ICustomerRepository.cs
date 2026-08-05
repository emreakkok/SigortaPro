using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// Müşteri modülüne özgü sorgular için özel repository (ARCHITECTURE_RULES.md §4.2, ADR-005).
// Application katmanı EF Core'a bağımlı olamadığından (async materialization, Include, projection
// EF gerektirir), modüle özgü okuma/arama mantığı bu arayüzün arkasında Persistence'ta implement edilir.
public interface ICustomerRepository : IReadRepository<Customer>, IWriteRepository<Customer>
{
    // Salt okunur profil görünümü: risk objeleriyle (araç/konut) birlikte, AppUserId üzerinden getirir.
    Task<Customer?> GetProfileByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default);

    // Salt okunur profil görünümü: risk objeleriyle birlikte, Customer Id üzerinden getirir (admin görünümü).
    Task<Customer?> GetProfileByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // İzlemeli (tracked) müşteri kaydı — güncelleme komutlarında AppUserId üzerinden çözümlenir.
    Task<Customer?> GetTrackedByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default);

    // İzlemeli (tracked) müşteri kaydı — Customer Id üzerinden. Acente personelinin "müşteri adına" işlem
    // yaptığı (teklif/araç/konut oluşturma) akışlarda hedef müşteriyi çözmek için kullanılır.
    Task<Customer?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // TCKN benzersizliği ön-kontrolü (kayıt akışı) — DB unique ihlalini (500) önlemek için insert öncesi kullanılır.
    Task<bool> ExistsByTcknAsync(string tckn, CancellationToken cancellationToken = default);

    // Admin müşteri listesi: ad/soyad/TCKN/e-posta/telefon (normalize) araması ve il filtresiyle
    // sayfalanmış sonuç (ADR-040 — searchTerm sözleşmesi değişmeden genişletildi).
    Task<PagedResult<Customer>> SearchAsync(
        string? searchTerm,
        string? city,
        PaginationParams paging,
        CancellationToken cancellationToken = default);
}
