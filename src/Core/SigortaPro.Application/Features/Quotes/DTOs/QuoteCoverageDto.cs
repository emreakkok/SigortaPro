namespace SigortaPro.Application.Features.Quotes.DTOs;

// Teklifin bir teminat kalemi; limit, seçilen teminat paketine göre ölçeklenmiştir.
public sealed record QuoteCoverageDto(
    string Name,
    string? Description,
    decimal Limit);
