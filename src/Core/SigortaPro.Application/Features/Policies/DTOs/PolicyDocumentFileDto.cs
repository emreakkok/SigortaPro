namespace SigortaPro.Application.Features.Policies.DTOs;

// İndirilebilir poliçe belgesi: içerik baytları + dosya adı + içerik tipi.
public sealed record PolicyDocumentFileDto(
    byte[] Content,
    string FileName,
    string ContentType);
