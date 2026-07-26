using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Renewals.Commands.GeneratePolicyRenewals;
using SigortaPro.Application.Features.Customers.Commands.AddProperty;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

/// <summary>
/// ADR-055/058: Deprem bölgesi kullanıcı beyanı değil, konutun adres İLİNDEN sistem tarafından türetilir.
/// <para>
/// Doğrulanan garantiler: bölge ilden türetilir, kullanıcı bunu API üzerinden gönderemez/değiştiremez,
/// ve konut fiyatlamasında önizleme ↔ oluşturulan teklif paritesi korunur.
/// </para>
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class EarthquakeZoneDerivationIntegrationTests
{
    private const CoveragePackage Package = CoveragePackage.Standart;

    private readonly SigortaProWebApplicationFactory _factory;

    public EarthquakeZoneDerivationIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("İstanbul", 1)]  // 1. derece (Marmara fayı)
    [InlineData("Samsun", 2)]
    [InlineData("Ankara", 3)]
    [InlineData("Konya", 4)]
    public async Task AddProperty_Should_DeriveZoneFromCity(string city, int expectedZone)
    {
        var client = await CustomerClientAsync();

        var property = await AddPropertyAsync(client, city);

        property.EarthquakeZone.Should().Be(expectedZone,
            "deprem bölgesi adresin ilinden türetilmelidir (kullanıcı beyanı değil)");
    }

    [Fact]
    public async Task AddProperty_Should_IgnoreClientSuppliedZone()
    {
        // İstemci gövdeye fazladan "earthquakeZone" koysa bile sözleşmede böyle bir alan YOKTUR;
        // sunucu değeri her hâlükârda ilden türetir → fiyat manipülasyonu mümkün değildir.
        var client = await CustomerClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/customers/me/properties", new
        {
            city = "İstanbul",
            district = "Kadıköy",
            neighborhood = "Caferağa",
            postalCode = "34710",
            buildingAge = 10,
            squareMeters = 120,
            earthquakeZone = 5, // en düşük risk — göz ardı edilmelidir
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var property = (await response.Content.ReadFromJsonAsync<PropertyDto>())!;

        property.EarthquakeZone.Should().Be(1,
            "istemcinin gönderdiği bölge yok sayılmalı, İstanbul için 1 türetilmelidir");
    }

    [Fact]
    public async Task PropertyZone_Should_NotBeChangeableByCustomer()
    {
        // Konut güncelleme ucu YOKTUR ve domain'de bölgeyi değiştiren bir metot bulunmaz (ADR-058):
        // müşteri kaydettikten sonra bölgeyi değiştiremez.
        var client = await CustomerClientAsync();
        var property = await AddPropertyAsync(client, "İstanbul");

        var update = await client.PutAsJsonAsync($"/api/v1/customers/me/properties/{property.Id}", new
        {
            city = "Konya",
            district = "Selçuklu",
            neighborhood = "Bosna",
            postalCode = "42250",
            buildingAge = 10,
            squareMeters = 120,
        });

        update.StatusCode.Should().Be(HttpStatusCode.NotFound, "konut güncelleme ucu bulunmamaktadır");

        // Bölge değişmemiş olmalı.
        var profile = await client.GetFromJsonAsync<CustomerDto>("/api/v1/customers/me");
        profile!.Properties.Single(p => p.Id == property.Id).EarthquakeZone.Should().Be(1);
    }

    [Fact]
    public async Task PropertyQuote_Preview_And_Created_Should_HaveIdenticalPremium()
    {
        // ADR-056 paritesi konut branşında da korunur (bölge her iki yolda da AYNI builder'dan türetilir).
        var client = await CustomerClientAsync();
        var property = await AddPropertyAsync(client, "İstanbul");

        var previewResponse = await client.GetAsync(
            $"/api/v1/quotes/compare?branch={(int)InsuranceBranch.Konut}&propertyId={property.Id}");
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var previewDocument = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var preview = previewDocument.RootElement.GetProperty("packages").EnumerateArray()
            .Single(item => item.GetProperty("coveragePackage").GetInt32() == (int)Package)
            .GetProperty("totalPremium").GetDecimal();

        var createResponse = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Konut, null, property.Id, Package));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var created = createDocument.RootElement.GetProperty("totalPremium").GetDecimal();

        created.Should().Be(preview);
    }

    [Fact]
    public async Task PropertyQuote_Premium_Should_ReflectCityRisk()
    {
        // Bölge gerçekten fiyatlanıyor mu? 1. derece (×1.50) ile 4. derece (×1.05) farklı prim üretmeli.
        var client = await CustomerClientAsync();

        var highRisk = await AddPropertyAsync(client, "İstanbul"); // zone 1
        var lowRisk = await AddPropertyAsync(client, "Konya");     // zone 4

        var highPremium = await CreatedPremiumAsync(client, highRisk.Id);
        var lowPremium = await CreatedPremiumAsync(client, lowRisk.Id);

        highPremium.Should().BeGreaterThan(lowPremium,
            "yüksek deprem riski taşıyan ildeki konut daha yüksek prim üretmelidir");
    }

    [Fact]
    public void RenewalFlow_Should_ResolveWithSharedPricingInputBuilder()
    {
        // ADR-058: Yenileme artık deprem bölgesini kendisi çözmez; ortak IQuotePricingInputBuilder'ı alır.
        // Bu test DI kablolamasını korur — bağımlılık kaydı unutulursa yenileme arkaplan işi çalışmaz.
        using var scope = _factory.Services.CreateScope();

        var builder = scope.ServiceProvider.GetService<IQuotePricingInputBuilder>();
        builder.Should().NotBeNull("ortak fiyatlama girdi kurucusu DI'da kayıtlı olmalıdır");

        var handler = scope.ServiceProvider
            .GetService<IRequestHandler<GeneratePolicyRenewalsCommand, int>>();
        handler.Should().NotBeNull("yenileme handler'ı yeni bağımlılığıyla birlikte çözülebilmelidir");
    }

    // --- Yardımcılar ---

    private async Task<HttpClient> CustomerClientAsync()
    {
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        return TestAccountFactory.CreateAuthorizedClient(_factory, session);
    }

    private static async Task<PropertyDto> AddPropertyAsync(HttpClient client, string city)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customers/me/properties",
            new AddPropertyCommand(city, "Merkez", "Mahalle", "34710", 10, 120));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PropertyDto>())!;
    }

    private static async Task<decimal> CreatedPremiumAsync(HttpClient client, Guid propertyId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Konut, null, propertyId, Package));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("totalPremium").GetDecimal();
    }
}
