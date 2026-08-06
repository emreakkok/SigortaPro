using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SigortaPro.Application.Features.Vehicles.DTOs;

namespace SigortaPro.WebAPI.Tests.Integration;

// : Araç kataloğu ucunun uçtan uca doğrulaması. Gerçek pipeline'da (DI → Singleton provider →
// gömülü JSON kaynağı → JWT auth) katalogun yüklendiğini ve yetki kuralını kanıtlar.
// Not: /vehicle-catalog auth rate-limit politikasına tabi DEĞİLDİR (yalnızca AuthController); bu testler
// koleksiyonun 10/dk auth HTTP bütçesini tüketmez (register arrange ISender ile yapılır).
[Collection(IntegrationTestCollection.Name)]
public sealed class VehicleCatalogIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public VehicleCatalogIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCatalog_Should_ReturnBrandsWithModels_When_Authenticated()
    {
        // Arrange: kimliği doğrulanmış bir kullanıcı (register ISender ile — rate limit bütçesine dokunmaz).
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var client = TestAccountFactory.CreateAuthorizedClient(_factory, session);

        // Act
        var response = await client.GetAsync("/api/v1/vehicle-catalog");

        // Assert: gömülü JSON gerçek host'ta yüklenir ve serileşir.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = await response.Content.ReadFromJsonAsync<VehicleCatalogDto>();
        catalog.Should().NotBeNull();
        catalog!.Brands.Should().NotBeEmpty();
        catalog.Brands.Should().Contain(brand => brand.Name == "Toyota" && brand.Models.Contains("Corolla"));
    }

    [Fact]
    public async Task GetCatalog_Should_Return401_When_Unauthenticated()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/vehicle-catalog");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
