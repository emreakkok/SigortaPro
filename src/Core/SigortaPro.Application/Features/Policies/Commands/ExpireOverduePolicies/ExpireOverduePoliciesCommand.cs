using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Policies.Commands.ExpireOverduePolicies;

// Sistem tetikli (arkaplan servisi): bitiş tarihi geçmiş aktif poliçeleri Expired'a çeker. Etkilenen adet döner.
public sealed record ExpireOverduePoliciesCommand : ICommand<int>;
