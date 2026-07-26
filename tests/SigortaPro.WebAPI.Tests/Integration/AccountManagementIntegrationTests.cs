using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Auth.Commands.Login;

namespace SigortaPro.WebAPI.Tests.Integration;

// ADR-040: Şifre değiştirme (Identity katmanı) ve genişletilmiş müşteri araması (e-posta + normalize telefon)
// entegrasyon doğrulaması. Rate limit bütçesi (10 istek/dk — ADR-020/ADR-034) korunur: şifre değiştirme,
// HTTP auth ucu yerine IIdentityService + ISender ile doğrulanır (controller köprüsü ince ve birim testlidir);
// müşteri arama ucu (/customers) auth politikasına tabi değildir.
[Collection(IntegrationTestCollection.Name)]
public sealed class AccountManagementIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public AccountManagementIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChangePassword_Should_UpdatePassword_When_CurrentPasswordIsCorrect()
    {
        // Arrange: kayıtlı müşteri (ISender — HTTP auth bütçesi harcanmaz).
        var email = TestAccountFactory.UniqueEmail();
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory, email: email);
        const string newPassword = "Yepyeni!Sifre1";

        using var scope = _factory.Services.CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act: yanlış mevcut şifre reddedilir, doğru mevcut şifre değişimi tamamlar.
        var wrongAttempt = await identityService.ChangePasswordAsync(session.UserId, "Yanlis!2345", newPassword);
        var succeeded = await identityService.ChangePasswordAsync(session.UserId, TestAccountFactory.DefaultPassword, newPassword);

        // Assert
        wrongAttempt.Should().BeFalse("yanlış mevcut şifre kabul edilmemelidir");
        succeeded.Should().BeTrue();

        var loginWithNew = await sender.Send(new LoginCommand(email, newPassword));
        loginWithNew.IsSuccess.Should().BeTrue("yeni şifre ile giriş yapılabilmelidir");

        var loginWithOld = await sender.Send(new LoginCommand(email, TestAccountFactory.DefaultPassword));
        loginWithOld.IsFailure.Should().BeTrue("eski şifre artık geçersiz olmalıdır");
    }

    [Fact]
    public async Task CustomerSearch_Should_MatchEmailAndNormalizedPhone_When_StaffSearches()
    {
        // Arrange: benzersiz e-postalı müşteri + admin oturumu (login ISender ile — HTTP bütçesi harcanmaz).
        var email = TestAccountFactory.UniqueEmail();
        await TestAccountFactory.RegisterCustomerAsync(_factory, email: email);

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var adminLogin = await sender.Send(new LoginCommand("admin@sigortapro.com", "Admin!2345"));
        adminLogin.IsSuccess.Should().BeTrue("seed admin ile giriş yapılabilmelidir");
        var client = TestAccountFactory.CreateAuthorizedClient(_factory, adminLogin.Value!);

        // Act + Assert: e-posta araması tam olarak bu müşteriyi bulur.
        var byEmail = await client.GetAsync($"/api/v1/customers?searchTerm={Uri.EscapeDataString(email)}");
        byEmail.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var body = JsonDocument.Parse(await byEmail.Content.ReadAsStringAsync()))
        {
            body.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        }

        // Act + Assert: telefon araması boşluk/parantez/tire ve baştaki 0'dan etkilenmez
        // (test müşterileri +905321112233 ile kayıtlıdır — normalize eşleşme).
        var byPhone = await client.GetAsync($"/api/v1/customers?searchTerm={Uri.EscapeDataString("(0532) 111-22 33")}");
        byPhone.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var body = JsonDocument.Parse(await byPhone.Content.ReadAsStringAsync()))
        {
            body.RootElement.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);
        }
    }
}
