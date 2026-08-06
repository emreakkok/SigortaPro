using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes;

/// <summary>
/// Fiyatlama girdisinin **tek kurulum noktası**. Hem teklif OLUŞTURMA hem de paket
/// KARŞILAŞTIRMA (önizleme) akışı bu sınıfı kullanır.
/// <para>
/// Neden var: Önizleme ve oluşturma daha önce girdiyi <b>ayrı ayrı</b> kuruyordu. Parite sözleşmeyle değil
/// tesadüfle sağlanıyordu ve sigara beyanı ile adresten türetilen deprem bölgesi
/// eklenince bozuldu: kullanıcıya karşılaştırmada bir fiyat gösterilip oluşan teklifte başka fiyat
/// uygulanıyordu. Girdi kurulumu tek noktaya alınarak parite <b>yapısal olarak</b> garanti edilir —
/// ileride yeni bir risk faktörü eklendiğinde iki akış tekrar ayrışamaz.
/// </para>
/// </summary>
public sealed class QuotePricingInputBuilder : IQuotePricingInputBuilder
{
    private readonly IEarthquakeZoneProvider _earthquakeZoneProvider;
    private readonly IPolicyRepository _policyRepository;
    private readonly IClaimRepository _claimRepository;

    public QuotePricingInputBuilder(
        IEarthquakeZoneProvider earthquakeZoneProvider,
        IPolicyRepository policyRepository,
        IClaimRepository claimRepository)
    {
        _earthquakeZoneProvider = earthquakeZoneProvider;
        _policyRepository = policyRepository;
        _claimRepository = claimRepository;
    }

    public async Task<PricingSnapshot> BuildAsync(
        InsuranceBranch branch,
        Customer customer,
        Vehicle? vehicle,
        Property? property,
        DateTime referenceDate,
        DateTime? insuredBirthDate,
        bool? isSmoker,
        CancellationToken cancellationToken = default)
    {
        // Deprem bölgesi kullanıcı beyanı değil, konutun ilinden türetilir. İl çözülemezse bölge
        // atanmaz → motor "bilinmeyen bölge" davranışını açık açıklamasıyla uygular.
        var earthquakeZone = property is null
            ? null
            : _earthquakeZoneProvider.ResolveZone(property.Address.City);

        // Bonus-Malus basamağı YALNIZCA araç branşlarında (Kasko/Trafik) hesaplanır ve her branş
        // kendi geçmişini taşır. Sağlık/Konut/DASK için hesaplanmaz — snapshot'ları bu alanı zaten taşımaz.
        var bonusMalusStep = branch is InsuranceBranch.Kasko or InsuranceBranch.Trafik
            ? await ResolveBonusMalusStepAsync(customer.Id, branch, referenceDate, cancellationToken)
            : BonusMalusScale.NeutralStep;

        return QuotePricingFactory.BuildSnapshot(
            branch, customer, vehicle, property, referenceDate, insuredBirthDate, isSmoker,
            earthquakeZone, bonusMalusStep);
    }

    /// <summary>
    /// Basamağı mevcut veriden DURUMSUZ türetir: aynı branştaki hasarsız tamamlanmış dönemler +1,
    /// onaylanmış/ödenmiş hasarlar −2. Geçmişi olmayan müşteri nötr (0) başlar — dış geçmiş varsayılmaz.
    /// </summary>
    private async Task<int> ResolveBonusMalusStepAsync(
        Guid customerId, InsuranceBranch branch, DateTime asOf, CancellationToken cancellationToken)
    {
        var claimFreePeriods = await _policyRepository.CountClaimFreeCompletedPeriodsAsync(
            customerId, branch, asOf, cancellationToken);
        var reportableClaims = await _claimRepository.CountReportableClaimsByCustomerAsync(
            customerId, branch, cancellationToken);

        return BonusMalusScale.ComputeStep(claimFreePeriods, reportableClaims);
    }
}
