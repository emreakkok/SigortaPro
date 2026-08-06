using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Auth.DTOs;

namespace SigortaPro.Application.Features.Auth.Commands.Register;

// Müşteri kaydı: Identity kullanıcısı + Customer profili birlikte oluşturulur.
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Tckn,
    DateTime BirthDate,
    string PhoneNumber,
    string City,
    string District,
    string Neighborhood,
    string PostalCode) : ICommand<Result<AuthResponse>>;
