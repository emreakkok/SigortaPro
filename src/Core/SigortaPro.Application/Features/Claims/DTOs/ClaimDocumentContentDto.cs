namespace SigortaPro.Application.Features.Claims.DTOs;

// Hasar belgesinin indirilebilir içeriği (baytlar + tür + ad). Controller bunu FileResult'a çevirir;
// JSON gövdesinde serileştirilmez.
public sealed record ClaimDocumentContentDto(byte[] Content, string ContentType, string FileName);
