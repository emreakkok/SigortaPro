using SigortaPro.Application.Common.Security;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes;

// Entity → DTO manuel eşlemeleri (AutoMapper kullanılmaz).
internal static class QuoteMappings
{
    // createdByStaffName: acente destekli teklifte üreten personelin adı (detay handler'ında IIdentityService
    // ile çözülüp geçilir). Self-service tekliflerde ve liste sorgularında null bırakılır.
    public static QuoteDto ToDto(
        Quote quote,
        InsuranceProduct product,
        Vehicle? vehicle,
        Property? property,
        QuotePricingOutcome pricing,
        string? createdByStaffName = null) => new(
        quote.Id,
        quote.CustomerId,
        quote.Branch,
        product.Name,
        quote.Status,
        quote.CoveragePackage,
        pricing.RiskScore,
        pricing.BasePremium,
        quote.TotalPremium,
        quote.ValidUntil,
        quote.CreatedAt,
        BuildRiskObject(vehicle, property, quote.InsuredPerson),
        pricing.Coverages,
        pricing.Breakdown,
        ToInsuredPersonDto(quote.InsuredPerson),
        CustomerFullName: FullName(quote.Customer),
        CustomerPhone: quote.Customer?.PhoneNumber,
        Source: ResolveSource(quote),
        CreatedByStaffName: createdByStaffName);

    // sigortalı özeti — ham TCKN sızmaz, maskeli döner.
    public static QuoteInsuredPersonDto? ToInsuredPersonDto(InsuredPerson? insuredPerson) =>
        insuredPerson is null
            ? null
            : new QuoteInsuredPersonDto(
                insuredPerson.FullName,
                SensitiveDataMasker.MaskTckn(insuredPerson.Tckn),
                insuredPerson.BirthDate,
                insuredPerson.Relationship);

    public static QuoteSummaryDto ToSummaryDto(Quote quote) => new(
        quote.Id,
        quote.Branch,
        quote.InsuranceProduct?.Name ?? string.Empty,
        quote.Status,
        quote.CoveragePackage,
        quote.TotalPremium,
        quote.ValidUntil,
        quote.CreatedAt,
        // Admin listesi müşteriyi Include eder; durum geçişi komutları etmez → null-safe (o yanıtlar müşteriye
        // aittir ve müşteri bilgisi gösterilmez). Telefon kanonik saklanır (+90…), gösterim frontend'de biçimlenir.
        CustomerId: quote.CustomerId,
        CustomerFullName: FullName(quote.Customer),
        CustomerPhone: quote.Customer?.PhoneNumber,
        Source: ResolveSource(quote));

    // Teklif kaynağı türetimi: üreten personel varsa acente destekli, yoksa self-service.
    public static QuoteSource ResolveSource(Quote quote) =>
        quote.CreatedByStaffUserId is null ? QuoteSource.SelfService : QuoteSource.AgentAssisted;

    // Müşteri (Sigorta Ettiren) tam adı; navigasyon yüklü değilse boş (null-safe).
    public static string FullName(Customer? customer) =>
        customer is null ? string.Empty : $"{customer.FirstName} {customer.LastName}";

    public static QuoteRiskObjectDto BuildRiskObject(
        Vehicle? vehicle,
        Property? property,
        InsuredPerson? insuredPerson = null)
    {
        if (vehicle is not null)
        {
            return new QuoteRiskObjectDto(
                "Araç",
                $"{vehicle.PlateNumber} · {vehicle.Brand} {vehicle.Model} ({vehicle.ManufactureYear})");
        }

        if (property is not null)
        {
            return new QuoteRiskObjectDto(
                "Konut",
                $"{property.Address.City}/{property.Address.District} · {property.SquareMeters} m²");
        }

        // "başkası adına" teklifte risk objesi sigortalının kendisidir.
        return insuredPerson is null
            ? new QuoteRiskObjectDto("Kişi", "Sağlık sigortası (sigortalı: poliçe sahibi)")
            : new QuoteRiskObjectDto("Kişi", $"Sigortalı: {insuredPerson.FullName} ({insuredPerson.Relationship})");
    }
}
