using FluentAssertions;
using SigortaPro.Infrastructure.Services.EarthquakeZone;

namespace SigortaPro.Infrastructure.Tests.Services;

// ADR-055: Deprem bölgesi kullanıcı beyanından değil, konutun İLİNDEN türetilir.
public class EarthquakeZoneProviderTests
{
    private readonly EarthquakeZoneProvider _provider = new();

    [Theory]
    [InlineData("İstanbul", 1)]
    [InlineData("Kocaeli", 1)]
    [InlineData("Samsun", 2)]
    [InlineData("Ankara", 3)]
    [InlineData("Konya", 4)]
    public void ResolveZone_Should_MapCityToZone(string city, int expected)
    {
        _provider.ResolveZone(city).Should().Be(expected);
    }

    [Fact]
    public void ResolveZone_Should_BeCaseAndWhitespaceInsensitive()
    {
        // Adres serbest metin olabildiğinden eşleşme büyük/küçük harf ve boşluk duyarsızdır.
        _provider.ResolveZone("  istanbul  ").Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bilinmeyenşehir")]
    public void ResolveZone_Should_ReturnNull_When_CityIsUnknown(string? city)
    {
        // Sessizce (ve müşteri lehine) bir bölge ATANMAZ; çağıran taraf "bilinmeyen" davranışını uygular.
        _provider.ResolveZone(city).Should().BeNull();
    }

    [Fact]
    public void ResolveZone_Should_CoverAllCatalogCities()
    {
        // Katalogdaki 81 ilin tamamı eşlenmelidir; aksi hâlde bazı konutlar sessizce "bilinmeyen" bölgeye düşer.
        var cityCatalog = new SigortaPro.Infrastructure.Services.CityCatalog.CityCatalogProvider().GetCatalog();

        var unmapped = cityCatalog.Cities
            .Where(city => _provider.ResolveZone(city.Name) is null)
            .Select(city => city.Name)
            .ToList();

        unmapped.Should().BeEmpty("il kataloğundaki her il bir deprem bölgesine eşlenmelidir");
    }
}
