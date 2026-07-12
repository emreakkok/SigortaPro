using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Documents;

// Poliçe sertifikası PDF'ini üretmek için gereken tüm veriyi taşıyan, sunum katmanından bağımsız model.
// TCKN maskeli taşınır (CLAUDE.md §4.5); render servisi yalnızca bu modeli tüketir.
public sealed record PolicyDocumentModel(
    string AgencyName,
    string AgencyAddress,
    string AgencyContact,
    string PolicyNumber,
    PolicyStatus PolicyStatus,
    DateTime StartDate,
    DateTime EndDate,
    DateTime IssuedAt,
    string CustomerFullName,
    string CustomerMaskedTckn,
    string CustomerPhone,
    string CustomerAddress,
    InsuranceBranch Branch,
    string ProductName,
    CoveragePackage CoveragePackage,
    string RiskObjectKind,
    string RiskObjectDisplay,
    RiskScore RiskScore,
    decimal BasePremium,
    decimal TotalPremium,
    IReadOnlyList<PolicyCoverageLine> Coverages,
    IReadOnlyList<PricingBreakdownItem> PremiumBreakdown);

// Poliçe teminat tablosu satırı (paket ölçeğiyle uygulanmış limitle).
public sealed record PolicyCoverageLine(string Name, string Description, decimal Limit);
