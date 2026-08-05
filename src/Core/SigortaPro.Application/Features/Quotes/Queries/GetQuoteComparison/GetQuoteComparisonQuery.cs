using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteComparison;

// Aynı risk objesi için teminat seviyeli 2-3 alternatif paket üretir (önizleme; teklif oluşturmaz).
// InsuredBirthDate (ADR-041): Sağlıkta "başkası adına" önizlemede sigortalının doğum tarihi; null = poliçe sahibi.
// IsSmoker (ADR-054/056): Sağlıkta sigara BEYANI — teklif oluşturmadaki kuralın aynısı (zorunlu).
//
// ADR-056: Önizleme, fiyatlama girdisini teklif oluşturmayla AYNI builder'dan kurar; bu yüzden burada
// gösterilen prim, aynı seçimle oluşturulacak teklifin primiyle **yapısal olarak** eşittir.
// CustomerId (acente destekli önizleme, additive): dolu ise karşılaştırma bu müşteri ADINA hesaplanır ve
// çağıran yalnızca acente personeli olabilir (controller nested route'ta set eder). null = self-service.
public sealed record GetQuoteComparisonQuery(
    InsuranceBranch Branch,
    Guid? VehicleId,
    Guid? PropertyId,
    DateTime? InsuredBirthDate = null,
    bool? IsSmoker = null,
    Guid? CustomerId = null) : IQuery<QuoteComparisonDto>;
