using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.DTOs;

// Teklif listesi görünümü için özet DTO (prim dökümü içermez).
public sealed record QuoteSummaryDto(
    Guid Id,
    InsuranceBranch Branch,
    string ProductName,
    QuoteStatus Status,
    CoveragePackage CoveragePackage,
    decimal TotalPremium,
    DateTime? ValidUntil,
    DateTime CreatedAt,
    // Müşteri kimliği (additive) — admin listesinde aynı isimli müşterileri ayırt etmek için ad + telefon.
    // CustomerId stabil kimliktir; telefon görsel ayırt edicidir (primary identity DEĞİL). Müşteri kendi
    // listesinde kendi bilgisini görür (sızıntı yok). Navigasyon yüklü değilse boş döner (null-safe).
    Guid CustomerId = default,
    string CustomerFullName = "",
    string? CustomerPhone = null,
    // Teklif kaynağı (türetilmiş) — listede "Online / Acente" rozeti için. Müşteri kendi listesinde bunu
    // "Kendiniz / Acente" olarak görür (personel kimliği sızmaz).
    QuoteSource Source = QuoteSource.SelfService);
