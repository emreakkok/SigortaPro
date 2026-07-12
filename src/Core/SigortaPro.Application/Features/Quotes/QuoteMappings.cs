using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes;

// Entity → DTO manuel eşlemeleri (AutoMapper kullanılmaz — CODING_STANDARDS.md §4.2).
internal static class QuoteMappings
{
    public static QuoteDto ToDto(
        Quote quote,
        InsuranceProduct product,
        Vehicle? vehicle,
        Property? property,
        QuotePricingOutcome pricing) => new(
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
        BuildRiskObject(vehicle, property),
        pricing.Coverages,
        pricing.Breakdown);

    public static QuoteSummaryDto ToSummaryDto(Quote quote) => new(
        quote.Id,
        quote.Branch,
        quote.InsuranceProduct?.Name ?? string.Empty,
        quote.Status,
        quote.CoveragePackage,
        quote.TotalPremium,
        quote.ValidUntil,
        quote.CreatedAt);

    public static QuoteRiskObjectDto BuildRiskObject(Vehicle? vehicle, Property? property)
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

        return new QuoteRiskObjectDto("Kişi", "Sağlık sigortası (sigortalı kişi)");
    }
}
