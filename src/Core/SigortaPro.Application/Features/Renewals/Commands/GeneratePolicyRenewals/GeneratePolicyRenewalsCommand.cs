using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Renewals.Commands.GeneratePolicyRenewals;

// Sistem tetikli (arkaplan servisi): bitişine ≤30 gün kalan, henüz yenileme teklifi olmayan aktif poliçeler için
// güncel fiyatlama + hasar geçmişi çarpanıyla yenileme teklifi üretir ve müşteriyi bilgilendirir. Üretilen adet döner.
public sealed record GeneratePolicyRenewalsCommand : ICommand<int>;
