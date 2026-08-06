using FluentAssertions;
using SigortaPro.Infrastructure.Services.VehicleCatalog;

namespace SigortaPro.Infrastructure.Tests.Services.VehicleCatalog;

public sealed class VehicleCatalogProviderTests
{
    private readonly VehicleCatalogProvider _provider = new();

    [Fact]
    public void GetCatalog_Should_LoadEmbeddedBrandsAndModels()
    {
        var catalog = _provider.GetCatalog();

        catalog.Should().NotBeNull();
        catalog.Brands.Should().NotBeEmpty();
        catalog.Brands.Should().OnlyContain(brand => !string.IsNullOrWhiteSpace(brand.Name));
        catalog.Brands.Should().OnlyContain(brand => brand.Models.Count > 0);
    }

    [Fact]
    public void GetCatalog_Should_ContainKnownBrandWithModels()
    {
        var catalog = _provider.GetCatalog();

        var toyota = catalog.Brands.SingleOrDefault(brand => brand.Name == "Toyota");
        toyota.Should().NotBeNull();
        toyota!.Models.Should().Contain("Corolla");
    }

    [Fact]
    public void GetCatalog_Should_ReturnSameCachedInstance_When_CalledTwice()
    {
        // In-Memory cache (Lazy): ikinci çağrı kaynağı yeniden okumaz, aynı referansı döner.
        var first = _provider.GetCatalog();
        var second = _provider.GetCatalog();

        second.Should().BeSameAs(first);
    }
}
