using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Auth.Commands.Login;
using SigortaPro.Application.Features.Auth.DTOs;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.WebAPI.Tests.Integration;

// Admin teklif/poliçe ekranlarında müşteri kimliklendirme + telefonla arama (format bağımsız).
// EN KRİTİK: aynı isimli iki müşteri telefonla ayırt edilebilmeli; telefon araması yalnızca doğru müşteriyi getirmeli.
// E-posta tetiklenmez (host NullEmailService); auth HTTP bütçesi ISender ile korunur (ADR-034).
[Collection(IntegrationTestCollection.Name)]
public sealed class AdminCustomerIdentityIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public AdminCustomerIdentityIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task QuoteList_Search_ByPhone_Should_ReturnOnlyMatchingCustomer()
    {
        // Aynı isim, farklı telefon → telefon araması yalnızca doğru müşteriyi getirmeli.
        var lastName = UniqueLastName();
        var phoneA = RandomPhone();
        var phoneB = RandomPhone();
        await RegisterQuoteAsync("Ahmet", lastName, phoneA);
        await RegisterQuoteAsync("Ahmet", lastName, phoneB);

        var admin = await AdminClientAsync();

        // Ulusal biçimde (0…) ara → yalnızca A.
        var result = await SearchQuotesAsync(admin, ToNationalFormat(phoneA));

        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(quote => quote.CustomerPhone == phoneA,
            "telefon araması yalnızca o telefona sahip müşterinin tekliflerini getirmelidir");
        result.Items.Should().OnlyContain(quote => quote.CustomerFullName == $"Ahmet {lastName}");
    }

    [Fact]
    public async Task QuoteList_Search_ByName_Should_ReturnBothSameNameCustomers()
    {
        // İsim araması aynı isimli müşterilerin İKİSİNİ de bulmalı (telefonla ayırt edilir).
        var lastName = UniqueLastName();
        var phoneA = RandomPhone();
        var phoneB = RandomPhone();
        await RegisterQuoteAsync("Ahmet", lastName, phoneA);
        await RegisterQuoteAsync("Ahmet", lastName, phoneB);

        var admin = await AdminClientAsync();
        var result = await SearchQuotesAsync(admin, $"Ahmet {lastName}");

        result.Items.Select(quote => quote.CustomerPhone).Should()
            .Contain(new[] { phoneA, phoneB }, "isim araması her iki aynı isimli müşteriyi de getirmelidir");
    }

    [Fact]
    public async Task QuoteList_Search_PhoneFormatVariants_Should_MatchSameCustomer()
    {
        var lastName = UniqueLastName();
        var phone = RandomPhone(); // "+905XXXXXXXXX"
        await RegisterQuoteAsync("Ahmet", lastName, phone);

        var admin = await AdminClientAsync();

        var national = ToNationalFormat(phone);          // "05XXXXXXXXX"
        var spaced = FormatSpaced(national);              // "05XX XXX XX XX"
        var international = phone;                        // "+905XXXXXXXXX"

        foreach (var variant in new[] { national, spaced, international })
        {
            var result = await SearchQuotesAsync(admin, variant);
            result.Items.Should().Contain(quote => quote.CustomerPhone == phone,
                $"'{variant}' biçimindeki arama aynı müşteriyi bulmalıdır");
        }
    }

    [Fact]
    public async Task QuoteList_Should_ExposeCustomerNameAndPhone_ForAdmin()
    {
        var lastName = UniqueLastName();
        var phone = RandomPhone();
        await RegisterQuoteAsync("Ahmet", lastName, phone);

        var admin = await AdminClientAsync();
        var result = await SearchQuotesAsync(admin, $"Ahmet {lastName}");

        var item = result.Items.Should().ContainSingle().Subject;
        item.CustomerFullName.Should().Be($"Ahmet {lastName}");
        item.CustomerPhone.Should().Be(phone);
        item.CustomerId.Should().NotBeEmpty("stabil müşteri kimliği taşınmalıdır");
    }

    [Fact]
    public async Task QuoteList_Search_Should_NotLeakOtherCustomers_ForCustomerCaller()
    {
        // Müşteri yalnızca kendi tekliflerini arar → başka müşterinin telefonu boş sonuç döndürür (sızıntı yok).
        var lastName = UniqueLastName();
        var otherPhone = RandomPhone();
        await RegisterQuoteAsync("Ahmet", lastName, otherPhone);

        var (self, _) = await RegisterQuoteAsync("Mehmet", UniqueLastName(), RandomPhone());

        var result = await SearchQuotesAsync(self, ToNationalFormat(otherPhone));

        result.Items.Should().BeEmpty("müşteri başka bir müşterinin tekliflerini arayamaz/göremez");
    }

    [Fact]
    public async Task PolicyReport_Should_RejectNonStaff()
    {
        var (customer, _) = await RegisterQuoteAsync("Mehmet", UniqueLastName(), RandomPhone());

        var response = await customer.GetAsync(
            "/api/v1/dashboard/reports/policies?from=2026-01-01&to=2026-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "admin poliçe raporu yalnızca personele açıktır");
    }

    // --- Arrange yardımcıları ---

    private async Task<HttpClient> AdminClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var login = await sender.Send(new LoginCommand("admin@sigortapro.com", "Admin!2345"));
        login.IsSuccess.Should().BeTrue();
        return TestAccountFactory.CreateAuthorizedClient(_factory, login.Value!);
    }

    private async Task<(HttpClient Client, AuthResponse Session)> RegisterQuoteAsync(
        string firstName, string lastName, string phone)
    {
        var session = await TestAccountFactory.RegisterCustomerAsync(
            _factory, firstName: firstName, lastName: lastName, phoneNumber: phone);
        var client = TestAccountFactory.CreateAuthorizedClient(_factory, session);

        var vehicle = await AddVehicleAsync(client);
        var quoteResponse = await client.PostAsJsonAsync("/api/v1/quotes",
            new CreateQuoteCommand(InsuranceBranch.Kasko, vehicle.Id, null, CoveragePackage.Standart));
        quoteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        return (client, session);
    }

    private static async Task<VehicleDto> AddVehicleAsync(HttpClient client)
    {
        var command = new AddVehicleCommand(
            $"34 TS {Random.Shared.Next(1000, 9999)}", "Toyota", "Corolla", 2022, 132, VehicleUsage.Hususi);
        var response = await client.PostAsJsonAsync("/api/v1/customers/me/vehicles", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
    }

    private static async Task<PagedResult<QuoteSummaryDto>> SearchQuotesAsync(HttpClient client, string search)
    {
        var response = await client.GetAsync(
            $"/api/v1/quotes?search={Uri.EscapeDataString(search)}&pageSize=100");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<QuoteSummaryDto>>())!;
    }

    // Testler arası çakışmayı önlemek için benzersiz soyad / telefon.
    private static string UniqueLastName() => $"Yilmaz{Guid.NewGuid():N}"[..16];

    private static string RandomPhone() => $"+905{Random.Shared.NextInt64(100_000_000, 999_999_999)}";

    // "+905XXXXXXXXX" → "05XXXXXXXXX" (ulusal biçim).
    private static string ToNationalFormat(string canonical) => "0" + canonical[3..];

    // "05XXXXXXXXX" → "05XX XXX XX XX" (boşluklu biçim).
    private static string FormatSpaced(string national) =>
        $"{national[..4]} {national[4..7]} {national[7..9]} {national[9..]}";
}
