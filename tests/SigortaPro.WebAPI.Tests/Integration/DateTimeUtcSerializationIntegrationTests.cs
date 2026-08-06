using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using SigortaPro.Application.Features.Claims.Commands.CreateClaim;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// TIMEZONE (T1/T5): Instant DateTime alanlarının veritabanından okunduktan SONRA da API çıktısında
// UTC + "Z" ile serileştirildiğini kanıtlar (13:23 → 10:23 kök nedeninin çözümü). Kök neden: EF `datetime2`
// materializasyonu Kind=Unspecified üretiyor, System.Text.Json "Z" koymuyordu; UtcDateTimeConverters bunu
// okumada Kind=Utc'ye çekiyor. E-posta tetiklenmez (NullEmailService); auth ISender ile.
[Collection(IntegrationTestCollection.Name)]
public sealed class DateTimeUtcSerializationIntegrationTests
{
    private const string SuccessCard = "4111111111111111";

    private readonly SigortaProWebApplicationFactory _factory;

    public DateTimeUtcSerializationIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatedAt_Should_SerializeAsUtcWithZ_When_ReadFromDatabase()
    {
        // Müşteri kaydı bir Customer + CreatedAt (UtcNow) üretir; ARDINDAN ayrı bir GET isteğiyle DB'den
        // yeniden okunur → converter Kind=Utc işaretler → JSON "Z" ile döner.
        await TestAccountFactory.RegisterCustomerAsync(_factory);
        var admin = await TestAccountFactory.AdminClientAsync(_factory);

        var response = await admin.GetAsync("/api/v1/customers?page=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rawJson = await response.Content.ReadAsStringAsync();

        // DB'den okunan CreatedAt, ISO-8601 + "Z" ile serileştirilmeli (Kind=Unspecified değil).
        Regex.IsMatch(rawJson, "\"createdAt\":\"[^\"]+Z\"")
            .Should().BeTrue("DB'den okunan instant alanlar UTC \"Z\" ekiyle serileştirilmelidir (T1). JSON: {0}", rawJson);
    }

    [Fact]
    public async Task ClaimIncidentDate_Should_RoundTripAsUtcInstant_With_Z()
    {
        // Senaryo: Türkiye 13:23 = UTC 10:23. Frontend UTC "Z" gönderir; burada bunu doğrudan taklit ediyoruz.
        var customerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, customerSession);
        var policy = await PurchaseActivePolicyAsync(customer);

        // Poliçe satın alındıktan SONRA bir UTC anı → poliçe penceresi içinde ve gelecekte değil.
        // (Frontend'in "13:23 → 10:23Z" dönüşümünün backend karşılığı: Kind=Utc bir instant gönderilir.)
        var incidentUtc = DateTime.UtcNow;
        incidentUtc.Kind.Should().Be(DateTimeKind.Utc);

        var createResponse = await customer.PostAsJsonAsync("/api/v1/claims",
            new CreateClaimCommand(policy.Id, incidentUtc, "Timezone round-trip testi.", 5000m));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ClaimSummaryDto>())!;

        // DB'den YENİDEN okuyan detay çağrısı (asıl kök nedenin test edildiği yol).
        var detailResponse = await customer.GetAsync($"/api/v1/claims/{created.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rawJson = await detailResponse.Content.ReadAsStringAsync();
        Regex.IsMatch(rawJson, "\"incidentDate\":\"[^\"]+Z\"")
            .Should().BeTrue("hasar olay anı UTC \"Z\" ile dönmelidir. JSON: {0}", rawJson);

        // Aynı instant'ı taşımalı (yerel/UTC kayması olmadan).
        using var document = JsonDocument.Parse(rawJson);
        var incidentString = document.RootElement.GetProperty("incidentDate").GetString()!;
        var parsed = DateTimeOffset.Parse(incidentString, CultureInfo.InvariantCulture).UtcDateTime;
        parsed.Should().BeCloseTo(incidentUtc, TimeSpan.FromSeconds(1),
            "gönderilen UTC anı, geri okunduğunda aynı UTC anı olmalıdır (kayma yok)");
    }

    private static async Task<PolicySummaryDto> PurchaseActivePolicyAsync(HttpClient client)
    {
        var vehicleResponse = await client.PostAsJsonAsync("/api/v1/customers/me/vehicles",
            new AddVehicleCommand($"34 TS {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132, VehicleUsage.Hususi));
        vehicleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleDto>())!;

        var quoteResponse = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, CoveragePackage.Standart));
        quoteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var quote = (await quoteResponse.Content.ReadFromJsonAsync<QuoteDto>())!;

        (await client.PostAsync($"/api/v1/quotes/{quote.Id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var purchaseResponse = await client.PostAsJsonAsync("/api/v1/payments",
            new PurchaseQuoteCommand(quote.Id, SuccessCard, "Test Müşteri", "12", "2030", "123", InstallmentCount: 1));
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var purchase = (await purchaseResponse.Content.ReadFromJsonAsync<PurchaseResultDto>())!;
        return purchase.Policy;
    }
}
