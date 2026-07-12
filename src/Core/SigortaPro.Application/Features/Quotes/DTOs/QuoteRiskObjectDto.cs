namespace SigortaPro.Application.Features.Quotes.DTOs;

// Teklifin risk objesinin özet gösterimi (araç/konut/kişi).
public sealed record QuoteRiskObjectDto(
    string Kind,
    string Display);
