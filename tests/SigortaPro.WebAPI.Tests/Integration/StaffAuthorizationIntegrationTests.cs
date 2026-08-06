using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Features.Auth.Commands.RefreshToken;
using SigortaPro.Application.Features.Claims.Commands.ApproveClaim;
using SigortaPro.Application.Features.Claims.Commands.CreateClaim;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Application.Features.Staff.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// / (STAFF_ROLE_AUTHORIZATION_PLAN.md,,): Admin/Personel yetki ayrımının
// uçtan uca güvenlik doğrulaması. Test numaraları plandaki S1–S19 senaryolarına karşılık gelir.
// E-posta tetiklenmez (host NullEmailService); auth arrange ISender ile yapılır (HTTP rate-limit bütçesi korunur).
[Collection(IntegrationTestCollection.Name)]
public sealed class StaffAuthorizationIntegrationTests
{
    private const string SuccessCard = "4111111111111111";
    private const string NewStaffPassword = "Personel!2345";

    private readonly SigortaProWebApplicationFactory _factory;

    public StaffAuthorizationIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Staff API erişim kontrolü (S1–S5) ─────────────────────────────────────────────────────

    [Fact] // S1 + S2 + rol=Personel doğrulaması
    public async Task Admin_Should_ListAndCreateStaff_And_CreatedUserIsPersonel()
    {
        var admin = await TestAccountFactory.AdminClientAsync(_factory);

        var list = await admin.GetAsync("/api/v1/staff");
        list.StatusCode.Should().Be(HttpStatusCode.OK, "Admin personel listesine erişebilmelidir (S1)");

        var created = await CreateStaffAsync(admin, UniqueStaffEmail(), "Yeni Personel");
        created.Roles.Should().ContainSingle().Which.Should().Be("Personel",
            "oluşturulan hesabın rolü daima Personel olmalıdır (S2)");
        created.IsActive.Should().BeTrue();
    }

    [Fact] // S3
    public async Task Personel_Should_BeForbidden_OnAllStaffEndpoints()
    {
        var personel = await TestAccountFactory.StaffClientAsync(_factory);
        var someId = Guid.NewGuid();

        (await personel.GetAsync("/api/v1/staff")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await personel.GetAsync($"/api/v1/staff/{someId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await personel.PostAsJsonAsync("/api/v1/staff",
            new { email = UniqueStaffEmail(), fullName = "X Y", password = NewStaffPassword }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await personel.PutAsJsonAsync($"/api/v1/staff/{someId}", new { fullName = "X Y" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await personel.PatchAsync($"/api/v1/staff/{someId}/status",
            JsonBody(new { isActive = false }))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact] // S4
    public async Task Customer_Should_BeForbidden_OnStaffEndpoints()
    {
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, session);

        (await customer.GetAsync("/api/v1/staff")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await customer.PostAsJsonAsync("/api/v1/staff",
            new { email = UniqueStaffEmail(), fullName = "X Y", password = NewStaffPassword }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact] // S5
    public async Task Anonymous_Should_BeUnauthorized_OnStaffEndpoints()
    {
        var anonymous = _factory.CreateClient();

        (await anonymous.GetAsync("/api/v1/staff")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync("/api/v1/staff",
            new { email = UniqueStaffEmail(), fullName = "X Y", password = NewStaffPassword }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Fiyatlandırma erişimi (S6–S7) ─────────────────────────────────────────────────────────

    [Fact] // S6 + S7
    public async Task Personel_Should_ViewPricing_ButNotModify()
    {
        var personel = await TestAccountFactory.StaffClientAsync(_factory);

        // (güncellendi): Personel fiyatlandırmayı GÖRÜNTÜLER (salt-okunur).
        (await personel.GetAsync("/api/v1/pricing/versions"))
            .StatusCode.Should().Be(HttpStatusCode.OK, "Personel fiyat versiyonlarını görüntüleyebilir (S6)");

        // Ancak DEĞİŞTİREMEZ — taslak oluşturma/aktifleştirme yalnızca Admin'e açıktır (S7).
        (await personel.PostAsync("/api/v1/pricing/versions", content: null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "Personel fiyat değiştiremez (S7)");
        (await personel.PostAsync($"/api/v1/pricing/versions/{Guid.NewGuid()}/activate", content: null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "Personel tarife aktifleştiremez (S7)");
    }

    // ── Mass-assignment / privilege escalation (S8) ───────────────────────────────────────────

    [Fact] // S8
    public async Task CreateStaff_Should_IgnoreClientRole_And_AlwaysCreatePersonel()
    {
        var admin = await TestAccountFactory.AdminClientAsync(_factory);
        var email = UniqueStaffEmail();

        // İstemci gövdeye role/isActive enjekte etse bile bunlar DTO'da yoktur → yok sayılır.
        var payload = JsonBody(new { email, fullName = "Sızma Denemesi", password = NewStaffPassword, role = "Admin", isActive = false });
        var response = await admin.PostAsync("/api/v1/staff", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = (await response.Content.ReadFromJsonAsync<StaffDetailDto>())!;
        created.Roles.Should().ContainSingle().Which.Should().Be("Personel",
            "istemci role gönderse bile Admin oluşturulamaz (S8)");
        created.IsActive.Should().BeTrue("istemci isActive=false gönderse bile hesap aktif oluşturulur");
    }

    // ── Son-Admin invariant + IDOR (S9, S12) ──────────────────────────────────────────────────

    [Fact] // S9
    public async Task SetStatus_Should_Return404_When_TargetIsAdmin()
    {
        var adminSession = await TestAccountFactory.AdminSessionAsync(_factory);
        var admin = TestAccountFactory.CreateAuthorizedClient(_factory, adminSession);

        var response = await admin.PatchAsync(
            $"/api/v1/staff/{adminSession.UserId}/status", JsonBody(new { isActive = false }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "hiçbir Admin (son Admin dahil) pasifleştirilemez — hedef yalnızca Personel olabilir (S9)");
    }

    [Fact] // S12
    public async Task GetStaffById_Should_Return404_When_TargetIsCustomer()
    {
        var admin = await TestAccountFactory.AdminClientAsync(_factory);
        var customer = await TestAccountFactory.RegisterCustomerAsync(_factory);

        var response = await admin.GetAsync($"/api/v1/staff/{customer.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "Personel olmayan bir kimlik bu yüzeyden okunamaz — varlık sızdırma/IDOR yok (S12)");
    }

    [Fact] // Rol değiştirme yüzeyi yok
    public async Task RoleChange_Endpoint_Should_NotExist()
    {
        var admin = await TestAccountFactory.AdminClientAsync(_factory);
        var created = await CreateStaffAsync(admin, UniqueStaffEmail(), "Rol Test");

        var response = await admin.PatchAsync(
            $"/api/v1/staff/{created.Id}/role", JsonBody(new { role = "Admin" }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "rol değiştirme endpoint'i bilinçli olarak yoktur (privilege escalation yüzeyi kapalı)");
    }

    // ── Pasifleştirme: login + refresh reddi (S10, S11) ───────────────────────────────────────

    [Fact] // S10 + S11
    public async Task DeactivatedStaff_Should_NotLogin_And_NotRefresh()
    {
        var admin = await TestAccountFactory.AdminClientAsync(_factory);
        var email = UniqueStaffEmail();
        var created = await CreateStaffAsync(admin, email, "Ayrılan Personel");

        // Pasifleştirmeden ÖNCE giriş yap → geçerli refresh token elde et.
        var beforeSession = await TestAccountFactory.LoginAsync(_factory, email, NewStaffPassword);

        // Admin personeli pasifleştirir.
        (await admin.PatchAsync($"/api/v1/staff/{created.Id}/status", JsonBody(new { isActive = false })))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // S10: pasif personel artık login olamaz (genel mesaj — aktiflik sızdırılmaz).
        using (var scope = _factory.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var login = await sender.Send(new SigortaPro.Application.Features.Auth.Commands.Login.LoginCommand(email, NewStaffPassword));
            login.IsSuccess.Should().BeFalse("pasif personel giriş yapamaz (S10)");
        }

        // S11: pasifleştirmeden önce alınmış refresh token artık yenileyemez.
        using (var scope = _factory.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var refresh = await sender.Send(new RefreshTokenCommand(beforeSession.RefreshToken));
            refresh.IsSuccess.Should().BeFalse("pasif personel token yenileyemez (S11)");
        }
    }

    [Fact] // Aktif kullanıcı davranışı korunur (kontrol grubu)
    public async Task ActiveUsers_Should_LoginAndRefresh_Normally()
    {
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var refresh = await sender.Send(new RefreshTokenCommand(session.RefreshToken));
        refresh.IsSuccess.Should().BeTrue("aktif kullanıcının refresh davranışı korunur");
    }

    // ── Personel operasyon erişimi korunur (S13, S14, S19) ────────────────────────────────────

    [Fact] // S13 + S19
    public async Task Personel_Should_Access_OperationalSurfaces()
    {
        var personel = await TestAccountFactory.StaffClientAsync(_factory);

        (await personel.GetAsync("/api/v1/customers")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await personel.GetAsync("/api/v1/quotes")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await personel.GetAsync("/api/v1/claims")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await personel.GetAsync("/api/v1/dashboard/summary")).StatusCode.Should().Be(HttpStatusCode.OK,
            "Personel operasyonel dashboard özetini görebilir (S19)");
    }

    [Fact] // S14
    public async Task Personel_Should_BeForbidden_OnCustomerSurfaces()
    {
        var personel = await TestAccountFactory.StaffClientAsync(_factory);

        (await personel.GetAsync("/api/v1/customers/me")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await personel.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, Guid.NewGuid(), null, CoveragePackage.Standart)))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Finansal rapor ayrımı (S18) ───────────────────────────────────────────────────────────

    [Fact] // S18
    public async Task PaymentsReport_Should_BeForbiddenForPersonel_And_AllowedForAdmin()
    {
        var personel = await TestAccountFactory.StaffClientAsync(_factory);
        var admin = await TestAccountFactory.AdminClientAsync(_factory);

        (await personel.GetAsync("/api/v1/dashboard/reports/payments"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "Personel ödeme/ciro raporunu göremez (S18)");
        (await admin.GetAsync("/api/v1/dashboard/reports/payments"))
            .StatusCode.Should().Be(HttpStatusCode.OK, "Admin ödeme/ciro raporunu görebilir");
    }

    // ── Riskli müşteriler raporu Admin-only (S20 — P1 kararı D3) ───────────────────────────────

    [Fact] // S20
    public async Task RiskiestCustomersReport_Should_BeAdminOnly()
    {
        const string endpoint = "/api/v1/dashboard/reports/riskiest-customers";

        var anonymous = _factory.CreateClient();
        var customer = TestAccountFactory.CreateAuthorizedClient(
            _factory, await TestAccountFactory.RegisterCustomerAsync(_factory));
        var personel = await TestAccountFactory.StaffClientAsync(_factory);
        var admin = await TestAccountFactory.AdminClientAsync(_factory);

        (await anonymous.GetAsync(endpoint))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized, "anonim erişemez");
        (await customer.GetAsync(endpoint))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "müşteri erişemez");
        (await personel.GetAsync(endpoint))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "Personel riskli müşteriler raporunu göremez (S20 — D3)");
        (await admin.GetAsync(endpoint))
            .StatusCode.Should().Be(HttpStatusCode.OK, "Admin riskli müşteriler raporuna erişebilir (mevcut davranış)");
    }

    // ── Hasar: onay Personel'de, ödeme Admin-only (S15, S16, S17) ─────────────────────────────

    [Fact] // S17 + S15 + S16
    public async Task ClaimPayment_Should_BeAdminOnly_While_PersonelKeepsApproval()
    {
        // Arrange: müşteri + aktif poliçe + bildirilmiş hasar.
        var customerSession = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customer = TestAccountFactory.CreateAuthorizedClient(_factory, customerSession);
        var policy = await PurchaseActivePolicyAsync(customer);

        var createClaim = await customer.PostAsJsonAsync("/api/v1/claims",
            new CreateClaimCommand(policy.Id, DateTime.UtcNow, "Test hasar.", 5000m));
        createClaim.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = (await createClaim.Content.ReadFromJsonAsync<ClaimSummaryDto>())!;

        var personel = await TestAccountFactory.StaffClientAsync(_factory);

        // Personel incelemeye alır ve ONAYLAR (operasyon korunur).
        (await personel.PostAsync($"/api/v1/claims/{claim.Id}/start-review", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await personel.PostAsJsonAsync($"/api/v1/claims/{claim.Id}/approve",
            new ApproveClaimCommand(claim.Id, 4000m, "Uygundur")))
            .StatusCode.Should().Be(HttpStatusCode.OK, "Personel hasar onaylayabilir (S17)");

        // Personel ÖDEME yapamaz (S15) — para çıkışı Admin-only.
        (await personel.PostAsync($"/api/v1/claims/{claim.Id}/pay", null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "Personel hasar ödemesi yapamaz (S15)");

        // Admin ödeme yapabilir (S16).
        var admin = await TestAccountFactory.AdminClientAsync(_factory);
        (await admin.PostAsync($"/api/v1/claims/{claim.Id}/pay", null))
            .StatusCode.Should().Be(HttpStatusCode.OK, "Admin hasar ödemesi yapabilir (S16)");
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────────────────────────

    private static string UniqueStaffEmail() => $"personel-{Guid.NewGuid():N}@test.sigortapro.com";

    private static StringContent JsonBody(object value) =>
        new(System.Text.Json.JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static async Task<StaffDetailDto> CreateStaffAsync(HttpClient adminClient, string email, string fullName)
    {
        var response = await adminClient.PostAsJsonAsync("/api/v1/staff",
            new { email, fullName, password = NewStaffPassword });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<StaffDetailDto>())!;
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
