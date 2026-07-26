using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Features.Auth.Commands.Login;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.WebAPI.Tests.Integration;

// ADR-052: Operasyon dashboard'ı uçtan uca. Doğrulanan kritik davranışlar: yetki (yalnızca personel),
// tarih aralığının uygulanması, geçersiz aralığın reddi ve tüm blokların TEK çağrıda dönmesi.
// E-posta tetiklenmez (host NullEmailService); auth HTTP bütçesi ISender ile korunur (ADR-034).
[Collection(IntegrationTestCollection.Name)]
public sealed class DashboardSummaryIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public DashboardSummaryIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var login = await sender.Send(new LoginCommand("admin@sigortapro.com", "Admin!2345"));
        login.IsSuccess.Should().BeTrue();
        return TestAccountFactory.CreateAuthorizedClient(_factory, login.Value!);
    }

    [Fact]
    public async Task Summary_Should_ReturnAllBlocksInSingleCall_ForStaff()
    {
        var admin = await AdminClientAsync();

        var response = await admin.GetAsync("/api/v1/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();

        summary.Should().NotBeNull();
        // Tüm bloklar tek çağrıda gelir (blok başına ayrı uç yoktur).
        summary!.Current.Should().NotBeNull();
        summary.Previous.Should().NotBeNull();
        summary.Deltas.Should().NotBeNull();
        summary.Alerts.Should().NotBeNull();
        summary.Portfolio.Should().NotBeNull();
        summary.Funnel.Should().NotBeNull();
        summary.Claims.Should().NotBeNull();
        summary.PremiumSeries.Should().NotBeNull();
        summary.BranchPerformance.Should().NotBeNull();

        // Varsayılan aralık son 30 gündür.
        (summary.To - summary.From).TotalDays.Should().BeApproximately(30, 0.01);
        summary.Alerts.UpcomingRenewalWindowDays.Should().Be(30);
    }

    [Fact]
    public async Task Summary_Should_HonourRequestedRange_And_DeriveGranularity()
    {
        var admin = await AdminClientAsync();

        // Tek günlük aralık → saatlik kova (tek noktalı anlamsız grafik oluşmaz).
        var to = DateTime.UtcNow;
        var from = to.AddDays(-1);

        var response = await admin.GetAsync(
            $"/api/v1/dashboard/summary?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = (await response.Content.ReadFromJsonAsync<DashboardSummaryDto>())!;

        summary.Granularity.Should().Be(PremiumGranularityDto.Hourly);
        (summary.To - summary.From).TotalDays.Should().BeApproximately(1, 0.01);
    }

    [Fact]
    public async Task Summary_Should_Return400_When_RangeIsInverted()
    {
        var admin = await AdminClientAsync();

        var to = DateTime.UtcNow.AddDays(-10);
        var from = DateTime.UtcNow;

        var response = await admin.GetAsync(
            $"/api/v1/dashboard/summary?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Summary_Should_RejectNonStaff()
    {
        var customer = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var customerClient = TestAccountFactory.CreateAuthorizedClient(_factory, customer);

        var read = await customerClient.GetAsync("/api/v1/dashboard/summary");
        read.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var anonymous = _factory.CreateClient();
        (await anonymous.GetAsync("/api/v1/dashboard/summary"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // P1 kararı D1 (uçtan uca): /dashboard/summary tek uç kalır; Personel için agregat finansal alanlar
    // backend response'unda NULL döner (yalnızca frontend gizleme değil), operasyonel alanlar dolu gelir.
    [Fact]
    public async Task Summary_Should_MaskFinancialFieldsForPersonel_ButKeepOperationalAndFullForAdmin()
    {
        var personel = await TestAccountFactory.StaffClientAsync(_factory);
        var admin = await TestAccountFactory.AdminClientAsync(_factory);

        var personelResponse = await personel.GetAsync("/api/v1/dashboard/summary");
        personelResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Personel dashboard özetine erişebilir");
        var personelSummary = (await personelResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>())!;

        // Finansal alanların tamamı Personel response'unda null (veri taşımıyor).
        personelSummary.Current.PremiumProduction.Should().BeNull();
        personelSummary.Previous.PremiumProduction.Should().BeNull();
        personelSummary.Deltas.PremiumProduction.Should().BeNull();
        personelSummary.Alerts.FailedPayments.Should().BeNull();
        personelSummary.Portfolio.LifetimePremiumProduction.Should().BeNull();
        personelSummary.Portfolio.PaidClaimAmount.Should().BeNull();
        personelSummary.Portfolio.LossRatio.Should().BeNull();
        personelSummary.Claims.PaidAmount.Should().BeNull();
        personelSummary.Claims.EstimatedAmount.Should().BeNull();
        personelSummary.PremiumSeries.Should().OnlyContain(point => point.PremiumTotal == null);
        personelSummary.BranchPerformance.Should().OnlyContain(branch => branch.PremiumTotal == null);

        // Operasyonel alanlar Personel için dolu ve kullanılabilir (adetler, huni, portföy adetleri, hasar durumu).
        personelSummary.Funnel.Should().NotBeNull();
        personelSummary.Portfolio.ActivePolicyCount.Should().BeGreaterThanOrEqualTo(0);
        personelSummary.Portfolio.TotalCustomerCount.Should().BeGreaterThanOrEqualTo(0);
        personelSummary.Alerts.PendingClaims.Should().BeGreaterThanOrEqualTo(0);
        personelSummary.Claims.Submitted.Should().BeGreaterThanOrEqualTo(0);

        // Admin için finansal alanlar dolu (bit-aynı davranış) + operasyonel alanlar dolu.
        var adminResponse = await admin.GetAsync("/api/v1/dashboard/summary");
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminSummary = (await adminResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>())!;

        adminSummary.Current.PremiumProduction.Should().NotBeNull("Admin dönem prim üretimini görür");
        adminSummary.Portfolio.LifetimePremiumProduction.Should().NotBeNull("Admin portföy primini görür");
        adminSummary.Portfolio.PaidClaimAmount.Should().NotBeNull("Admin ödenen hasar toplamını görür");
        adminSummary.Claims.PaidAmount.Should().NotBeNull("Admin hasar tutar toplamını görür");
        adminSummary.Portfolio.ActivePolicyCount.Should().BeGreaterThanOrEqualTo(0);
    }

    // P1 kararı D2 (regresyon kilidi): poliçe raporu değişmedi; Personel erişimini korur (kayıt-başına prim).
    [Fact]
    public async Task PolicyReport_Should_RemainAccessibleByPersonel()
    {
        var personel = await TestAccountFactory.StaffClientAsync(_factory);

        var response = await personel.GetAsync(
            "/api/v1/dashboard/reports/policies?from=2026-01-01&to=2026-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Personel poliçe raporuna erişmeye devam eder — kayıt-başına prim operasyoneldir (D2)");
    }
}
