using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;

namespace SigortaPro.Application.Features.Pricing.Commands.ActivatePricingVersion;

// TASLAK versiyonu YÜRÜRLÜĞE ALIR (Aktifleştir). O ana kadar oluşturulmuş teklif/poliçe primleri
// DEĞİŞMEZ (sabitledikleri versiyonla hesaplanmaya devam eder); yalnızca bu andan SONRA oluşturulacak
// teklifler yeni tarifeyi kullanır. Önceki aktif versiyon otomatik ARŞİVLENİR (aynı anda tek aktif versiyon).
public sealed record ActivatePricingVersionCommand(Guid VersionId) : ICommand<PricingVersionDto>;
