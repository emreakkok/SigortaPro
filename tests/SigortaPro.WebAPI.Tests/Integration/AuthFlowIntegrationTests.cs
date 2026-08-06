using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Features.Auth.Commands.Login;
using SigortaPro.Application.Features.Auth.Commands.RefreshToken;
using SigortaPro.Application.Features.Auth.DTOs;

namespace SigortaPro.WebAPI.Tests.Integration;

// Auth akışının uçtan uca entegrasyon testleri (gerçek pipeline: middleware → MediatR → Identity → EF).
// Not: Auth uçları IP başına 10 istek/dk rate limit'lidir; koleksiyondaki HTTP auth çağrısı sayısı
// bilinçli olarak bu bütçenin altında tutulur, arrange aşaması TestAccountFactory (ISender) ile yapılır.
[Collection(IntegrationTestCollection.Name)]
public sealed class AuthFlowIntegrationTests
{
    private readonly SigortaProWebApplicationFactory _factory;

    public AuthFlowIntegrationTests(SigortaProWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Should_ReturnCustomerSessionAndUsableToken_When_RequestIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = TestAccountFactory.CreateRegisterCommand();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<AuthResponse>();
        session.Should().NotBeNull();
        session!.Roles.Should().ContainSingle().Which.Should().Be(Roles.Customer);
        session.AccessToken.Should().NotBeNullOrWhiteSpace();
        session.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // Üretilen access token korumalı bir uçta gerçekten çalışmalı; yanıt ham TCKN sızdırmamalı.
        var authorizedClient = TestAccountFactory.CreateAuthorizedClient(_factory, session);
        var profileResponse = await authorizedClient.GetAsync("/api/v1/customers/me");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profileBody = await profileResponse.Content.ReadAsStringAsync();
        profileBody.Should().NotContain(command.Tckn, "profil yanıtı ham TCKN içermemelidir (maskeli döner)");
    }

    [Fact]
    public async Task Register_Should_Return409_When_TcknAlreadyRegistered()
    {
        // Arrange: aynı TCKN ile önceden kayıtlı bir müşteri.
        var tckn = TestAccountFactory.GenerateValidTckn();
        await TestAccountFactory.RegisterCustomerAsync(_factory, tckn: tckn);
        var client = _factory.CreateClient();

        // Act: farklı e-posta, aynı TCKN.
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            TestAccountFactory.CreateRegisterCommand(tckn: tckn));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_Should_Return400ProblemDetails_When_TcknIsInvalid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var command = TestAccountFactory.CreateRegisterCommand(tckn: "12345678901");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", command);

        // Assert: FluentValidation → ValidationBehavior → ExceptionHandlingMiddleware (RFC 7807).
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").GetProperty("Tckn")[0].GetString()
            .Should().Be("Geçerli bir TCKN giriniz.");
        problem.RootElement.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Login_Should_ReturnSession_When_CredentialsAreValid()
    {
        // Arrange
        var email = TestAccountFactory.UniqueEmail();
        var registered = await TestAccountFactory.RegisterCustomerAsync(_factory, email: email);
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginCommand(email, TestAccountFactory.DefaultPassword));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<AuthResponse>();
        session!.UserId.Should().Be(registered.UserId);
        session.Roles.Should().Contain(Roles.Customer);
        session.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_Should_Return401_When_PasswordIsIncorrect()
    {
        // Arrange
        var email = TestAccountFactory.UniqueEmail();
        await TestAccountFactory.RegisterCustomerAsync(_factory, email: email);
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginCommand(email, "Yanlis!2345"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_Should_RotateTokenAndRejectOldOne_When_TokenIsValid()
    {
        // Arrange
        var session = await TestAccountFactory.RegisterCustomerAsync(_factory);
        var client = _factory.CreateClient();

        // Act: geçerli refresh token ile yenile.
        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh-token",
            new RefreshTokenCommand(session.RefreshToken));

        // Assert: yeni token çifti üretilir (rotasyon).
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        rotated!.RefreshToken.Should().NotBe(session.RefreshToken);
        rotated.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Act + Assert: eski (revoke edilmiş) refresh token artık kullanılamaz.
        var reuseResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh-token",
            new RefreshTokenCommand(session.RefreshToken));
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_Should_Return401_When_TokenIsMissing()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/quotes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
