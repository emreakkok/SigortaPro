using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Features.Auth.Commands.Login;
using SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// ADR-048: Fiyatlandırma yönetiminin uçtan uca doğrulaması. En kritik garanti:
// tarife değiştiğinde ESKİ teklifin primi DEĞİŞMEZ, YENİ teklif yeni tarifeyi kullanır.
// E-posta tetiklenmez (host NullEmailService kullanır); auth HTTP bütçesi ISender ile korunur (ADR-034).
[Collection(IntegrationTestCollection.Name)]
public sealed class PricingManagementIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public PricingManagementIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreatePricingVersionCommand TariffWith(decimal saglikBasePremium, DateTime effectiveFrom) =>
        new(
            effectiveFrom,
            "Test tarifesi",
            Enum.GetValues<InsuranceBranch>()
                .Select(branch => new BranchRateInput(
                    branch, branch == InsuranceBranch.Saglik ? saglikBasePremium : 5000m))
                .ToList());

    private async Task<HttpClient> AdminClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var login = await sender.Send(new LoginCommand("admin@sigortapro.com", "Admin!2345"));
        login.IsSuccess.Should().BeTrue();
        return TestAccountFactory.CreateAuthorizedClient(_factory, login.Value!);
    }

    private static async Task<decimal> CreateHealthQuotePremiumAsync(HttpClient customerClient)
    {
        var response = await customerClient.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, IsSmoker: false));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("totalPremium").GetDecimal();
    }

    private static async Task<decimal> ReadQuotePremiumAsync(HttpClient client, string quoteId)
    {
        var response = await client.GetAsync($"/api/v1/quotes/{quoteId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("totalPremium").GetDecimal();
    }

    private static async Task<string> CreateHealthQuoteIdAsync(HttpClient customerClient)
    {
        var response = await customerClient.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, IsSmoker: false));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task PricingChange_Should_AffectOnlyNewQuotes_And_PreserveExistingQuotePremium()
    {
        var admin = await AdminClientAsync();
        var customer = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customerClient = TestAccountFactory.CreateAuthorizedClient(_factory, customer);

        // 1) Tarifeyi bilinen bir değere sabitle ve bu tarifeyle bir teklif oluştur.
        var firstTariff = await admin.PostAsJsonAsync("/api/v1/pricing/versions",
            TariffWith(8000m, DateTime.UtcNow));
        firstTariff.StatusCode.Should().Be(HttpStatusCode.Created);

        var existingQuoteId = await CreateHealthQuoteIdAsync(customerClient);
        var premiumBeforeChange = await ReadQuotePremiumAsync(customerClient, existingQuoteId);
        premiumBeforeChange.Should().BeGreaterThan(0m);

        // 2) Admin baz primi belirgin şekilde artırır (yeni versiyon — eski versiyon değişmez).
        var secondTariff = await admin.PostAsJsonAsync("/api/v1/pricing/versions",
            TariffWith(24000m, DateTime.UtcNow));
        secondTariff.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3) ESKİ teklif tekrar görüntülenir → primi DEĞİŞMEMELİDİR (sabitlenen versiyonla hesaplanır).
        var premiumAfterChange = await ReadQuotePremiumAsync(customerClient, existingQuoteId);
        premiumAfterChange.Should().Be(premiumBeforeChange,
            "mevcut teklifin fiyatı, sonraki tarife değişikliklerinden etkilenmemelidir");

        // 4) YENİ teklif → güncel (yükseltilmiş) tarifeyi kullanmalıdır.
        var newQuotePremium = await CreateHealthQuotePremiumAsync(customerClient);
        newQuotePremium.Should().BeGreaterThan(premiumBeforeChange,
            "değişiklikten sonra oluşturulan teklif yeni tarifeyi kullanmalıdır");
    }

    [Fact]
    public async Task GetVersions_Should_ExposeHistoryWithCurrentFlag()
    {
        var admin = await AdminClientAsync();
        await admin.PostAsJsonAsync("/api/v1/pricing/versions", TariffWith(9500m, DateTime.UtcNow));

        var response = await admin.GetAsync("/api/v1/pricing/versions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var versions = document.RootElement.EnumerateArray().ToList();
        versions.Should().NotBeEmpty();
        versions.Should().ContainSingle(version => version.GetProperty("isCurrent").GetBoolean());
        versions[0].GetProperty("rates").EnumerateArray().Should()
            .HaveCount(Enum.GetValues<InsuranceBranch>().Length, "kısmi tarife yayınlanamaz");
    }

    [Fact]
    public async Task CreateVersion_Should_Reject_BackdatedEffectiveFrom()
    {
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/v1/pricing/versions",
            TariffWith(9000m, DateTime.UtcNow.AddDays(-3)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "geçmişe tarihleme, geçmiş teklifleri etkileme yanılgısı doğurur");
    }

    [Fact]
    public async Task CreateVersion_Should_Reject_InvalidBasePremium()
    {
        var admin = await AdminClientAsync();
        var rates = Enum.GetValues<InsuranceBranch>()
            .Select(branch => new BranchRateInput(branch, branch == InsuranceBranch.Kasko ? 0m : 5000m))
            .ToList();

        var response = await admin.PostAsJsonAsync("/api/v1/pricing/versions",
            new CreatePricingVersionCommand(DateTime.UtcNow, null, rates));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PricingEndpoints_Should_Reject_NonAdminUsers()
    {
        // Yetkilendirme: müşteri fiyatlandırmayı ne görebilir ne değiştirebilir.
        var customer = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customerClient = TestAccountFactory.CreateAuthorizedClient(_factory, customer);

        var read = await customerClient.GetAsync("/api/v1/pricing/versions");
        read.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var write = await customerClient.PostAsJsonAsync("/api/v1/pricing/versions",
            TariffWith(1m, DateTime.UtcNow));
        write.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Kimliksiz erişim de reddedilir.
        var anonymous = _factory.CreateClient();
        var anonymousRead = await anonymous.GetAsync("/api/v1/pricing/versions");
        anonymousRead.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
