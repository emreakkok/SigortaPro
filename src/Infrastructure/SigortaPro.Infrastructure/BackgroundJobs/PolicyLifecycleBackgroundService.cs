using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SigortaPro.Application.Features.Policies.Commands.ExpireOverduePolicies;
using SigortaPro.Application.Features.Quotes.Commands.ExpireOutdatedQuotes;
using SigortaPro.Application.Features.Renewals.Commands.GeneratePolicyRenewals;

namespace SigortaPro.Infrastructure.BackgroundJobs;

// Poliçe yaşam döngüsü arkaplan servisi (Task 13): periyodik olarak süresi dolmuş teklif/poliçeleri Expired'a
// çeker ve bitişine ≤30 gün kalan poliçeler için yenileme teklifi üretir. İş mantığı Application katmanındaki
// CQRS komutlarındadır; bu servis yalnızca zamanlama + scope yönetimi yapar (Clean Architecture korunur).
public sealed class PolicyLifecycleBackgroundService : BackgroundService
{
    // MVP için sabit periyot; başlangıçta bir kez, ardından bu aralıkla çalışır (KISS — yapılandırma gerekmiyor).
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PolicyLifecycleBackgroundService> _logger;

    public PolicyLifecycleBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PolicyLifecycleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Bir çalışmadaki hata servisi durdurmamalı; loglanır ve bir sonraki periyotta yeniden denenir.
                _logger.LogError(exception, "Poliçe yaşam döngüsü arkaplan işi sırasında hata oluştu.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        // Her çalışma için ayrı DI scope (BackgroundService singleton; scoped bağımlılıklar scope içinde çözülür).
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var expiredQuotes = await sender.Send(new ExpireOutdatedQuotesCommand(), cancellationToken);
        var expiredPolicies = await sender.Send(new ExpireOverduePoliciesCommand(), cancellationToken);
        var generatedRenewals = await sender.Send(new GeneratePolicyRenewalsCommand(), cancellationToken);

        if (expiredQuotes > 0 || expiredPolicies > 0 || generatedRenewals > 0)
        {
            _logger.LogInformation(
                "Poliçe yaşam döngüsü işi tamamlandı. Süresi dolan teklif: {ExpiredQuotes}, süresi dolan poliçe: " +
                "{ExpiredPolicies}, üretilen yenileme: {GeneratedRenewals}",
                expiredQuotes, expiredPolicies, generatedRenewals);
        }
    }
}
