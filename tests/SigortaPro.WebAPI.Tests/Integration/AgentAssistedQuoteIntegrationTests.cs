using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// ACENTE DESTEKLİ TEKLİF (agent-assisted): Personel/Admin, seçtiği müşteri ADINA teklif oluşturur. Teklif
// SAHİBİ müşteridir; onay/ödeme/poliçeleştirme müşteriye aittir (personel yapamaz). Bu testler gerçek akışı
// uçtan uca doğrular: personel oluşturur → müşteri kendi hesabında görür ve onaylar; personel onaylayamaz.
[Collection(IntegrationTestCollection.Name)]
public sealed class AgentAssistedQuoteIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public AgentAssistedQuoteIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Staff_Should_CreateQuoteForCustomer_OwnedByCustomer_ThenCustomerApproves_ButStaffCannot()
    {
        // Arrange: müşteri kaydı (kendi client'ı) + personel (acente) client'ı.
        var customerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customerClient = TestAccountFactory.CreateAuthorizedClient(_factory, customerSession);
        var customerId = (await customerClient.GetFromJsonAsync<CustomerDto>("/api/v1/customers/me"))!.Id;

        var staffClient = await TestAccountFactory.StaffClientAsync(_factory);

        // Act 1: personel, müşteri ADINA araç ekler (telefonla teklif hazırlarken risk objesi kayıtlı değilse).
        var vehicleResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/customers/{customerId}/vehicles",
            new AddVehicleCommand($"34 AC {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132, VehicleUsage.Hususi));
        vehicleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleDto>())!;

        // Act 2: personel, müşteri ADINA teklif oluşturur.
        var createResponse = await staffClient.PostAsJsonAsync(
            $"/api/v1/customers/{customerId}/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, CoveragePackage.Standart));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<QuoteDto>())!;

        // Teklif sahibi müşteridir; kaynak acente destekli; personelin adı personel yüzeyine döner.
        created.CustomerId.Should().Be(customerId);
        created.Status.Should().Be(QuoteStatus.Priced);
        created.Source.Should().Be(QuoteSource.AgentAssisted);

        var staffView = await staffClient.GetFromJsonAsync<QuoteDto>($"/api/v1/quotes/{created.Id}");
        staffView!.Source.Should().Be(QuoteSource.AgentAssisted);
        staffView.CreatedByStaffName.Should().Be("Örnek Personel", "üreten personelin adı acente yüzeyinde görünür");

        // Müşteri kendi hesabında teklifi görür — kaynak acente destekli, personel kimliği SIZDIRILMAZ.
        var customerView = await customerClient.GetFromJsonAsync<QuoteDto>($"/api/v1/quotes/{created.Id}");
        customerView!.Source.Should().Be(QuoteSource.AgentAssisted);
        customerView.CreatedByStaffName.Should().BeNull("müşteri yüzeyine personel kimliği taşınmaz (KVKK)");

        // Teklif müşterinin "Tekliflerim" listesinde görünür.
        var customerQuotes = await customerClient.GetFromJsonAsync<PagedResult<QuoteSummaryDto>>("/api/v1/quotes");
        customerQuotes!.Items.Should().Contain(q => q.Id == created.Id && q.Source == QuoteSource.AgentAssisted);

        // Act 3 + Assert: PERSONEL müşteri adına ONAYLAYAMAZ (approve yalnızca Customer rolüne açık → 403).
        (await staffClient.PostAsync($"/api/v1/quotes/{created.Id}/approve", content: null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "personel müşteri adına teklifi onaylayamaz");

        // Act 4 + Assert: MÜŞTERİ kendi hesabından onaylar (Priced → Approved).
        (await customerClient.PostAsync($"/api/v1/quotes/{created.Id}/approve", content: null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var afterApprove = await customerClient.GetFromJsonAsync<QuoteDto>($"/api/v1/quotes/{created.Id}");
        afterApprove!.Status.Should().Be(QuoteStatus.Approved);
    }

    [Fact]
    public async Task Customer_Should_NotCreateQuoteForAnotherCustomer_ViaStaffRoute()
    {
        // Hedef müşteri.
        var targetSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var targetClient = TestAccountFactory.CreateAuthorizedClient(_factory, targetSession);
        var targetId = (await targetClient.GetFromJsonAsync<CustomerDto>("/api/v1/customers/me"))!.Id;

        // Başka bir müşteri, personel ucunu kullanarak hedef adına teklif oluşturmayı dener.
        var attackerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var attackerClient = TestAccountFactory.CreateAuthorizedClient(_factory, attackerSession);

        var response = await attackerClient.PostAsJsonAsync(
            $"/api/v1/customers/{targetId}/quotes",
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, IsSmoker: false));

        // Personel ucu Staff rolüne kilitli → müşteri erişemez (403).
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SelfServiceQuote_Should_HaveSelfServiceSource()
    {
        // Müşteri kendi teklifini oluşturursa kaynak SelfService olmalıdır (davranış regresyonu kontrolü).
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var client = TestAccountFactory.CreateAuthorizedClient(_factory, session);

        var vehicleResponse = await client.PostAsJsonAsync("/api/v1/customers/me/vehicles",
            new AddVehicleCommand($"34 SS {Random.Shared.Next(1000, 9999)}", "Fiat", "Egea", 2021, 95, VehicleUsage.Hususi));
        vehicleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleDto>())!;

        var createResponse = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, CoveragePackage.Standart));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<QuoteDto>())!;

        created.Source.Should().Be(QuoteSource.SelfService);
        created.CreatedByStaffName.Should().BeNull();
    }
}
