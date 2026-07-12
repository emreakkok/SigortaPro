using Microsoft.Extensions.Configuration;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Infrastructure.Services.Storage;

// IFileStorageService yerel disk implementasyonu (ADR-023). Kök dizin `FileStorage:RootPath` ile
// yapılandırılır; göreli anahtarlar kök altında saklanır. İleride blob depolamaya geçiş yalnızca
// yeni bir implementasyon eklemektir (arayüz değişmez). Dizin dışına çıkış (path traversal) engellenir.
public sealed class LocalFileStorageService : IFileStorageService
{
    private const string DefaultRoot = "App_Data";
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        var configured = configuration["FileStorage:RootPath"];
        var basePath = string.IsNullOrWhiteSpace(configured) ? DefaultRoot : configured;

        _rootPath = Path.GetFullPath(
            Path.IsPathRooted(basePath) ? basePath : Path.Combine(AppContext.BaseDirectory, basePath));
    }

    public async Task<string> SaveAsync(string key, byte[] content, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        return key;
    }

    public async Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullPath(key);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    private string ResolveFullPath(string key)
    {
        var normalized = key.Replace('\\', '/').TrimStart('/');
        var combined = Path.GetFullPath(Path.Combine(_rootPath, normalized));

        // Kök dizin dışına çıkışı (../) engelle.
        if (!combined.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Geçersiz dosya anahtarı.");
        }

        return combined;
    }
}
