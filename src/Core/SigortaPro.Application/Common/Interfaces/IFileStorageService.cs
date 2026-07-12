namespace SigortaPro.Application.Common.Interfaces;

// Dosya saklama soyutlaması. Yerel disk implementasyonu Infrastructure'da; ileride blob depolamaya
// (Azure Blob / S3) geçişe hazır — çağıranlar yalnızca göreli anahtar (key) ile çalışır (ADR-023).
public interface IFileStorageService
{
    // İçeriği verilen anahtara yazar ve kalıcı anahtarı döner (blob implementasyonu kanonik anahtar döndürebilir).
    Task<string> SaveAsync(string key, byte[] content, CancellationToken cancellationToken = default);

    // Anahtardaki içeriği okur; yoksa null döner.
    Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken = default);
}
