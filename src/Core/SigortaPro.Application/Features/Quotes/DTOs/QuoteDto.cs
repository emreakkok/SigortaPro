using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.DTOs;

// Teklif detayı: durum, prim ve prim dökümü (risk faktörleri + teminat paketi) ile birlikte.
// Prim dökümü ve risk skoru, saklanan seçim + veriden deterministik olarak yeniden hesaplanır (ADR-021).
public sealed record QuoteDto(
    Guid Id,
    Guid CustomerId,
    InsuranceBranch Branch,
    string ProductName,
    QuoteStatus Status,
    CoveragePackage CoveragePackage,
    RiskScore RiskScore,
    decimal BasePremium,
    decimal TotalPremium,
    DateTime? ValidUntil,
    DateTime CreatedAt,
    QuoteRiskObjectDto RiskObject,
    IReadOnlyList<QuoteCoverageDto> Coverages,
    IReadOnlyList<PricingBreakdownItem> PremiumBreakdown,
    // ADR-041 (additive): Sağlıkta "başkası adına" teklifte sigortalı özeti; kendisi için null.
    QuoteInsuredPersonDto? InsuredPerson = null,
    // Müşteri (Sigorta Ettiren) kimliği (additive) — admin detayında ad + telefon özeti. Navigasyon yoksa boş.
    string CustomerFullName = "",
    string? CustomerPhone = null,
    // Teklif kaynağı (türetilmiş): müşteri kendi mi oluşturdu (SelfService) yoksa acente mi (AgentAssisted).
    QuoteSource Source = QuoteSource.SelfService,
    // Acente destekli teklifte üreten personelin görünen adı (yalnızca personel yüzeyine — admin detayı).
    // Müşteri yüzeyine taşınmaz (KVKK/gizlilik — müşteri yalnızca "Acente" görür). Self-service ise null.
    string? CreatedByStaffName = null);
