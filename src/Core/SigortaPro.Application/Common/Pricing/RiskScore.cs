namespace SigortaPro.Application.Common.Pricing;

// Fiyatlama motorunun ürettiği risk seviyesi. Domain'de saklanmaz; teklif ekranında
// gösterim ve fiyat dökümü için hesaplanan bir çıktıdır.
public enum RiskScore
{
    Low,
    Medium,
    High
}
