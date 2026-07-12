using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Renewals.DTOs;

namespace SigortaPro.Application.Features.Renewals.Commands.AcceptRenewal;

// Müşteri yenileme teklifini onaylar: yenileme kabul edilir ve yeni teklif Approved'a çekilir (ödeme için hazır).
public sealed record AcceptRenewalCommand(Guid RenewalId) : ICommand<RenewalDto>;
