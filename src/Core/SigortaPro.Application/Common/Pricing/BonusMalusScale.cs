namespace SigortaPro.Application.Common.Pricing;

/// <summary>
/// ADR-059: Hasar geçmişinin TEK ölçeği (bonus-malus basamağı). Önceden iki bağımsız çarpan vardı
/// (<c>ClaimHistoryFactor</c> malus, <c>NoClaimTier</c> bonus); aralarında hiçbir değişmez olmadığından
/// çelişkili sonuç üretebiliyorlardı (ör. 3 hasarlı + yüksek basamaklı müşteri neredeyse nötr fiyat).
/// Artık hasar geçmişi tek bir basamakla temsil edilir.
/// <para>
/// Hesap <b>durumsuzdur</b>: her fiyatlamada mevcut veriden sıfırdan türetilir → zincir/durum makinesi yok,
/// idempotent ve deterministik. Basamak teklif anında <c>PricingSnapshot</c>'a dondurulur.
/// </para>
/// </summary>
public static class BonusMalusScale
{
    /// <summary>En düşük basamak (en ağır malus).</summary>
    public const int MinStep = -3;

    /// <summary>En yüksek basamak (en yüksek bonus).</summary>
    public const int MaxStep = 6;

    /// <summary>Geçmişi bilinmeyen / yeni müşteri basamağı — ne indirim ne ceza (×1.00).</summary>
    public const int NeutralStep = 0;

    /// <summary>Raporlanabilir her hasarın düşürdüğü basamak sayısı.</summary>
    private const int StepsLostPerClaim = 2;

    /// <summary>
    /// Basamağı hesaplar: hasarsız tamamlanan her dönem +1, raporlanabilir her hasar −2; sonuç
    /// [<see cref="MinStep"/>, <see cref="MaxStep"/>] aralığına sıkıştırılır.
    /// <para>
    /// SigortaPro dışındaki geçmiş <b>bilinmediğinden varsayılmaz</b>: geçmişi olmayan müşteri
    /// <see cref="NeutralStep"/>'ten başlar (indirim de ceza da almaz).
    /// </para>
    /// Malus kalıcı değildir: sonraki hasarsız dönemler basamağı yeniden yükseltir (sönümlenme).
    /// </summary>
    /// <param name="claimFreeCompletedPeriods">Aynı branşta hasarsız tamamlanmış poliçe dönemi sayısı.</param>
    /// <param name="reportableClaims">Aynı branştaki onaylanmış/ödenmiş hasar sayısı.</param>
    public static int ComputeStep(int claimFreeCompletedPeriods, int reportableClaims)
    {
        var raw = claimFreeCompletedPeriods - (reportableClaims * StepsLostPerClaim);
        return Math.Clamp(raw, MinStep, MaxStep);
    }
}
