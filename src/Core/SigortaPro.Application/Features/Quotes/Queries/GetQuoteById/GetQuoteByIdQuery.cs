using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteById;

// Teklif detayı (prim dökümü ile). Sahibi müşteri veya acente personeli erişebilir.
public sealed record GetQuoteByIdQuery(Guid QuoteId) : IQuery<QuoteDto>;
