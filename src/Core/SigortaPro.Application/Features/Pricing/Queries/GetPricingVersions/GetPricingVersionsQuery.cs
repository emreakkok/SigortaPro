using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;

namespace SigortaPro.Application.Features.Pricing.Queries.GetPricingVersions;

// ADR-048: Fiyatlandırma yönetimi ekranının tek veri kaynağı — yürürlükteki tarife + tüm geçmiş
// (en yeni önce). Versiyonlar değişmez olduğundan "geçmiş" ayrı bir audit sorgusu gerektirmez.
public sealed record GetPricingVersionsQuery : IQuery<IReadOnlyList<PricingVersionDto>>;
