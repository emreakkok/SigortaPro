using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;

namespace SigortaPro.Application.Features.Claims.Commands.CreateClaim;

// Müşteri, aktif bir poliçesi için hasar bildirir (olay tarihi/açıklaması, tahmini tutar) ve destekleyici
// belge/görsel (foto/PDF) ekleyebilir. Belgeler gerçek olarak IFileStorageService'te saklanır
// ve Admin/Personel değerlendirmesinde görüntülenir. İçerik JSON'da base64 taşınır (System.Text.Json byte[]).
public sealed record CreateClaimCommand(
    Guid PolicyId,
    DateTime IncidentDate,
    string Description,
    decimal EstimatedAmount,
    IReadOnlyList<CreateClaimDocument>? Documents = null) : ICommand<ClaimDto>;

// Yüklenen tek belge: ad, MIME türü ve içerik (JSON'da base64). Baytlar depolamaya yazılır, metadata DB'ye.
public sealed record CreateClaimDocument(string FileName, string ContentType, byte[] Content);
