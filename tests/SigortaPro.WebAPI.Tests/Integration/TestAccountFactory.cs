using System.Net.Http.Headers;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Features.Auth.Commands.Register;
using SigortaPro.Application.Features.Auth.DTOs;

namespace SigortaPro.WebAPI.Tests.Integration;

// Entegrasyon testlerinin arrange aşaması: müşteri kaydı HTTP yerine doğrudan MediatR (ISender) üzerinden
// yapılır. Böylece (1) her test kendi verisini hazırlar (DEVELOPMENT_RULES.md §5.4), (2) auth uçlarındaki
// IP bazlı rate limit (10 istek/dk — ADR-020) yalnızca fiilen test edilen HTTP çağrılarına harcanır.
internal static class TestAccountFactory
{
    // Identity varsayılan şifre politikasıyla (büyük/küçük harf, rakam, özel karakter) uyumlu test şifresi.
    public const string DefaultPassword = "Test!2345";

    public static string UniqueEmail() => $"musteri-{Guid.NewGuid():N}@test.sigortapro.com";

    // TcknValidation.IsValid ile birebir aynı kontrol basamaklarını üreten rastgele geçerli TCKN.
    public static string GenerateValidTckn()
    {
        var digits = new int[11];
        digits[0] = Random.Shared.Next(1, 10);
        for (var i = 1; i < 9; i++)
        {
            digits[i] = Random.Shared.Next(0, 10);
        }

        var sumOdd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var sumEven = digits[1] + digits[3] + digits[5] + digits[7];

        var tenthDigit = ((sumOdd * 7) - sumEven) % 10;
        if (tenthDigit < 0)
        {
            tenthDigit += 10;
        }

        digits[9] = tenthDigit;
        digits[10] = digits.Take(10).Sum() % 10;

        return string.Concat(digits);
    }

    public static RegisterCommand CreateRegisterCommand(
        string? email = null,
        string? tckn = null,
        string password = DefaultPassword) =>
        new(
            email ?? UniqueEmail(),
            password,
            "Test",
            "Müşteri",
            tckn ?? GenerateValidTckn(),
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "+905321112233",
            "İstanbul",
            "Kadıköy",
            "Caferağa",
            "34710");

    /// <summary>Yeni bir müşteri kaydeder (Identity kullanıcısı + Customer profili) ve oturumunu döndürür.</summary>
    public static async Task<AuthResponse> RegisterCustomerAsync(
        SigortaProWebApplicationFactory factory,
        string? email = null,
        string? tckn = null,
        string password = DefaultPassword)
    {
        using var scope = factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(CreateRegisterCommand(email, tckn, password));

        result.IsSuccess.Should().BeTrue(
            "test kullanıcısı kaydı başarılı olmalıdır (hatalar: {0})",
            string.Join("; ", result.Errors));

        return result.Value!;
    }

    /// <summary>Verilen oturumun access token'ı ile yetkilendirilmiş bir HttpClient oluşturur.</summary>
    public static HttpClient CreateAuthorizedClient(SigortaProWebApplicationFactory factory, AuthResponse session)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }
}
