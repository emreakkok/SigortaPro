using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// ADR-041: Sağlıkta "başkası adına" teklif akışının uçtan uca doğrulaması (gerçek pipeline: JWT →
// validation → domain guard → fiyatlama → EF owned entity). /quotes auth rate-limit politikasına tabi
// değildir; arrange ISender ile yapılır (HTTP auth bütçesi harcanmaz — ADR-034).
[Collection(IntegrationTestCollection.Name)]
public sealed class InsuredPersonQuoteIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public InsuredPersonQuoteIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateHealthQuote_Should_PriceByInsuredAgeAndMaskTckn_When_OnBehalfOfSomeoneElse()
    {
        // Arrange: müşteri 1990 doğumlu (TestAccountFactory); sigortalı 1955 doğumlu → farklı yaş bandı.
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var client = TestAccountFactory.CreateAuthorizedClient(_factory, session);
        var insuredTckn = TestAccountFactory.GenerateValidTckn();

        // Önce kendisi için sağlık teklifi (kıyas primi).
        var selfResponse = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, IsSmoker: false));
        selfResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var selfBody = JsonDocument.Parse(await selfResponse.Content.ReadAsStringAsync());
        var selfPremium = selfBody.RootElement.GetProperty("totalPremium").GetDecimal();

        // Act: 1955 doğumlu anne adına teklif.
        var insured = new InsuredPersonInput(
            "Ayşe", "Yılmaz", insuredTckn,
            new DateTime(1955, 5, 1, 0, 0, 0, DateTimeKind.Utc), "+905321112233", "Anne");
        var response = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, insured, IsSmoker: false));

        // Assert: oluşturuldu, sigortalı özeti maskeli TCKN ile döner, prim sigortalının yaşından hesaplanır.
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain(insuredTckn, "yanıt ham sigortalı TCKN'si sızdırmamalıdır (maskeli döner)");

        using var body = JsonDocument.Parse(raw);
        var root = body.RootElement;
        root.GetProperty("insuredPerson").GetProperty("fullName").GetString().Should().Be("Ayşe Yılmaz");
        root.GetProperty("insuredPerson").GetProperty("relationship").GetString().Should().Be("Anne");

        var insuredPremium = root.GetProperty("totalPremium").GetDecimal();
        insuredPremium.Should().NotBe(selfPremium,
            "prim, poliçe sahibinin değil sigortalının yaşından hesaplanmalıdır (1990 vs 1955)");

        // Deterministik yeniden hesap (ADR-021): detay, saklanan sigortalı beyanıyla aynı primi üretir.
        var quoteId = root.GetProperty("id").GetString();
        var detailResponse = await client.GetAsync($"/api/v1/quotes/{quoteId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        detail.RootElement.GetProperty("totalPremium").GetDecimal().Should().Be(insuredPremium);
        detail.RootElement.GetProperty("insuredPerson").GetProperty("fullName").GetString().Should().Be("Ayşe Yılmaz");
    }

    [Fact]
    public async Task CreateQuote_Should_Return400_When_InsuredProvidedForVehicleBranch()
    {
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var client = TestAccountFactory.CreateAuthorizedClient(_factory, session);

        var insured = new InsuredPersonInput(
            "Ayşe", "Yılmaz", TestAccountFactory.GenerateValidTckn(),
            new DateTime(1955, 5, 1, 0, 0, 0, DateTimeKind.Utc), "+905321112233", "Anne");
        var response = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, Guid.NewGuid(), null, CoveragePackage.Standart, insured));

        // FluentValidation: sigortalı beyanı yalnızca Sağlık branşında girilebilir.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NotificationHub_Should_Return401_When_Unauthenticated()
    {
        // ADR-041: hub uç noktası map edilmiş ve kimlik doğrulaması zorunlu olmalıdır.
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/hubs/notifications/negotiate?negotiateVersion=1", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
