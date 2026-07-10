using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Auth.DTOs;

namespace SigortaPro.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            // Kullanıcının var olup olmadığını sızdırmamak için tek bir genel mesaj döner.
            _logger.LogWarning("Başarısız giriş denemesi. Email: {Email}", request.Email);
            return Result<AuthResponse>.Failure("E-posta veya şifre hatalı.");
        }

        var tokens = _tokenService.CreateTokenPair(user.Id, user.Email, user.Roles);
        await _refreshTokenService.StoreAsync(user.Id, tokens.RefreshToken, tokens.RefreshTokenExpiresAt, cancellationToken);

        _logger.LogInformation("Kullanıcı giriş yaptı. UserId: {UserId}", user.Id);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id,
            user.Email,
            user.Roles,
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt));
    }
}
