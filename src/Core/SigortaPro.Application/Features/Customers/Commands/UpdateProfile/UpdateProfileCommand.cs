using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Commands.UpdateProfile;

// Oturum sahibi müşteri kendi profilini günceller. TCKN ve doğum tarihi kimlik alanları olduğundan
// değiştirilemez; yalnızca ad/soyad, telefon ve adres güncellenir.
public sealed record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string City,
    string District,
    string Neighborhood,
    string PostalCode) : ICommand<CustomerDto>;
