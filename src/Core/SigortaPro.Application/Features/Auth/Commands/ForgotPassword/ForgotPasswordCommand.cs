using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Features.Auth.Commands.ForgotPassword;

// Şifre sıfırlama talebi. Güvenlik gereği (kullanıcı varlığını sızdırmama) sonuç her zaman başarıdır;
// controller da sonuçtan bağımsız tek bir generic yanıt döner.
public sealed record ForgotPasswordCommand(string Email) : ICommand<Result>;
