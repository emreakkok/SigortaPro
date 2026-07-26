using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Features.Auth.Commands.Login;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// ADR-042: Kalıcı bildirim merkezi uçtan uca — iş olayı (teklif) → behavior → dispatcher fan-out →
// DB kaydı → liste/rozet/okundu akışı gerçek pipeline'da doğrulanır. E-posta tetiklenmez (bildirim
// kataloğunda e-posta kanalı yoktur; host NullEmailService kullanır). Auth HTTP bütçesi harcanmaz
// (register/login ISender ile — ADR-034).
[Collection(IntegrationTestCollection.Name)]
public sealed class NotificationCenterIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public NotificationCenterIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotificationFlow_Should_PersistListAndMarkRead_When_BusinessEventOccurs()
    {
        // Arrange: müşteri sağlık teklifi oluşturur (HTTP → behavior → dispatcher → staff fan-out).
        var customer = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customerClient = TestAccountFactory.CreateAuthorizedClient(_factory, customer);
        var quoteResponse = await customerClient.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, IsSmoker: false));
        quoteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Admin oturumu (ISender — HTTP auth bütçesine dokunmaz).
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var adminLogin = await sender.Send(new LoginCommand("admin@sigortapro.com", "Admin!2345"));
        adminLogin.IsSuccess.Should().BeTrue();
        var adminClient = TestAccountFactory.CreateAuthorizedClient(_factory, adminLogin.Value!);

        // Act + Assert 1: kalıcı bildirim admin'in listesinde görünür (DB'den okunur).
        var listResponse = await adminClient.GetAsync("/api/v1/notifications?pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var items = list.RootElement.GetProperty("items").EnumerateArray().ToList();
        items.Should().NotBeEmpty("iş olayı staff için kalıcı bildirim üretmelidir");
        var quoteNotification = items.FirstOrDefault(item =>
            item.GetProperty("type").GetString() == "quote-created"
            && !item.GetProperty("isRead").GetBoolean());
        quoteNotification.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        var notificationId = quoteNotification.GetProperty("id").GetString();

        // Assert 1b (ADR-047): bildirim tek başına "kim / kimin için / ne / hangi kayıt" sorularını
        // cevaplamalı ve ilgili kayda gidilebilmelidir. Hassas veri (TCKN) ASLA taşınmamalıdır.
        var message = quoteNotification.GetProperty("message").GetString();
        message.Should().Contain("Test Müşteri", "bildirim ilgili müşteriyi adıyla belirtmelidir");
        message.Should().Contain("Sağlık", "bildirim hangi ürün/branş olduğunu belirtmelidir");
        quoteNotification.GetProperty("actorName").GetString()
            .Should().Be("Test Müşteri", "işlemi yapan kişi snapshot olarak saklanmalıdır");
        quoteNotification.GetProperty("relatedEntityType").GetString().Should().Be("Quote");
        quoteNotification.GetProperty("relatedEntityId").GetString()
            .Should().NotBeNullOrEmpty("tıkla-git navigasyonu için ilgili kayıt kimliği gereklidir");
        message.Should().NotContain(customer.Email, "e-posta gibi kişisel veri bildirime yazılmaz");

        // Assert 2: okunmamış sayaç > 0.
        var unreadBefore = await adminClient.GetFromJsonAsync<int>("/api/v1/notifications/unread-count");
        unreadBefore.Should().BeGreaterThan(0);

        // Act + Assert 3: tek bildirimi okundu işaretle → filtreli listede okunmuş görünür.
        var markResponse = await adminClient.PostAsync($"/api/v1/notifications/{notificationId}/read", null);
        markResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act + Assert 4: tümünü okundu işaretle → sayaç 0.
        var markAllResponse = await adminClient.PostAsync("/api/v1/notifications/read-all", null);
        markAllResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var unreadAfter = await adminClient.GetFromJsonAsync<int>("/api/v1/notifications/unread-count");
        unreadAfter.Should().Be(0);

        // Assert 5: sahiplik — müşteri, admin'in bildirimini okundu işaretleyemez (403).
        var foreignMark = await customerClient.PostAsync($"/api/v1/notifications/{notificationId}/read", null);
        foreignMark.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
