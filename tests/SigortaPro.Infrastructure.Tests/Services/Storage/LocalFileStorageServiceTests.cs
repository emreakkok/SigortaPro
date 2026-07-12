using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SigortaPro.Infrastructure.Services.Storage;

namespace SigortaPro.Infrastructure.Tests.Services.Storage;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _root;
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "sigortapro-tests", Guid.NewGuid().ToString("N"));

        var configuration = Substitute.For<IConfiguration>();
        configuration["FileStorage:RootPath"].Returns(_root);

        _service = new LocalFileStorageService(configuration);
    }

    [Fact]
    public async Task SaveAsync_Then_ReadAsync_Should_Roundtrip()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };

        var key = await _service.SaveAsync("policy-documents/test.pdf", content, CancellationToken.None);
        var read = await _service.ReadAsync(key, CancellationToken.None);

        key.Should().Be("policy-documents/test.pdf");
        read.Should().Equal(content);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnNull_When_FileMissing()
    {
        var read = await _service.ReadAsync("policy-documents/none.pdf", CancellationToken.None);

        read.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_Should_Throw_When_KeyEscapesRoot()
    {
        var act = () => _service.SaveAsync("../../escape.pdf", new byte[] { 1 }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
