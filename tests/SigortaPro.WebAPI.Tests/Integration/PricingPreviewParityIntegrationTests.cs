using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SigortaPro.Application.Features.Customers.Commands.AddProperty;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

/// <summary>
/// ADR-056 — REGRESYON KORUMASI: Karşılaştırma önizlemesinde gösterilen prim ile aynı seçimle
/// OLUŞTURULAN teklifin primi **birebir aynı** olmalıdır.
/// <para>
/// Bu test, önizleme ve oluşturma akışlarının fiyatlama girdisini ayrı ayrı kurmasından doğan gerçek bir
/// hatayı yakalamak için yazıldı: sigara beyanı ve adresten türetilen deprem bölgesi yalnızca oluşturma
/// yolunda uygulanıyordu; kullanıcıya bir fiyat gösterilip başka fiyat uygulanıyordu.
/// Gelecekte biri yeni bir risk faktörünü YALNIZCA tek akışa eklerse bu testler kırılır.
/// </para>
/// E-posta tetiklenmez (host NullEmailService); auth HTTP bütçesi ISender ile korunur (ADR-034).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class PricingPreviewParityIntegrationTests
{
    // Karşılaştırmada ve teklifte aynı paket seçilir; parite bu paket üzerinden ölçülür.
    private const CoveragePackage Package = CoveragePackage.Standart;

    private readonly SigortaProWebApplicationFactory _factory;

    public PricingPreviewParityIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preview_And_CreatedQuote_Should_HaveIdenticalPremium_ForVehicleBranch()
    {
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);

        var previewPremium = await PreviewPremiumAsync(
            client, $"branch={(int)InsuranceBranch.Kasko}&vehicleId={vehicle.Id}");
        var createdPremium = await CreatedPremiumAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, Package));

        createdPremium.Should().Be(previewPremium);
    }

    [Fact]
    public async Task Preview_And_CreatedQuote_Should_HaveIdenticalPremium_ForPropertyBranch()
    {
        // Deprem bölgesi her iki akışta da adresin İLİNDEN türetilmelidir (ADR-055).
        var client = await CustomerClientAsync();
        var property = await AddPropertyAsync(client);

        var previewPremium = await PreviewPremiumAsync(
            client, $"branch={(int)InsuranceBranch.Konut}&propertyId={property.Id}");
        var createdPremium = await CreatedPremiumAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Konut, null, property.Id, Package));

        createdPremium.Should().Be(previewPremium);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Preview_And_CreatedQuote_Should_HaveIdenticalPremium_ForHealthBranch(bool isSmoker)
    {
        // Sigara beyanı (ADR-054) her iki akışta da UYGULANMALIDIR; aksi hâlde beyan eden kullanıcıya
        // önizlemede düşük fiyat gösterilip teklifte %25 fazlası uygulanırdı.
        var client = await CustomerClientAsync();

        var previewPremium = await PreviewPremiumAsync(
            client, $"branch={(int)InsuranceBranch.Saglik}&isSmoker={isSmoker.ToString().ToLowerInvariant()}");
        var createdPremium = await CreatedPremiumAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, Package, IsSmoker: isSmoker));

        createdPremium.Should().Be(previewPremium);
    }

    [Fact]
    public async Task HealthPreview_Should_Reject_When_SmokerDeclarationMissing()
    {
        // Kural teklif oluşturmayla birebir aynıdır: beyansız önizleme, sapmalı fiyat üretemesin diye reddedilir.
        var client = await CustomerClientAsync();

        var response = await client.GetAsync(
            $"/api/v1/quotes/compare?branch={(int)InsuranceBranch.Saglik}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthPreview_Should_ReflectSmokerDeclarationInPremium()
    {
        // Beyan önizlemede gerçekten fiyatlanıyor mu? (Sadece "eşit" olmaları yetmez; etkili de olmalı.)
        var client = await CustomerClientAsync();

        var nonSmoker = await PreviewPremiumAsync(
            client, $"branch={(int)InsuranceBranch.Saglik}&isSmoker=false");
        var smoker = await PreviewPremiumAsync(
            client, $"branch={(int)InsuranceBranch.Saglik}&isSmoker=true");

        smoker.Should().BeGreaterThan(nonSmoker);
    }

    // --- Yardımcılar ---

    private async Task<HttpClient> CustomerClientAsync()
    {
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        return TestAccountFactory.CreateAuthorizedClient(_factory, session);
    }

    private static async Task<VehicleDto> AddVehicleAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customers/me/vehicles",
            new AddVehicleCommand($"34 TS {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132, VehicleUsage.Hususi));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
    }

    private static async Task<PropertyDto> AddPropertyAsync(HttpClient client)
    {
        // ADR-055: earthquakeZone artık istekte YOKTUR — sistem adresin ilinden türetir.
        var response = await client.PostAsJsonAsync("/api/v1/customers/me/properties",
            new AddPropertyCommand("İstanbul", "Kadıköy", "Caferağa", "34710", 10, 120));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PropertyDto>())!;
    }

    /// <summary>Karşılaştırma önizlemesinde seçilen paketin primi.</summary>
    private static async Task<decimal> PreviewPremiumAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/api/v1/quotes/compare?{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var package = document.RootElement.GetProperty("packages").EnumerateArray()
            .Single(item => item.GetProperty("coveragePackage").GetInt32() == (int)Package);

        return package.GetProperty("totalPremium").GetDecimal();
    }

    /// <summary>Aynı seçimle oluşturulan teklifin primi.</summary>
    private static async Task<decimal> CreatedPremiumAsync(HttpClient client, CreateQuoteCommand command)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("totalPremium").GetDecimal();
    }
}
