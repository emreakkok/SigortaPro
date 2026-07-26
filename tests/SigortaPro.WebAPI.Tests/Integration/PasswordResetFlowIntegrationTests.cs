using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Auth.Commands.ForgotPassword;
using SigortaPro.Application.Features.Auth.Commands.Login;
using SigortaPro.Application.Features.Auth.Commands.ResetPassword;

namespace SigortaPro.WebAPI.Tests.Integration;

// Task 23: Şifre sıfırlama akışının uçtan uca entegrasyon testleri (ADR-035). Gerçek pipeline'ı
// (middleware → MediatR → Identity DataProtector token provider → EF) doğrular; böylece
// AddDefaultTokenProviders kaydının fiilen çalıştığı kanıtlanır.
// Rate limit bütçesi (10 istek/dk — ADR-020): bu sınıf yalnızca 2 HTTP auth çağrısı ekler
// (arrange ISender ile). Koleksiyon toplamı 7 + 2 = 9 < 10 (ADR-034).
[Collection(IntegrationTestCollection.Name)]
public sealed class PasswordResetFlowIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public PasswordResetFlowIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgotPassword_Should_Return200GenericResponse_When_EmailIsRegistered()
    {
        // Arrange: kayıtlı bir müşteri (arrange ISender ile — rate limit bütçesi HTTP'ye harcanmaz).
        var email = TestAccountFactory.UniqueEmail();
        await TestAccountFactory.RegisterCustomerAsync(_factory, email: email);
        var client = _factory.CreateClient();

        // Act: Test host'unda IEmailService, no-op NullEmailService ile değiştirilmiştir (gerçek SMTP'ye
        // çıkılmaz — SigortaProWebApplicationFactory). Uç yine de generic başarı döner (kullanıcı varlığı sızdırılmaz).
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordCommand(email));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_Should_UpdatePassword_When_TokenIsValid()
    {
        // Arrange: müşteri kaydı + gerçek Identity reset token'ı (AddDefaultTokenProviders kanıtı) — HTTP'siz.
        var email = TestAccountFactory.UniqueEmail();
        await TestAccountFactory.RegisterCustomerAsync(_factory, email: email);

        string? resetToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            resetToken = await identityService.GeneratePasswordResetTokenAsync(email);
        }

        resetToken.Should().NotBeNullOrWhiteSpace("DataProtector token provider kayıtlı olmalı (AddDefaultTokenProviders)");

        const string newPassword = "Yeni!Sifre2345";
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new ResetPasswordCommand(email, resetToken!, newPassword));

        // Assert: sıfırlama başarılı.
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Yeni şifre çalışmalı, eski şifre çalışmamalı (arrange ISender ile doğrulanır — HTTP bütçesi harcanmaz).
        using var verifyScope = _factory.Services.CreateScope();
        var sender = verifyScope.ServiceProvider.GetRequiredService<ISender>();

        var loginWithNew = await sender.Send(new LoginCommand(email, newPassword));
        loginWithNew.IsSuccess.Should().BeTrue("yeni şifre ile giriş yapılabilmelidir");

        var loginWithOld = await sender.Send(new LoginCommand(email, TestAccountFactory.DefaultPassword));
        loginWithOld.IsFailure.Should().BeTrue("eski şifre artık geçersiz olmalıdır");
    }
}
