using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.Commands.UpdateProfile;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// Fiyatlama girdilerinin teklifte dondurulması ve gerçek veriye dayalı faktörler.
// EN KRİTİK GARANTİ: teklif oluşturulduktan sonra müşteri profili değişse bile eski teklifin primi VE
// prim dökümü DEĞİŞMEZ. Ayrıca kullanıcıya sorulmamış hiçbir faktör dökümde gösterilmez.
// E-posta tetiklenmez (host NullEmailService); auth HTTP bütçesi ISender ile korunur.
[Collection(IntegrationTestCollection.Name)]
public sealed class PricingDeterminismIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public PricingDeterminismIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task QuotePricing_Should_StayIdentical_When_CustomerChangesCityAfterQuote()
    {
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);

        // İstanbul'da (il risk katsayısı 1.25) Kasko teklifi oluşturulur.
        var quoteId = await CreateKaskoQuoteAsync(client, vehicle.Id);
        var (premiumBefore, breakdownBefore) = await ReadPricingAsync(client, quoteId);

        // Müşteri ilini risk katsayısı FARKLI bir ile taşır (İstanbul 1.25 → Konya 1.00).
        var move = await client.PutAsJsonAsync("/api/v1/customers/me",
            new UpdateProfileCommand("Test", "Müşteri", "+905321112233", "Konya", "Selçuklu", "Bosna", "42250"));
        move.StatusCode.Should().Be(HttpStatusCode.OK);

        // Eski teklif yeniden okunur → hem prim hem DÖKÜM birebir aynı kalmalıdır.
        var (premiumAfter, breakdownAfter) = await ReadPricingAsync(client, quoteId);

        premiumAfter.Should().Be(premiumBefore, "teklif primi profil değişikliğinden etkilenmemelidir");
        breakdownAfter.Should().Equal(breakdownBefore,
            "prim dökümü dondurulmuş girdilerden üretilir; profil değişikliği geçmiş teklifi değiştiremez");
    }

    [Fact]
    public async Task Breakdown_Should_NeverContainNoClaimFactor()
    {
        // Hasarsızlık basamağı sistemde türetilmiyor → kullanıcıya aktif bir faktör gibi GÖSTERİLMEZ.
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);
        var quoteId = await CreateKaskoQuoteAsync(client, vehicle.Id);

        var (_, breakdown) = await ReadPricingAsync(client, quoteId);

        breakdown.Should().NotContain(factor => factor.Contains("Hasarsızlık", StringComparison.Ordinal));
        breakdown.Should().Contain(factor => factor.Contains("Sürücü Yaşı", StringComparison.Ordinal),
            "gerçek veriye dayanan faktörler gösterilmeye devam etmelidir");
    }

    [Fact]
    public async Task HealthQuote_Should_RequireSmokerDeclaration()
    {
        // Beyan alınmadan sağlık teklifi oluşturulamaz — sessizce "içmiyor" varsayılmaz.
        var client = await CustomerClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthQuote_SmokerDeclaration_Should_AffectPremiumAndBeShownInBreakdown()
    {
        var client = await CustomerClientAsync();

        var nonSmokerId = await CreateHealthQuoteAsync(client, isSmoker: false);
        var smokerId = await CreateHealthQuoteAsync(client, isSmoker: true);

        var (nonSmokerPremium, nonSmokerBreakdown) = await ReadPricingAsync(client, nonSmokerId);
        var (smokerPremium, smokerBreakdown) = await ReadPricingAsync(client, smokerId);

        smokerPremium.Should().BeGreaterThan(nonSmokerPremium,
            "sigara beyanı artık gerçekten fiyatlanır (önceden sabit false idi)");

        // Beyan alındığı için faktör dökümde görünür — her iki beyanda da.
        smokerBreakdown.Should().Contain(factor => factor.Contains("Sigara", StringComparison.Ordinal));
        nonSmokerBreakdown.Should().Contain(factor => factor.Contains("Sigara", StringComparison.Ordinal));
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

    private static async Task<string> CreateKaskoQuoteAsync(HttpClient client, Guid vehicleId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicleId, null, CoveragePackage.Standart));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateHealthQuoteAsync(HttpClient client, bool isSmoker)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, IsSmoker: isSmoker));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetString()!;
    }

    /// <summary>Teklifin primi ve prim dökümü (faktör adı + çarpan) — karşılaştırılabilir biçimde.</summary>
    private static async Task<(decimal Premium, List<string> Breakdown)> ReadPricingAsync(
        HttpClient client, string quoteId)
    {
        var response = await client.GetAsync($"/api/v1/quotes/{quoteId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        var breakdown = root.GetProperty("premiumBreakdown").EnumerateArray()
            .Select(item =>
                $"{item.GetProperty("factor").GetString()}={item.GetProperty("multiplier").GetDecimal()}")
            .ToList();

        return (root.GetProperty("totalPremium").GetDecimal(), breakdown);
    }
}
