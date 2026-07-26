namespace SigortaPro.Application.Features.Claims.DTOs;

// Hasar belgesi metadata görünümü (baytlar ayrı uçla indirilir: GET /claims/{id}/documents/{documentId}).
// IsImage: görsel ise (image/*) frontend önizleme (thumbnail) gösterir; değilse (PDF vb.) aç/indir sunar.
public sealed record ClaimDocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    bool IsImage,
    DateTime CreatedAt);
