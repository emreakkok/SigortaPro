using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Application.Features.Policies.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// Teklif → onay → ödeme → poliçe akışının uçtan uca entegrasyon testleri.
// Kimlik arrange'i TestAccountFactory (ISender) ile yapılır; auth uçlarına HTTP çağrısı yapılmaz (rate limit).
[Collection(IntegrationTestCollection.Name)]
public sealed class QuotePurchaseFlowIntegrationTests
{
    // Mock POS senaryo kartları.
    private const string SuccessCard = "4111111111111111";
    private const string InsufficientFundsCard = "4000000000000002";

    private readonly SigortaProWebApplicationFactory _factory;

    public QuotePurchaseFlowIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PurchaseFlow_Should_CreateActivePolicyAndMarkQuotePurchased_When_PaymentSucceeds()
    {
        // Arrange: müşteri + araç + fiyatlanmış teklif.
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var client = TestAccountFactory.CreateAuthorizedClient(_factory, session);

        var vehicle = await AddVehicleAsync(client);
        var quote = await CreateKaskoQuoteAsync(client, vehicle.Id);

        quote.Status.Should().Be(QuoteStatus.Priced);
        quote.TotalPremium.Should().BeGreaterThan(0);
        quote.PremiumBreakdown.Should().NotBeEmpty("teklif fiyatlama motorunun prim dökümüyle dönmelidir");

        // Act 1: teklifi onayla (Priced → Approved).
        var approveResponse = await client.PostAsync($"/api/v1/quotes/{quote.Id}/approve", content: null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2: mock POS ile öde.
        var purchaseResponse = await client.PostAsJsonAsync(
            "/api/v1/payments",
            CreatePurchaseCommand(quote.Id, SuccessCard));

        // Assert: ödeme başarılı, aktif poliçe üretildi.
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var purchase = await purchaseResponse.Content.ReadFromJsonAsync<PurchaseResultDto>();
        purchase!.PaymentStatus.Should().Be(PaymentStatus.Successful);
        purchase.Amount.Should().Be(quote.TotalPremium);
        purchase.MaskedCardNumber.Should().NotContain(SuccessCard[..12], "kart yalnızca maskeli saklanır (ADR-007)");
        purchase.MaskedCardNumber.Should().EndWith(SuccessCard[^4..]);
        purchase.Policy.Status.Should().Be(PolicyStatus.Active);
        purchase.Policy.PolicyNumber.Should().MatchRegex(@"^POL-\d{4}-\d{6}$");

        // Teklif Purchased durumuna geçti.
        var quoteAfter = await client.GetFromJsonAsync<QuoteDto>($"/api/v1/quotes/{quote.Id}");
        quoteAfter!.Status.Should().Be(QuoteStatus.Purchased);

        // Poliçe "Poliçelerim" listesinde görünür (ucu).
        var policies = await client.GetFromJsonAsync<PagedResult<PolicyListItemDto>>("/api/v1/policies");
        policies!.Items.Should().ContainSingle(policy => policy.Id == purchase.Policy.Id)
            .Which.PolicyNumber.Should().Be(purchase.Policy.PolicyNumber);
    }

    [Fact]
    public async Task PurchaseQuote_Should_Return402AndKeepQuoteApproved_When_CardHasInsufficientFunds()
    {
        // Arrange: onaylanmış teklif.
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var client = TestAccountFactory.CreateAuthorizedClient(_factory, session);

        var vehicle = await AddVehicleAsync(client);
        var quote = await CreateKaskoQuoteAsync(client, vehicle.Id);
        (await client.PostAsync($"/api/v1/quotes/{quote.Id}/approve", content: null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: yetersiz bakiye senaryo kartı ile öde.
        var purchaseResponse = await client.PostAsJsonAsync(
            "/api/v1/payments",
            CreatePurchaseCommand(quote.Id, InsufficientFundsCard));

        // Assert: 402 döner; teklif Approved kalır, poliçe üretilmez.
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        purchaseResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var quoteAfter = await client.GetFromJsonAsync<QuoteDto>($"/api/v1/quotes/{quote.Id}");
        quoteAfter!.Status.Should().Be(QuoteStatus.Approved);

        var policies = await client.GetFromJsonAsync<PagedResult<PolicyListItemDto>>("/api/v1/policies");
        policies!.Items.Should().BeEmpty();

        // Başarısız deneme ödeme geçmişine maskeli kartla yazılır.
        var payments = await client.GetFromJsonAsync<PagedResult<PaymentHistoryItemDto>>("/api/v1/payments");
        payments!.Items.Should().ContainSingle()
            .Which.Should().Match<PaymentHistoryItemDto>(payment =>
                payment.Status == PaymentStatus.Failed && payment.QuoteId == quote.Id);
    }

    [Fact]
    public async Task PurchaseQuote_Should_Return409_When_QuoteIsNotApproved()
    {
        // Arrange: yalnızca Priced durumda (onaylanmamış) teklif.
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var client = TestAccountFactory.CreateAuthorizedClient(_factory, session);

        var vehicle = await AddVehicleAsync(client);
        var quote = await CreateKaskoQuoteAsync(client, vehicle.Id);

        // Act
        var purchaseResponse = await client.PostAsJsonAsync(
            "/api/v1/payments",
            CreatePurchaseCommand(quote.Id, SuccessCard));

        // Assert: kart çekilmeden reddedilir (durum guard'ı → 409).
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateQuote_Should_Return403_When_VehicleBelongsToAnotherCustomer()
    {
        // Arrange: A müşterisinin aracı, B müşterisinin oturumu.
        var ownerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var ownerClient = TestAccountFactory.CreateAuthorizedClient(_factory, ownerSession);
        var vehicle = await AddVehicleAsync(ownerClient);

        var otherSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var otherClient = TestAccountFactory.CreateAuthorizedClient(_factory, otherSession);

        // Act: B, A'nın aracı için teklif oluşturmayı dener.
        var response = await otherClient.PostAsJsonAsync(
            "/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, CoveragePackage.Standart));

        // Assert: kaynak sahipliği ihlali.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<VehicleDto> AddVehicleAsync(HttpClient client)
    {
        // Plaka formatı ValidationPatterns.TurkishPlate ile uyumlu; testler arası çakışmamak için rastgele hane.
        var command = new AddVehicleCommand(
            $"34 TS {Random.Shared.Next(1000, 9999)}",
            "Toyota",
            "Corolla",
            2022,
            132,
            VehicleUsage.Hususi);

        var response = await client.PostAsJsonAsync("/api/v1/customers/me/vehicles", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
    }

    private static async Task<QuoteDto> CreateKaskoQuoteAsync(HttpClient client, Guid vehicleId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicleId, null, CoveragePackage.Standart));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<QuoteDto>())!;
    }

    private static PurchaseQuoteCommand CreatePurchaseCommand(Guid quoteId, string cardNumber) =>
        new(quoteId, cardNumber, "Test Müşteri", "12", "2030", "123", InstallmentCount: 1);
}
