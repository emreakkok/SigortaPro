using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Features.Auth.Commands.ChangePassword;

// Oturum sahibinin şifresini değiştirir (ADR-040). Kullanıcı kimliği ICurrentUserService'ten alınır;
// mevcut şifre yanlışsa soft-fail döner (controller 400'e eşler). JWT/refresh mimarisi değişmez.
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<Result>;
