using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Features.Customers.Commands.AddProperty;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.WebAPI.Tests.Integration;

/// <summary>
/// ADR-059: Hasar geçmişinin TEK ölçeği (Bonus-Malus basamağı) — uçtan uca.
/// <para>
/// Doğrulanan garantiler: yeni müşteri nötr, hasarsız dönemler indirim getirir, hasar ek prim getirir ve
/// sonraki hasarsız dönemlerde sönümlenir, Kasko ↔ Trafik basamakları birbirinden bağımsızdır,
/// Sağlık/Konut/DASK bu faktörü hiç görmez, Approved/Paid dışındaki hasarlar sayılmaz, önizleme ↔ teklif
/// paritesi korunur ve teklif oluştuktan sonra hasar geçmişi değişse bile eski teklif değişmez.
/// </para>
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class BonusMalusPricingIntegrationTests
{
    private const CoveragePackage Package = CoveragePackage.Standart;
    private const string FactorName = "Hasarsızlık Basamağı";

    private readonly SigortaProWebApplicationFactory _factory;

    public BonusMalusPricingIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NewCustomer_Should_BeNeutral_AndFactorHidden()
    {
        // Geçmişi olmayan müşteri: ×1.00 ve dökümde etkisiz kalem GÖSTERİLMEZ (eski kayıtlarla da uyumlu).
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);

        var (_, breakdown) = await CreateAndReadAsync(client, InsuranceBranch.Kasko, vehicle.Id);

        breakdown.Should().NotContain(line => line.StartsWith(FactorName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ClaimFreeHistory_Should_ProduceBonusDiscount()
    {
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);
        var customerId = await CustomerIdAsync(client);

        var neutralPremium = (await CreateAndReadAsync(client, InsuranceBranch.Kasko, vehicle.Id)).Premium;

        // Aynı branşta 3 hasarsız tamamlanmış dönem → +3 basamak → ×0.85
        await SeedCompletedPeriodsAsync(customerId, InsuranceBranch.Kasko, claimFreePeriods: 3);

        var (bonusPremium, breakdown) = await CreateAndReadAsync(client, InsuranceBranch.Kasko, vehicle.Id);

        breakdown.Should().Contain($"{FactorName}=0.85");
        bonusPremium.Should().BeLessThan(neutralPremium);
    }

    [Fact]
    public async Task Claim_Should_ProduceMalus_AndDecayWithLaterClaimFreePeriods()
    {
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);
        var customerId = await CustomerIdAsync(client);

        // 1 hasarlı dönem → 0 hasarsız dönem, 1 hasar → −2 basamak → ×1.40
        await SeedCompletedPeriodsAsync(customerId, InsuranceBranch.Kasko, claimFreePeriods: 0, claimPeriods: 1);
        var (malusPremium, malusBreakdown) = await CreateAndReadAsync(client, InsuranceBranch.Kasko, vehicle.Id);
        malusBreakdown.Should().Contain($"{FactorName}=1.40");

        // Sonrasında 4 hasarsız dönem → 4 − 2 = +2 basamak → ×0.90 (malus SÖNÜMLENDİ)
        await SeedCompletedPeriodsAsync(customerId, InsuranceBranch.Kasko, claimFreePeriods: 4);
        var (decayedPremium, decayedBreakdown) = await CreateAndReadAsync(client, InsuranceBranch.Kasko, vehicle.Id);

        decayedBreakdown.Should().Contain($"{FactorName}=0.90");
        decayedPremium.Should().BeLessThan(malusPremium, "hasarsız dönemler malusu geri kazandırmalıdır");
    }

    [Fact]
    public async Task KaskoClaims_Should_NotAffectTrafikStep_AndViceVersa()
    {
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);
        var customerId = await CustomerIdAsync(client);

        // Yalnızca KASKO'da hasarlı geçmiş oluştur.
        await SeedCompletedPeriodsAsync(customerId, InsuranceBranch.Kasko, claimFreePeriods: 0, claimPeriods: 1);

        var (_, kaskoBreakdown) = await CreateAndReadAsync(client, InsuranceBranch.Kasko, vehicle.Id);
        var (_, trafikBreakdown) = await CreateAndReadAsync(client, InsuranceBranch.Trafik, vehicle.Id);

        kaskoBreakdown.Should().Contain($"{FactorName}=1.40", "hasar kendi branşını etkilemelidir");
        trafikBreakdown.Should().NotContain(line => line.StartsWith(FactorName, StringComparison.Ordinal),
            "Kasko hasarı Trafik basamağını ETKİLEMEMELİDİR");
    }

    [Fact]
    public async Task NonReportableClaims_Should_NotAffectStep()
    {
        // Submitted/UnderReview/Rejected hasarlar basamağı etkilemez (yalnızca Approved/Paid sayılır).
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);
        var customerId = await CustomerIdAsync(client);

        await SeedCompletedPeriodsAsync(
            customerId, InsuranceBranch.Kasko, claimFreePeriods: 2, nonReportableClaimPeriods: 1);

        var (_, breakdown) = await CreateAndReadAsync(client, InsuranceBranch.Kasko, vehicle.Id);

        // 2 hasarsız + 1 dönem (hasarı raporlanabilir DEĞİL → o dönem de hasarsız sayılır) = +3 → 0.85
        breakdown.Should().Contain($"{FactorName}=0.85");
    }

    [Fact]
    public async Task UnrelatedBranches_Should_NeverCarryBonusMalusFactor()
    {
        var client = await CustomerClientAsync();
        var customerId = await CustomerIdAsync(client);
        await SeedCompletedPeriodsAsync(customerId, InsuranceBranch.Kasko, claimFreePeriods: 5);

        var healthId = await CreateQuoteIdAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, Package, IsSmoker: false));
        var property = await AddPropertyAsync(client);
        var propertyId = await CreateQuoteIdAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Konut, null, property.Id, Package));

        var (_, healthBreakdown) = await ReadPricingAsync(client, healthId);
        var (_, propertyBreakdown) = await ReadPricingAsync(client, propertyId);

        healthBreakdown.Should().NotContain(line => line.StartsWith(FactorName, StringComparison.Ordinal));
        propertyBreakdown.Should().NotContain(line => line.StartsWith(FactorName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Preview_And_CreatedQuote_Should_HaveIdenticalPremium_WithBonusMalus()
    {
        // ADR-056 paritesi yeni faktörle de korunur (ortak QuotePricingInputBuilder).
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);
        var customerId = await CustomerIdAsync(client);
        await SeedCompletedPeriodsAsync(customerId, InsuranceBranch.Kasko, claimFreePeriods: 3);

        var previewResponse = await client.GetAsync(
            $"/api/v1/quotes/compare?branch={(int)InsuranceBranch.Kasko}&vehicleId={vehicle.Id}");
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var previewDocument = JsonDocument.Parse(await previewResponse.Content.ReadAsStringAsync());
        var preview = previewDocument.RootElement.GetProperty("packages").EnumerateArray()
            .Single(item => item.GetProperty("coveragePackage").GetInt32() == (int)Package)
            .GetProperty("totalPremium").GetDecimal();

        var (created, _) = await CreateAndReadAsync(client, InsuranceBranch.Kasko, vehicle.Id);

        created.Should().Be(preview);
    }

    [Fact]
    public async Task ExistingQuote_Should_NotChange_WhenClaimHistoryChangesAfterwards()
    {
        // ADR-053 determinizmi: basamak teklif anında snapshot'lanır.
        var client = await CustomerClientAsync();
        var vehicle = await AddVehicleAsync(client);
        var customerId = await CustomerIdAsync(client);
        await SeedCompletedPeriodsAsync(customerId, InsuranceBranch.Kasko, claimFreePeriods: 3);

        var quoteId = await CreateQuoteIdAsync(client,
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, Package));
        var (premiumBefore, breakdownBefore) = await ReadPricingAsync(client, quoteId);

        // Teklif oluştuktan SONRA hasar geçmişi kötüleşir.
        await SeedCompletedPeriodsAsync(customerId, InsuranceBranch.Kasko, claimFreePeriods: 0, claimPeriods: 3);

        var (premiumAfter, breakdownAfter) = await ReadPricingAsync(client, quoteId);

        premiumAfter.Should().Be(premiumBefore, "eski teklifin primi hasar geçmişi değişince değişmemelidir");
        breakdownAfter.Should().Equal(breakdownBefore, "prim dökümü de dondurulmuş basamaktan üretilir");

        // Yeni teklif ise güncel (kötüleşmiş) basamağı kullanır.
        var (newPremium, _) = await CreateAndReadAsync(client, InsuranceBranch.Kasko, vehicle.Id);
        newPremium.Should().BeGreaterThan(premiumBefore);
    }

    // --- Yardımcılar ---

    private async Task<HttpClient> CustomerClientAsync()
    {
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        return TestAccountFactory.CreateAuthorizedClient(_factory, session);
    }

    private static async Task<Guid> CustomerIdAsync(HttpClient client)
    {
        var profile = await client.GetFromJsonAsync<CustomerDto>("/api/v1/customers/me");
        return profile!.Id;
    }

    private static async Task<VehicleDto> AddVehicleAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customers/me/vehicles",
            new AddVehicleCommand($"34 BM {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132,
                VehicleUsage.Hususi));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
    }

    private static async Task<PropertyDto> AddPropertyAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/customers/me/properties",
            new AddPropertyCommand("İstanbul", "Kadıköy", "Caferağa", "34710", 10, 120));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PropertyDto>())!;
    }

    /// <summary>Geçmiş dönemleri (tamamlanmış poliçeler) ve isteğe bağlı hasarlarını doğrudan DB'ye kurar.</summary>
    private async Task SeedCompletedPeriodsAsync(
        Guid customerId,
        InsuranceBranch branch,
        int claimFreePeriods = 0,
        int claimPeriods = 0,
        int nonReportableClaimPeriods = 0)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = await context.InsuranceProducts.FirstAsync(item => item.Branch == branch);
        var now = DateTime.UtcNow;

        for (var index = 0; index < claimFreePeriods + claimPeriods + nonReportableClaimPeriods; index++)
        {
            var policy = await AddCompletedPolicyAsync(context, customerId, product, branch, now, index);

            if (index >= claimFreePeriods && index < claimFreePeriods + claimPeriods)
            {
                // Raporlanabilir hasar (Paid) → basamağı düşürür.
                var claim = new Claim(policy.Id, customerId, now.AddYears(-2), "Dönem hasarı.", 5000m);
                claim.StartReview();
                claim.Approve(4000m, "Onaylandı.");
                claim.MarkPaid();
                context.Claims.Add(claim);
            }
            else if (index >= claimFreePeriods + claimPeriods)
            {
                // Raporlanabilir OLMAYAN hasar (Submitted) → basamağı etkilemez.
                context.Claims.Add(new Claim(policy.Id, customerId, now.AddYears(-2), "İncelenmemiş hasar.", 5000m));
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task<Policy> AddCompletedPolicyAsync(
        AppDbContext context, Guid customerId, InsuranceProduct product, InsuranceBranch branch,
        DateTime now, int index)
    {
        // Araç branşı teklifte risk objesi zorunlu kıldığından geçmiş dönem için bir araç kaydı üretilir.
        var vehicle = new Vehicle(customerId, $"34 GE {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla",
            2015, 100, VehicleUsage.Hususi);
        context.Vehicles.Add(vehicle);

        var quote = new Quote(customerId, product.Id, branch, vehicle.Id, null);
        quote.MarkAsPriced(10000m, now.AddYears(-3));
        quote.Approve();
        quote.Purchase();
        context.Quotes.Add(quote);

        // Dönemi BİTMİŞ poliçe (EndDate geçmişte) → "tamamlanmış dönem" sayılır.
        var policy = new Policy(
            $"POL-BM-{Random.Shared.Next(100000, 999999)}-{index}",
            customerId, quote.Id, now.AddYears(-4), now.AddYears(-3), 10000m);
        context.Policies.Add(policy);

        await Task.CompletedTask;
        return policy;
    }

    private static async Task<string> CreateQuoteIdAsync(HttpClient client, CreateQuoteCommand command)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task<(decimal Premium, List<string> Breakdown)> CreateAndReadAsync(
        HttpClient client, InsuranceBranch branch, Guid vehicleId)
    {
        var quoteId = await CreateQuoteIdAsync(client,
            new CreateQuoteCommand(branch, vehicleId, null, Package));
        return await ReadPricingAsync(client, quoteId);
    }

    private static async Task<(decimal Premium, List<string> Breakdown)> ReadPricingAsync(
        HttpClient client, string quoteId)
    {
        var response = await client.GetAsync($"/api/v1/quotes/{quoteId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        var breakdown = root.GetProperty("premiumBreakdown").EnumerateArray()
            .Select(item =>
                $"{item.GetProperty("factor").GetString()}={item.GetProperty("multiplier").GetDecimal().ToString(CultureInfo.InvariantCulture)}")
            .ToList();

        return (root.GetProperty("totalPremium").GetDecimal(), breakdown);
    }
}
