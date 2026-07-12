using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;

namespace SigortaPro.Application.Features.Quotes.Commands.ApproveQuote;

// Müşteri, fiyatlandırılmış teklifini onaylar (Priced → Approved).
public sealed record ApproveQuoteCommand(Guid QuoteId) : ICommand<QuoteSummaryDto>;
