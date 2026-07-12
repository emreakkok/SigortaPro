using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteComparison;

// Aynı risk objesi için teminat seviyeli 2-3 alternatif paket üretir (önizleme; teklif oluşturmaz).
public sealed record GetQuoteComparisonQuery(
    InsuranceBranch Branch,
    Guid? VehicleId,
    Guid? PropertyId) : IQuery<QuoteComparisonDto>;
