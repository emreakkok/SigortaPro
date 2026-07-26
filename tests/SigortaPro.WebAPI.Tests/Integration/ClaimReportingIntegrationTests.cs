using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SigortaPro.Application.Features.Claims.Commands.CreateClaim;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// Hasar bildiriminin teminat penceresi kontrolünün uçtan uca doğrulaması (saat hassasiyetli).
// EN KRİTİK KABUL: poliçe bugün satın alma anında (saat dahil) aktifleşir; aynı gün başlangıçtan SONRAKİ
// bir olay "poliçe başlangıcından önce" gerekçesiyle REDDEDİLMEMELİDİR. Başlangıçtan önceki an ise reddedilir.
// E-posta tetiklenmez (host NullEmailService); auth HTTP bütçesi TestAccountFactory (ISender) ile korunur (ADR-034).
[Collection(IntegrationTestCollection.Name)]
public sealed class ClaimReportingIntegrationTests
{
    private const string SuccessCard = "4111111111111111";

    private readonly SigortaProWebApplicationFactory _factory;

    public ClaimReportingIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SameDayClaim_Should_BeAccepted_When_IncidentIsAfterPolicyStart()
    {
        var client = await CustomerWithActivePolicyClientAsync();
        var policy = await PurchaseActivePolicyAsync(client);

        // Olay, poliçe aktifleştikten (satın alma anı) sonra, aynı gün gerçekleşti → geçerli.
        var incidentAt = DateTime.UtcNow;
        incidentAt.Should().BeAfter(policy.StartDate, "olay poliçe başlangıcından sonradır");

        var response = await client.PostAsJsonAsync("/api/v1/claims",
            new CreateClaimCommand(policy.Id, incidentAt, "Aynı gün oluşan hasar.", 5000m));

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "aynı gün poliçe başlangıcından sonra gerçekleşen hasar reddedilmemelidir");
    }

    [Fact]
    public async Task Claim_Should_BeRejected_When_IncidentIsOneSecondBeforePolicyStart()
    {
        var client = await CustomerWithActivePolicyClientAsync();
        var policy = await PurchaseActivePolicyAsync(client);

        // Poliçe aktif olmadan bir saniye önceki olay → geçersiz (saat hassasiyetli sınır).
        var incidentAt = policy.StartDate.AddSeconds(-1);

        var response = await client.PostAsJsonAsync("/api/v1/claims",
            new CreateClaimCommand(policy.Id, incidentAt, "Poliçe başlamadan önceki olay.", 5000m));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "poliçe başlangıç anından önce gerçekleşen olay reddedilmelidir");
    }

    [Fact]
    public async Task Claim_Should_BeRejected_When_IncidentIsInFuture()
    {
        var client = await CustomerWithActivePolicyClientAsync();
        var policy = await PurchaseActivePolicyAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/claims",
            new CreateClaimCommand(policy.Id, DateTime.UtcNow.AddDays(1), "Gelecekteki olay.", 5000m));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict, "gelecekteki olay reddedilmelidir");
    }

    // --- Arrange yardımcıları (QuotePurchaseFlowIntegrationTests deseniyle) ---

    private async Task<HttpClient> CustomerWithActivePolicyClientAsync()
    {
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        return TestAccountFactory.CreateAuthorizedClient(_factory, session);
    }

    private static async Task<PolicySummaryDto> PurchaseActivePolicyAsync(HttpClient client)
    {
        var vehicle = await AddVehicleAsync(client);
        var quote = await CreateKaskoQuoteAsync(client, vehicle.Id);

        (await client.PostAsync($"/api/v1/quotes/{quote.Id}/approve", content: null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var purchaseResponse = await client.PostAsJsonAsync("/api/v1/payments",
            new PurchaseQuoteCommand(quote.Id, SuccessCard, "Test Müşteri", "12", "2030", "123", InstallmentCount: 1));
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var purchase = await purchaseResponse.Content.ReadFromJsonAsync<PurchaseResultDto>();
        purchase!.Policy.Status.Should().Be(PolicyStatus.Active);
        return purchase.Policy;
    }

    private static async Task<VehicleDto> AddVehicleAsync(HttpClient client)
    {
        var command = new AddVehicleCommand(
            $"34 TS {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132, VehicleUsage.Hususi);
        var response = await client.PostAsJsonAsync("/api/v1/customers/me/vehicles", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
    }

    private static async Task<QuoteDto> CreateKaskoQuoteAsync(HttpClient client, Guid vehicleId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicleId, null, CoveragePackage.Standart));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<QuoteDto>())!;
    }
}
