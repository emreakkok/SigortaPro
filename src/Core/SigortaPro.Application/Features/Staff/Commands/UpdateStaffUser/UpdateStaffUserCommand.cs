using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff.Commands.UpdateStaffUser;

// Personel güncelleme (yalnızca Admin). YALNIZCA FullName değişir; e-posta ve rol değiştirilemez,
// IsActive bu uçtan yönetilmez (ayrı status ucu). Id route'tan gelir (gövdede otoriter değildir).
public sealed record UpdateStaffUserCommand(Guid Id, string FullName) : ICommand<StaffDetailDto>;
