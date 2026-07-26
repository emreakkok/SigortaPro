using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SigortaPro.Application.Features.Cities.DTOs;

namespace SigortaPro.WebAPI.Tests.Integration;

// Post-MVP (ADR-037): İl kataloğu ucunun uçtan uca doğrulaması. Gerçek pipeline'da (DI → Singleton provider →
// gömülü JSON → JWT auth) 81 ilin yüklendiğini ve yetki kuralını kanıtlar.
// Not: /city-catalog auth rate-limit politikasına tabi DEĞİLDİR; register arrange ISender ile yapılır.
[Collection(IntegrationTestCollection.Name)]
public sealed class CityCatalogIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public CityCatalogIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCatalog_Should_Return81Provinces_When_Anonymous()
    {
        // ADR-039: Kayıt formu anonim olduğundan il kataloğu kimlik doğrulaması gerektirmez.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/city-catalog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = await response.Content.ReadFromJsonAsync<CityCatalogDto>();
        catalog.Should().NotBeNull();
        catalog!.Cities.Should().HaveCount(81);
        catalog.Cities.Should().Contain(city => city.Name == "İstanbul");
    }
}
