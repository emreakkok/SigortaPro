using FluentAssertions;
using SigortaPro.Infrastructure.Services.CityCatalog;

namespace SigortaPro.Infrastructure.Tests.Services.CityCatalog;

public sealed class CityCatalogProviderTests
{
    private readonly CityCatalogProvider _provider = new();

    [Fact]
    public void GetCatalog_Should_LoadAll81Provinces()
    {
        var catalog = _provider.GetCatalog();

        catalog.Should().NotBeNull();
        catalog.Cities.Should().HaveCount(81);
        catalog.Cities.Should().OnlyContain(city => !string.IsNullOrWhiteSpace(city.Name));
        catalog.Cities.Select(city => city.Name).Should().OnlyHaveUniqueItems();
    }

    private static readonly string[] KnownProvinces = ["İstanbul", "Ankara", "İzmir", "Düzce"];

    [Fact]
    public void GetCatalog_Should_ContainKnownProvinces()
    {
        var names = _provider.GetCatalog().Cities.Select(city => city.Name).ToList();

        names.Should().Contain(KnownProvinces);
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
