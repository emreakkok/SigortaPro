namespace SigortaPro.Application.Features.Dashboard.ReadModels;

// Prim üretimi zaman serisi kovası. BucketStart, kovanın başlangıç anıdır (saat/gün/ay).
// Seri ÜRETİM tarihine (Policy.CreatedAt) göredir — poliçe listesindeki StartDate (teminat başlangıcı)
// ile bilinçli olarak farklıdır; amaç satış/üretim performansını ölçmektir. Salt okunur sorgu sonucu (ADR-026).
public sealed record PremiumSeriesAggregate(
    DateTime BucketStart,
    int PolicyCount,
    decimal PremiumTotal);
