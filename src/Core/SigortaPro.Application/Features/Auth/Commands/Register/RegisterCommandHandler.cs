using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Auth.DTOs;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<RegisterCommandHandler> logger)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _identityService.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result<AuthResponse>.Failure("Bu e-posta adresi ile daha önce bir hesap oluşturulmuş.");
        }

        // TCKN benzersizliği transaction öncesi ön-kontrol edilir (e-posta ile aynı desen); aksi halde
        // UQ_Customers_TCKN ihlali DbUpdateException olarak 500'e düşerdi. transaction akışı değişmez.
        if (await _customerRepository.ExistsByTcknAsync(request.Tckn, cancellationToken))
        {
            return Result<AuthResponse>.Failure("Bu TCKN ile daha önce bir kayıt oluşturulmuş.");
        }

        var userId = Guid.Empty;

        // Identity kullanıcısı ve Domain Customer kaydı atomik oluşturulmalı; biri başarısız olursa
        // diğeri de geri alınır.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            userId = await _identityService.CreateUserAsync(request.Email, request.Password, Roles.Customer, cancellationToken);

            var address = new Address(request.City, request.District, request.Neighborhood, request.PostalCode);
            var customer = new Customer(
                userId,
                request.FirstName,
                request.LastName,
                request.Tckn,
                request.BirthDate,
                request.PhoneNumber,
                address);

            await _customerRepository.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        var tokens = _tokenService.CreateTokenPair(userId, request.Email, new[] { Roles.Customer });
        await _refreshTokenService.StoreAsync(userId, tokens.RefreshToken, tokens.RefreshTokenExpiresAt, cancellationToken);

        _logger.LogInformation("Yeni müşteri kaydı oluşturuldu. UserId: {UserId}", userId);

        return Result<AuthResponse>.Success(new AuthResponse(
            userId,
            request.Email,
            new[] { Roles.Customer },
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt));
    }
}
