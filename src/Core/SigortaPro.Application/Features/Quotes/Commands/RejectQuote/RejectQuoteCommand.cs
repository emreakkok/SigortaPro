using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;

namespace SigortaPro.Application.Features.Quotes.Commands.RejectQuote;

// Müşteri teklifini reddeder (satın alınmış/süresi dolmuş teklif hariç → Rejected).
public sealed record RejectQuoteCommand(Guid QuoteId) : ICommand<QuoteSummaryDto>;
