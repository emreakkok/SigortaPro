using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Features.Auth.Commands.ResetPassword;

// E-posta + token + yeni şifre ile şifre sıfırlama. Token geçersiz/süresi dolmuş ise (veya hesap yoksa)
// soft-fail döner; controller bunu 400'e eşler (ADR-035). Hangisinin hatalı olduğu sızdırılmaz.
public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : ICommand<Result>;
