using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Pricing.Commands.DiscardPricingDraft;

// ADR-048: Kullanılmayan bir TASLAK versiyonu iptal eder (soft-delete). Yalnızca taslak iptal edilebilir —
// aktif/arşiv versiyon asla silinemez/değişemez (geçmiş primler korunur). İptal sonrası admin yeni bir taslak
// oluşturabilir (aynı anda tek taslak kuralı serbest kalır).
public sealed record DiscardPricingDraftCommand(Guid VersionId) : ICommand;
