using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff.Commands.CreateStaffUser;

// ADR-060: Admin bir Personel hesabı oluşturur. GÜVENLİK: DTO'da rol/IsActive alanı YOKTUR — rol sunucuda
// daima Roles.Personel'e sabitlenir (mass-assignment/privilege-escalation savunması). Admin/Customer
// oluşturma yolu bilinçli olarak açılmamıştır.
public sealed record CreateStaffUserCommand(
    string Email,
    string FullName,
    string Password) : ICommand<StaffDetailDto>;
