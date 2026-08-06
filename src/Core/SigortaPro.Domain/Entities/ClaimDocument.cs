using SigortaPro.Domain.Common;

namespace SigortaPro.Domain.Entities;

// Hasar bildirimine eklenen belge/görsel (foto veya PDF). Claim aggregate'inin bir parçasıdır.
// Dosya BAYTLARI IFileStorageService'te (StorageKey ile) saklanır; bu entity yalnızca metadata tutar
// (ad, tür, boyut, depolama anahtarı) depolama soyutlaması yeniden kullanılır.
public class ClaimDocument : BaseEntity
{
    protected ClaimDocument()
    {
    }

    public ClaimDocument(
        Guid id, Guid claimId, string fileName, string contentType, long fileSizeBytes, string storageKey)
    {
        Id = id;
        ClaimId = claimId;
        FileName = fileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        StorageKey = storageKey;
    }

    public Guid ClaimId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
}
