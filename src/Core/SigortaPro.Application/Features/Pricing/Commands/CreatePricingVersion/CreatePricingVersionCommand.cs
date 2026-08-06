using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;

namespace SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;

// Yeni bir TASLAK tarife versiyonu oluşturur. İsim ZORUNLUDUR. Taslak, mevcut AKTİF versiyonun
// (yoksa yerleşik baseline'ın) TÜM değerleriyle seed edilir → admin güncel tarifeden başlayarak düzenler.
// Taslak oluşturmak canlı fiyatları ETKİLEMEZ. Aynı anda yalnızca bir taslak bulunur: açık taslak varsa
// yeni oluşturulmaz, mevcut taslak döner.
public sealed record CreatePricingVersionCommand(string Name) : ICommand<PricingVersionDto>;
