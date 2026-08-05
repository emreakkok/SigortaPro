using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Auth.Commands.Login;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Dashboard.DTOs;
using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Application.Features.Policies.DTOs;
using SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;
using SigortaPro.Application.Features.Pricing.Commands.UpdatePricingDraft;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Seed;

namespace SigortaPro.WebAPI.Tests.Integration;

// ADR-048: Fiyatlandırma yönetiminin uçtan uca doğrulaması. Yaşam döngüsü Taslak → Aktifleştir. EN KRİTİK
// GARANTİ: tarife değiştiğinde ESKİ teklif/poliçe primi DEĞİŞMEZ; yalnızca aktifleştirmeden SONRA oluşturulan
// teklifler yeni tarifeyi kullanır. Taslak, aktifleştirilene kadar canlı fiyatları etkilemez.
[Collection(IntegrationTestCollection.Name)]
public sealed class PricingManagementIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public PricingManagementIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var login = await sender.Send(new LoginCommand(IdentitySeeder.AdminEmail, IdentitySeeder.AdminPassword));
        login.IsSuccess.Should().BeTrue();
        return TestAccountFactory.CreateAuthorizedClient(_factory, login.Value!);
    }

    // Taslak oluşturur (aktif tarifeden seed) ve DTO'sunu döner. İsim zorunludur.
    private static async Task<PricingVersionDto> CreateDraftAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync(
            "/api/v1/pricing/versions", new CreatePricingVersionCommand("Test Tarifesi"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PricingVersionDto>())!;
    }

    // Taslağı, verilen dönüştürücüyle düzenleyip PUT eder. Tüm faktör grupları taslağın (seed edilmiş) DTO'sundan taşınır.
    private static UpdatePricingDraftCommand ToUpdate(PricingVersionDto draft) => new(
        draft.Id,
        draft.Name ?? "Test Tarifesi",
        draft.EffectiveFrom,
        draft.EffectiveTo,
        draft.Note,
        draft.Rates.Select(rate => new BranchRateInput(rate.Branch, rate.BasePremium)).ToList(),
        draft.RuleSet.PackagePremiumFactors.Select(f => new PackageFactorInput(f.Package, f.PremiumFactor)).ToList(),
        draft.RuleSet.CityRiskCoefficients.Select(c => new CityCoefficientInput(c.City, c.Coefficient)).ToList(),
        draft.RuleSet.DefaultCityRiskCoefficient,
        draft.RuleSet.RenewalDiscountFactor,
        draft.RuleSet.DriverAgeFactors,
        draft.RuleSet.VehicleAgeFactors,
        draft.RuleSet.EnginePowerFactors,
        draft.RuleSet.VehicleUsageFactors,
        draft.RuleSet.BonusMalusFactors,
        draft.RuleSet.BuildingAgeFactors,
        draft.RuleSet.SquareMetersFactors,
        draft.RuleSet.EarthquakeZoneFactors,
        draft.RuleSet.HealthAgeFactors,
        draft.RuleSet.SmokerSurcharge);

    // Bir taslak oluşturur, verilen güncellemeyi uygular ve AKTİFLEŞTİRİR (tarife yayınlar).
    private static async Task PublishAsync(HttpClient admin, Func<UpdatePricingDraftCommand, UpdatePricingDraftCommand> mutate)
    {
        var draft = await CreateDraftAsync(admin);
        var update = mutate(ToUpdate(draft));

        var put = await admin.PutAsJsonAsync($"/api/v1/pricing/versions/{draft.Id}", update);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var activate = await admin.PostAsync($"/api/v1/pricing/versions/{draft.Id}/activate", content: null);
        activate.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static UpdatePricingDraftCommand WithSaglikBasePremium(UpdatePricingDraftCommand command, decimal saglik) =>
        WithBasePremium(command, InsuranceBranch.Saglik, saglik);

    private static UpdatePricingDraftCommand WithBasePremium(
        UpdatePricingDraftCommand command, InsuranceBranch branch, decimal basePremium) =>
        command with
        {
            Rates = command.Rates
                .Select(rate => rate.Branch == branch ? rate with { BasePremium = basePremium } : rate)
                .ToList(),
        };

    private static async Task<(string Id, decimal Premium)> CreateHealthQuoteAsync(HttpClient customer)
    {
        var response = await customer.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart, IsSmoker: false));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var quote = (await response.Content.ReadFromJsonAsync<QuoteDto>())!;
        return (quote.Id.ToString(), quote.TotalPremium);
    }

    private static async Task<decimal> ReadPremiumAsync(HttpClient client, string quoteId) =>
        (await client.GetFromJsonAsync<QuoteDto>($"/api/v1/quotes/{quoteId}"))!.TotalPremium;

    [Fact]
    public async Task ActivatedTariff_Should_AffectOnlyNewQuotes_And_PreserveExistingQuotePremium()
    {
        var admin = await AdminClientAsync();
        var customerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, customerSession);

        // 1) Bilinen bir tarifeyi yayınla (aktifleştir) ve bu tarifeyle bir teklif oluştur.
        await PublishAsync(admin, command => WithSaglikBasePremium(command, 8000m));
        var existing = await CreateHealthQuoteAsync(customer);
        existing.Premium.Should().BeGreaterThan(0m);

        // 2) Baz primi belirgin artıran YENİ bir tarife yayınla.
        await PublishAsync(admin, command => WithSaglikBasePremium(command, 24000m));

        // 3) ESKİ teklif → primi DEĞİŞMEMELİ (sabitlediği versiyonla hesaplanır).
        (await ReadPremiumAsync(customer, existing.Id)).Should().Be(existing.Premium,
            "mevcut teklifin fiyatı sonraki tarife değişikliklerinden etkilenmemelidir");

        // 4) YENİ teklif → güncel (yükseltilmiş) tarifeyi kullanmalı.
        var fresh = await CreateHealthQuoteAsync(customer);
        fresh.Premium.Should().BeGreaterThan(existing.Premium,
            "aktifleştirmeden sonra oluşturulan teklif yeni tarifeyi kullanmalıdır");
    }

    [Fact]
    public async Task Draft_Should_NotAffectLivePricing_UntilActivated()
    {
        var admin = await AdminClientAsync();
        var customerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, customerSession);

        await PublishAsync(admin, command => WithSaglikBasePremium(command, 8000m));
        var before = (await CreateHealthQuoteAsync(customer)).Premium;

        // Taslak oluştur + baz primi 3 katına çıkar + KAYDET — ama AKTİFLEŞTİRME.
        var draft = await CreateDraftAsync(admin);
        var update = WithSaglikBasePremium(ToUpdate(draft), 24000m);
        (await admin.PutAsJsonAsync($"/api/v1/pricing/versions/{draft.Id}", update))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Taslak canlıyı etkilememeli → yeni teklif hâlâ ESKİ (aktif) tarifeyi kullanır.
        var afterDraft = (await CreateHealthQuoteAsync(customer)).Premium;
        afterDraft.Should().Be(before, "taslak aktifleştirilene kadar canlı fiyatlar değişmez");
    }

    [Fact]
    public async Task PackageFactorChange_Should_ApplyToNewQuotes_Only()
    {
        var admin = await AdminClientAsync();
        var customerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, customerSession);

        // Standart paket çarpanını 1.00 → 2.00 yapan tarifeyi yayınla (baz prim sabit).
        await PublishAsync(admin, command => command with
        {
            PackagePremiumFactors = command.PackagePremiumFactors
                .Select(f => f.Package == CoveragePackage.Standart ? f with { PremiumFactor = 2.00m } : f)
                .ToList(),
        });

        var doubled = (await CreateHealthQuoteAsync(customer)).Premium;

        // Çarpanı 1.00'a geri çeken YENİ tarife yayınla.
        await PublishAsync(admin, command => command with
        {
            PackagePremiumFactors = command.PackagePremiumFactors
                .Select(f => f.Package == CoveragePackage.Standart ? f with { PremiumFactor = 1.00m } : f)
                .ToList(),
        });

        var single = (await CreateHealthQuoteAsync(customer)).Premium;

        // Paket çarpanı versiyonlanmıştır → yeni teklif yaklaşık yarı prim (2.00 → 1.00) kullanmalı.
        single.Should().BeLessThan(doubled);
        (doubled / single).Should().BeApproximately(2m, 0.01m, "paket çarpanı 2.00'dan 1.00'a düştü");
    }

    [Fact]
    public async Task Personel_CanView_ButCannotModifyPricing()
    {
        var staff = await TestAccountFactory.StaffClientAsync(_factory);

        // Personel GÖRÜNTÜLEYEBİLİR (ADR: personel salt-okunur).
        (await staff.GetAsync("/api/v1/pricing/versions")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Ancak DEĞİŞTİREMEZ (taslak oluşturma/aktifleştirme yalnızca Admin).
        (await staff.PostAsync("/api/v1/pricing/versions", content: null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.PostAsync($"/api/v1/pricing/versions/{Guid.NewGuid()}/activate", content: null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PricingEndpoints_Should_Reject_CustomerAndAnonymous()
    {
        var customerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, customerSession);

        (await customer.GetAsync("/api/v1/pricing/versions")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await customer.PostAsync("/api/v1/pricing/versions", content: null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _factory.CreateClient().GetAsync("/api/v1/pricing/versions"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateDraft_Should_Reject_InvalidBasePremium()
    {
        var admin = await AdminClientAsync();
        var draft = await CreateDraftAsync(admin);
        var update = WithSaglikBasePremium(ToUpdate(draft), 0m);

        (await admin.PutAsJsonAsync($"/api/v1/pricing/versions/{draft.Id}", update))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ActuarialFactorChange_Should_ApplyToNewQuotes_Only()
    {
        var admin = await AdminClientAsync();
        var customerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, customerSession);

        // Sağlık yaş bandı faktörünü (index 2 = 31–45 yaş; test müşterisi ~36) baseline değerinde yayınla.
        await PublishAsync(admin, command => command); // seed = baseline (1.15)
        var existing = await CreateHealthQuoteAsync(customer);

        // Aynı bandın faktörünü İKİYE KATLAYAN yeni tarife yayınla (yalnızca yeni teklifleri etkiler).
        await PublishAsync(admin, command => command with
        {
            HealthAgeFactors = command.HealthAgeFactors
                .Select((value, index) => index == 2 ? value * 2m : value).ToList(),
        });

        // ESKİ teklif değişmez; YENİ teklif ~2× prim kullanır (yaş bandı faktörü versiyonlandı).
        (await ReadPremiumAsync(customer, existing.Id)).Should().Be(existing.Premium,
            "aktüeryal faktör değişikliği mevcut teklifi etkilemez (snapshot + pin)");
        var fresh = await CreateHealthQuoteAsync(customer);
        (fresh.Premium / existing.Premium).Should().BeApproximately(2m, 0.01m,
            "yeni teklif, sağlık yaş bandı faktörünün yeni değerini kullanmalıdır");
    }

    [Fact]
    public async Task CreateDraft_Should_Reject_EmptyName()
    {
        var admin = await AdminClientAsync();

        (await admin.PostAsJsonAsync("/api/v1/pricing/versions", new CreatePricingVersionCommand("")))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "taslak adı zorunludur");
    }

    [Fact]
    public async Task DiscardDraft_Should_RemoveDraft_AndAllowNewDraft()
    {
        var admin = await AdminClientAsync();
        var draft = await CreateDraftAsync(admin);

        // Taslak iptal edilir (soft-delete).
        (await admin.DeleteAsync($"/api/v1/pricing/versions/{draft.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Artık açık taslak yok → GET listesinde taslak görünmez ve YENİ taslak oluşturulabilir.
        var versions = await admin.GetFromJsonAsync<List<PricingVersionDto>>("/api/v1/pricing/versions");
        versions!.Should().NotContain(version => version.Id == draft.Id);

        (await admin.PostAsJsonAsync("/api/v1/pricing/versions", new CreatePricingVersionCommand("Yeni Taslak")))
            .StatusCode.Should().Be(HttpStatusCode.Created, "iptal sonrası yeni taslak oluşturulabilir");
    }

    // EN KRİTİK GARANTİ (uçtan uca): tarife değiştiğinde ESKİ POLİÇE'nin primi — poliçe detayı, PDF ve
    // dashboard raporu dahil — DEĞİŞMEZ; yalnızca yeni teklifler yeni tarifeyi kullanır.
    [Fact]
    public async Task TariffChange_Should_PreserveExistingPolicy_PDF_AndDashboardReport()
    {
        var admin = await AdminClientAsync();
        var customerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, customerSession);

        // Bilinen bir Kasko tarifesi yayınla ve bu tarifeyle bir poliçe satın al.
        await PublishAsync(admin, command => WithBasePremium(command, InsuranceBranch.Kasko, 15000m));
        var policyId = await PurchaseKaskoPolicyAsync(customer);

        var before = (await customer.GetFromJsonAsync<PolicyDetailDto>($"/api/v1/policies/{policyId}"))!.TotalPremium;
        before.Should().BeGreaterThan(0m);

        var pdfBefore = await customer.GetAsync($"/api/v1/policies/{policyId}/document");
        pdfBefore.StatusCode.Should().Be(HttpStatusCode.OK);

        var reportBefore = await AdminPolicyReportPremiumAsync(admin, policyId);
        reportBefore.Should().Be(before);

        // Kasko baz primini BELİRGİN artıran YENİ tarife yayınla (aktifleştir).
        await PublishAsync(admin, command => WithBasePremium(command, InsuranceBranch.Kasko, 90000m));

        // Poliçe primi DEĞİŞMEMELİ (poliçe detayı, PDF hâlâ üretilir, dashboard raporu aynı).
        (await customer.GetFromJsonAsync<PolicyDetailDto>($"/api/v1/policies/{policyId}"))!
            .TotalPremium.Should().Be(before, "mevcut poliçenin primi tarife değişikliğinden etkilenmez");
        (await customer.GetAsync($"/api/v1/policies/{policyId}/document"))
            .StatusCode.Should().Be(HttpStatusCode.OK, "poliçe PDF'i sabitlenmiş primle üretilmeye devam eder");
        (await AdminPolicyReportPremiumAsync(admin, policyId)).Should().Be(before,
            "dashboard poliçe raporu geçmiş primi korur (saklanan değer)");
    }

    private static async Task<Guid> PurchaseKaskoPolicyAsync(HttpClient customer)
    {
        var vehicleResponse = await customer.PostAsJsonAsync("/api/v1/customers/me/vehicles",
            new AddVehicleCommand($"34 PR {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132, VehicleUsage.Hususi));
        vehicleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var vehicle = (await vehicleResponse.Content.ReadFromJsonAsync<VehicleDto>())!;

        var quoteResponse = await customer.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, CoveragePackage.Standart));
        quoteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var quote = (await quoteResponse.Content.ReadFromJsonAsync<QuoteDto>())!;

        (await customer.PostAsync($"/api/v1/quotes/{quote.Id}/approve", content: null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var purchaseResponse = await customer.PostAsJsonAsync("/api/v1/payments",
            new PurchaseQuoteCommand(quote.Id, "4111111111111111", "Test Müşteri", "12", "2030", "123", InstallmentCount: 1));
        purchaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var purchase = (await purchaseResponse.Content.ReadFromJsonAsync<PurchaseResultDto>())!;
        return purchase.Policy.Id;
    }

    private static async Task<decimal> AdminPolicyReportPremiumAsync(HttpClient admin, Guid policyId)
    {
        var from = DateTime.UtcNow.AddDays(-1).ToString("O");
        var to = DateTime.UtcNow.AddDays(1).ToString("O");
        var report = await admin.GetFromJsonAsync<PagedResult<PolicyReportItemDto>>(
            $"/api/v1/dashboard/reports/policies?From={from}&To={to}&PageSize=100");
        return report!.Items.Single(item => item.Id == policyId).TotalPremium;
    }
}
