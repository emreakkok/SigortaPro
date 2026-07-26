using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.WebAPI.Tests.Integration;

/// <summary>
/// ADR-054 — REGRESYON KORUMASI: Yenileme fiyatlamasını besleyen hasar geçmişi sayımı **branşa göre**
/// kapsanmalıdır. Önceden müşterinin TÜM branşlardaki hasarları sayılıyordu; bu, bir <b>Kasko hasarının
/// Sağlık yenileme primini artırmasına</b> yol açıyordu (branşlar arası risk kirlenmesi).
/// <para>
/// Test, gerçek SQL sorgusunu (Claim → Policy → Quote.Branch join'i) çalıştırarak izolasyonu kanıtlar;
/// birisi filtreyi kaldırırsa kırılır.
/// </para>
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ClaimHistoryBranchIsolationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public ClaimHistoryBranchIsolationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CountReportableClaims_Should_OnlyCountClaimsOfRequestedBranch()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var claimRepository = scope.ServiceProvider.GetRequiredService<IClaimRepository>();

        var customerId = await SeedCustomerWithClaimsAsync(context);

        // Kasko'da 1 ödenmiş hasar, Sağlık'ta hiç hasar yok.
        var kaskoClaims = await claimRepository.CountReportableClaimsByCustomerAsync(
            customerId, InsuranceBranch.Kasko);
        var healthClaims = await claimRepository.CountReportableClaimsByCustomerAsync(
            customerId, InsuranceBranch.Saglik);

        kaskoClaims.Should().Be(1, "hasar kendi branşında sayılmalıdır");
        healthClaims.Should().Be(0,
            "Kasko hasarı Sağlık yenilemesinin primini ETKİLEMEMELİDİR (branşlar arası kirlenme yok)");
    }

    /// <summary>Aynı müşteriye ait bir Kasko poliçesi + ödenmiş Kasko hasarı ve bir Sağlık poliçesi kurar.</summary>
    private static async Task<Guid> SeedCustomerWithClaimsAsync(AppDbContext context)
    {
        var kaskoProduct = await context.InsuranceProducts
            .FirstAsync(product => product.Branch == InsuranceBranch.Kasko);
        var healthProduct = await context.InsuranceProducts
            .FirstAsync(product => product.Branch == InsuranceBranch.Saglik);

        var customer = new Customer(
            Guid.NewGuid(), "Branş", "İzolasyon", TestAccountFactory.GenerateValidTckn(),
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc), "+905321112233",
            new Address("İstanbul", "Kadıköy", "Caferağa", "34710"));
        context.Customers.Add(customer);

        var vehicle = new Vehicle(customer.Id, $"34 BR {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132);
        context.Vehicles.Add(vehicle);

        var now = DateTime.UtcNow;

        var kaskoPolicy = await AddPolicyAsync(
            context, customer, kaskoProduct, InsuranceBranch.Kasko, vehicle.Id, now, "POL-ISO-KASKO");
        await AddPolicyAsync(
            context, customer, healthProduct, InsuranceBranch.Saglik, null, now, "POL-ISO-SAGLIK");

        // Yalnızca KASKO poliçesine ödenmiş bir hasar bağlanır.
        var claim = new Claim(kaskoPolicy.Id, customer.Id, now.AddDays(-1), "Kasko hasarı.", 5000m);
        claim.StartReview();
        claim.Approve(4000m, "Onaylandı.");
        claim.MarkPaid();
        context.Claims.Add(claim);

        await context.SaveChangesAsync();
        return customer.Id;
    }

    private static async Task<Policy> AddPolicyAsync(
        AppDbContext context,
        Customer customer,
        InsuranceProduct product,
        InsuranceBranch branch,
        Guid? vehicleId,
        DateTime now,
        string policyNumberPrefix)
    {
        var quote = new Quote(customer.Id, product.Id, branch, vehicleId, null);
        quote.MarkAsPriced(10000m, now.AddDays(30));
        quote.Approve();
        quote.Purchase();
        context.Quotes.Add(quote);

        var policy = new Policy(
            $"{policyNumberPrefix}-{Random.Shared.Next(100000, 999999)}",
            customer.Id, quote.Id, now.AddYears(-1), now.AddDays(10), 10000m);
        context.Policies.Add(policy);

        await Task.CompletedTask;
        return policy;
    }
}
