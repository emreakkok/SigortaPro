using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Payments.DTOs;

namespace SigortaPro.Application.Features.Payments.Queries.GetInstallmentOptions;

// Onaylanmış teklifin primi için taksit seçeneklerini getirir (ödeme sayfası önizlemesi).
public sealed record GetInstallmentOptionsQuery(Guid QuoteId) : IQuery<IReadOnlyList<InstallmentOptionDto>>;
