namespace SigortaPro.Application.Features.Quotes.DTOs;

// "Başkası adına" sağlık teklifinde sigortalının özet görünümü (ADR-041).
// Ham TCKN taşınmaz; maskeli döner (CODING_STANDARDS.md §4.2).
public sealed record QuoteInsuredPersonDto(
    string FullName,
    string MaskedTckn,
    DateTime BirthDate,
    string Relationship);
