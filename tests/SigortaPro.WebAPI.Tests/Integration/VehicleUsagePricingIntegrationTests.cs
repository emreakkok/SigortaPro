using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SigortaPro.Application.Features.Customers.Commands.AddProperty;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.Commands.UpdateVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

/// <summary>
/// Araç kullanım amacı (hususi/ticari/taksi) fiyatlaması — uçtan uca.
/// Doğrulanan garantiler: faktör yalnızca araç branşlarını etkiler, önizleme ↔ teklif paritesi korunur,
/// beyan teklif anında snapshot'lanır (araç sonradan değişse bile eski teklif değişmez) ve
/// beyanı olmayan araçlardan üretilen tekliflere faktör geriye dönük uygulanmaz.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class VehicleUsagePricingIntegrationTests
{
    private const CoveragePackage Package = CoveragePackage.Standart;

    private readonly SigortaProWebApplicationFactory _factory;

    public VehicleUsagePricingIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Premium_Should_IncreaseWithRiskierUsage()
    {
        var client = await CustomerClientAsync();

        var hususi = await QuotePremiumForUsageAsync(client, VehicleUsage.Hususi);
        var ticari = await QuotePremiumForUsageAsync(client, VehicleUsage.Ticari);
        var taksi = await QuotePremiumForUsageAsync(client, VehicleUsage.Taksi);

        ticari.Should().BeGreaterThan(hususi, "ticari kullanım daha yüksek maruziyet taşır");
        taksi.Should().BeGreaterThan(ticari, "taksi kullanımı en yüksek maruziyeti taşır");
    }

    [Theory]
    [InlineData(VehicleUsage.Hususi)]
    [InlineData(VehicleUsage.Ticari)]
    [InlineData(VehicleUsage.Taksi)]
    public async Task Preview_And_CreatedQuote_Should_HaveIdenticalPremium(VehicleUsage usage)
    {
        // paritesi yeni faktörle de korunuyor mu? (Ortak QuotePricingInputBuilder sayesinde.)
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client, usage);

        var preview = await PreviewPremiumAsync(client, $"branch={(int)InsuranceBranch.Kasko}&vehicleId={vehicle.Id}");
        var created = await CreatedPremiumAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, Package));

        created.Should().Be(preview);
    }

    [Fact]
    public async Task Breakdown_Should_ShowUsageFactor_WithDeclaredValue()
    {
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client, VehicleUsage.Taksi);
        var quoteId = await CreateQuoteIdAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, Package));

        var (_, breakdown) = await ReadPricingAsync(client, quoteId);

        breakdown.Should().Contain(line => line.StartsWith("Kullanım Amacı=", StringComparison.Ordinal));
        breakdown.Should().Contain("Kullanım Amacı=1.60", "taksi katsayısı dökümde açıkça görünmelidir");
    }

    [Fact]
    public async Task ChangingVehicleUsage_Should_NotAffectExistingQuote()
    {
        // determinizmi: beyan teklif anında dondurulur.
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client, VehicleUsage.Hususi);

        var quoteId = await CreateQuoteIdAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, Package));
        var (premiumBefore, breakdownBefore) = await ReadPricingAsync(client, quoteId);

        // Araç TAKSİ'ye çevrilir (çok daha pahalı bir kullanım).
        var update = await client.PutAsJsonAsync($"/api/v1/customers/me/vehicles/{vehicle.Id}",
            new UpdateVehicleCommand(vehicle.Id, vehicle.PlateNumber, vehicle.Brand, vehicle.Model,
                vehicle.ManufactureYear, vehicle.EnginePowerHp, VehicleUsage.Taksi));
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var (premiumAfter, breakdownAfter) = await ReadPricingAsync(client, quoteId);

        premiumAfter.Should().Be(premiumBefore, "eski teklifin primi araç güncellemesinden etkilenmemelidir");
        breakdownAfter.Should().Equal(breakdownBefore, "prim dökümü de dondurulmuş beyandan üretilir");

        // Buna karşılık YENİ teklif güncel beyanı kullanır.
        var newQuoteId = await CreateQuoteIdAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, Package));
        var (newPremium, _) = await ReadPricingAsync(client, newQuoteId);
        newPremium.Should().BeGreaterThan(premiumBefore, "yeni teklif güncel (taksi) beyanıyla fiyatlanır");
    }

    [Fact]
    public async Task UnrelatedBranches_Should_NotBeAffectedByUsageFactor()
    {
        // Sağlık ve Konut/DASK dökümlerinde kullanım amacı kalemi BULUNMAMALIDIR.
        var client = await CustomerClientAsync();

        var healthId = await CreateQuoteIdAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, Package, IsSmoker: false));
        var property = await AddPropertyAsync(client);
        var propertyId = await CreateQuoteIdAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Konut, null, property.Id, Package));

        var (_, healthBreakdown) = await ReadPricingAsync(client, healthId);
        var (_, propertyBreakdown) = await ReadPricingAsync(client, propertyId);

        healthBreakdown.Should().NotContain(line => line.StartsWith("Kullanım Amacı=", StringComparison.Ordinal));
        propertyBreakdown.Should().NotContain(line => line.StartsWith("Kullanım Amacı=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddVehicle_Should_Reject_When_UsagePurposeMissing()
    {
        // API doğrulaması: beyan zorunludur (sessiz varsayım yasak).
        var client = await CustomerClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/customers/me/vehicles",
            new AddVehicleCommand($"34 TS {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VehicleEndpoints_Should_RequireAuthentication()
    {
        // Mevcut yetkilendirme kuralları korunur.
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/v1/customers/me/vehicles",
            new AddVehicleCommand("34 TS 1234", "Toyota", "Corolla", 2022, 132, VehicleUsage.Hususi));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Yardımcılar ---

    private async Task<HttpClient> CustomerClientAsync()
    {
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        return TestAccountFactory.CreateAuthorizedClient(_factory, session);
    }

    private static async Task<decimal> QuotePremiumForUsageAsync(HttpClient client, VehicleUsage usage)
    {
        var vehicle = await AddVehicleAsync(client, usage);
        return await CreatedPremiumAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, Package));
    }

    private static async Task<VehicleDto> AddVehicleAsync(HttpClient client, VehicleUsage usage)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customers/me/vehicles",
            new AddVehicleCommand($"34 TS {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132, usage));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
    }

    private static async Task<PropertyDto> AddPropertyAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customers/me/properties",
            new AddPropertyCommand("İstanbul", "Kadıköy", "Caferağa", "34710", 10, 120));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PropertyDto>())!;
    }

    private static async Task<decimal> PreviewPremiumAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/api/v1/quotes/compare?{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("packages").EnumerateArray()
            .Single(item => item.GetProperty("coveragePackage").GetInt32() == (int)Package)
            .GetProperty("totalPremium").GetDecimal();
    }

    private static async Task<decimal> CreatedPremiumAsync(HttpClient client, CreateQuoteCommand command)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("totalPremium").GetDecimal();
    }

    private static async Task<string> CreateQuoteIdAsync(HttpClient client, CreateQuoteCommand command)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task<(decimal Premium, List<string> Breakdown)> ReadPricingAsync(
        HttpClient client, string quoteId)
    {
        var response = await client.GetAsync($"/api/v1/quotes/{quoteId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        var breakdown = root.GetProperty("premiumBreakdown").EnumerateArray()
            .Select(item =>
                $"{item.GetProperty("factor").GetString()}={item.GetProperty("multiplier").GetDecimal().ToString(CultureInfo.InvariantCulture)}")
            .ToList();

        return (root.GetProperty("totalPremium").GetDecimal(), breakdown);
    }
}
