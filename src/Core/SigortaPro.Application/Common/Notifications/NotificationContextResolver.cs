using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Common.Notifications;

// INotificationContextResolver implementasyonu. Mevcut soyutlamaları yeniden kullanır
// (ICurrentUserService + ICustomerRepository) — yeni tablo/audit altyapısı kurulmaz.
// Sorgu maliyeti bilinçli olarak düşük tutulur: personel için hiç sorgu yapılmaz (e-posta zaten
// oturum bağlamındadır), müşteri için tek profil okuması yapılır.
public sealed class NotificationContextResolver : INotificationContextResolver
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICustomerRepository _customerRepository;

    public NotificationContextResolver(
        ICurrentUserService currentUserService,
        ICustomerRepository customerRepository)
    {
        _currentUserService = currentUserService;
        _customerRepository = customerRepository;
    }

    public async Task<NotificationActor> ResolveActorAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            // Anonim akış (ör. şifre sıfırlama isteği) — actor bilgisi yoktur ve üretilmez.
            return NotificationActor.Anonymous;
        }

        var isStaff = Roles.StaffRoles.Any(_currentUserService.IsInRole);

        if (isStaff)
        {
            // Personelin ad-soyadı veri modelinde tutulmaz; iç operasyon kimliği olarak e-posta kullanılır.
            return new NotificationActor(userId, _currentUserService.Email, true);
        }

        var customer = await _customerRepository.GetProfileByAppUserIdAsync(userId.Value, cancellationToken);
        var displayName = customer is null
            ? _currentUserService.Email
            : $"{customer.FirstName} {customer.LastName}".Trim();

        return new NotificationActor(userId, displayName, false);
    }

    public async Task<string?> GetCustomerNameAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        return customer is null ? null : $"{customer.FirstName} {customer.LastName}".Trim();
    }
}
