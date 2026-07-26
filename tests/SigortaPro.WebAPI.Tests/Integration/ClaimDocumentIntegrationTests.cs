using System.Net;
using System.Net.Http.Json;
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

// Hasar belgeleri: müşteri hasar bildiriminde belge (foto/PDF) yükler; belge GERÇEK olarak saklanır ve
// müşteri + Admin + Personel değerlendirmede görüntüler. Erişim yetkisi (sahip müşteri / personel) doğrulanır.
[Collection(IntegrationTestCollection.Name)]
public sealed class ClaimDocumentIntegrationTests
{
    private const string SuccessCard = "4111111111111111";
    private static readonly byte[] SampleImage = { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8 };

    private readonly SigortaProWebApplicationFactory _factory;

    public ClaimDocumentIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ClaimDocument_Should_BeUploaded_AndVisibleToCustomerAdminAndPersonel_WithAuthorization()
    {
        // Müşteri + aktif poliçe + belgeli hasar bildirimi.
        var ownerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var owner = TestAccountFactory.CreateAuthorizedClient(_factory, ownerSession);
        var policy = await PurchaseActivePolicyAsync(owner);

        var createResponse = await owner.PostAsJsonAsync("/api/v1/claims", new CreateClaimCommand(
            policy.Id, DateTime.UtcNow, "Belgeli hasar.", 5000m,
            new[] { new CreateClaimDocument("hasar.jpg", "image/jpeg", SampleImage) }));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ClaimDto>())!;

        // Belge metadata'sı oluşturma yanıtında ve detayda döner.
        created.Documents.Should().ContainSingle();
        var document = created.Documents.Single();
        document.FileName.Should().Be("hasar.jpg");
        document.IsImage.Should().BeTrue();
        document.FileSizeBytes.Should().Be(SampleImage.Length);

        var detail = await owner.GetFromJsonAsync<ClaimDto>($"/api/v1/claims/{created.Id}");
        detail!.Documents.Should().ContainSingle().Which.Id.Should().Be(document.Id);

        var documentUrl = $"/api/v1/claims/{created.Id}/documents/{document.Id}";

        // Sahip müşteri belgeyi görebilir (içerik doğru dönmeli).
        var ownerContent = await owner.GetAsync(documentUrl);
        ownerContent.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ownerContent.Content.ReadAsByteArrayAsync()).Should().Equal(SampleImage);

        // Admin ve Personel değerlendirmede belgeyi görebilir.
        var admin = await TestAccountFactory.AdminClientAsync(_factory);
        (await admin.GetAsync(documentUrl)).StatusCode.Should().Be(HttpStatusCode.OK,
            "Admin hasar değerlendirmesinde belgeyi görüntüleyebilmelidir");

        var personel = await TestAccountFactory.StaffClientAsync(_factory);
        (await personel.GetAsync(documentUrl)).StatusCode.Should().Be(HttpStatusCode.OK,
            "Personel hasar değerlendirmesinde belgeyi görüntüleyebilmelidir");

        // Başka bir müşteri erişemez (kaynak sahipliği).
        var otherSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var other = TestAccountFactory.CreateAuthorizedClient(_factory, otherSession);
        (await other.GetAsync(documentUrl)).StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "başka bir müşteri, başkasının hasar belgesine erişemez");

        // Anonim erişemez.
        (await _factory.CreateClient().GetAsync(documentUrl)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Claim_Should_BeCreatable_WithoutDocuments()
    {
        // Belge zorunlu değildir (opsiyonel) — mevcut hasar akışı bozulmaz.
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, session);
        var policy = await PurchaseActivePolicyAsync(customer);

        var response = await customer.PostAsJsonAsync("/api/v1/claims",
            new CreateClaimCommand(policy.Id, DateTime.UtcNow, "Belgesiz hasar.", 3000m));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = (await response.Content.ReadFromJsonAsync<ClaimDto>())!;
        claim.Documents.Should().BeEmpty();
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
