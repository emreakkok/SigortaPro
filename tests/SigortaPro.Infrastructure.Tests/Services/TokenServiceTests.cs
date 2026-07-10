using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using SigortaPro.Infrastructure.Security;
using SigortaPro.Infrastructure.Services;

namespace SigortaPro.Infrastructure.Tests.Services;

public class TokenServiceTests
{
    private static readonly string[] CustomerRole = { "Customer" };

    private static TokenService CreateService() => new(new JwtSettings
    {
        Issuer = "SigortaPro",
        Audience = "SigortaProClient",
        SecretKey = "unit-test-only-super-secret-signing-key-1234567890",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
    });

    [Fact]
    public void CreateTokenPair_Should_EmbedUserIdEmailAndRoles_When_TokenGenerated()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var pair = service.CreateTokenPair(userId, "kullanici@ornek.com", CustomerRole);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(pair.AccessToken);
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == userId.ToString());
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Email && claim.Value == "kullanici@ornek.com");
        jwt.Claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == "Customer");
        jwt.Issuer.Should().Be("SigortaPro");
        jwt.Audiences.Should().Contain("SigortaProClient");
    }

    [Fact]
    public void CreateTokenPair_Should_SetAccessTokenExpiryAccordingToSettings()
    {
        var service = CreateService();

        var pair = service.CreateTokenPair(Guid.NewGuid(), "kullanici@ornek.com", CustomerRole);

        pair.AccessTokenExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
        pair.RefreshTokenExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void CreateTokenPair_Should_GenerateUniqueRefreshTokens_When_CalledMultipleTimes()
    {
        var service = CreateService();

        var first = service.CreateTokenPair(Guid.NewGuid(), "a@ornek.com", CustomerRole);
        var second = service.CreateTokenPair(Guid.NewGuid(), "b@ornek.com", CustomerRole);

        first.RefreshToken.Should().NotBeNullOrWhiteSpace();
        first.RefreshToken.Should().NotBe(second.RefreshToken);
    }
}
