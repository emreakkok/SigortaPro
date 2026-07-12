namespace SigortaPro.Application.Common.Pricing;

// Fiyatlama motorunun ürettiği risk seviyesi (ADR-008). Domain'de saklanmaz; teklif ekranında
// gösterim ve fiyat dökümü için hesaplanan bir çıktıdır.
public enum RiskScore
{
    Low,
    Medium,
    High
}
