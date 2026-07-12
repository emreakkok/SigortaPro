using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Quotes.Commands.ExpireOutdatedQuotes;

// Sistem tetikli (arkaplan servisi): geçerlilik süresi dolmuş teklifleri Expired'a çeker. Etkilenen adet döner.
public sealed record ExpireOutdatedQuotesCommand : ICommand<int>;
