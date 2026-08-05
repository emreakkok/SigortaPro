using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Authorization;

// Bir işlemin (teklif/araç/konut oluşturma) HEDEF müşterisini çözer. İki mod:
//  • Self-service: requestedCustomerId null → oturum sahibi müşterinin KENDİ kaydı (mevcut davranış birebir korunur).
//  • Acente destekli (on-behalf): requestedCustomerId dolu → çağıran YALNIZCA acente personeli (Admin/Personel)
//    olabilir; verilen müşteri kaydı çözülür. Bir müşteri BAŞKA bir müşteri adına işlem yapamaz (ForbiddenAccess).
// "Müşteri adına işlem" yetkisi böylece tek yerde tanımlanır ve tüm ilgili handler'larda tutarlı olur (DRY).
internal static class TargetCustomerResolver
{
    public static async Task<Customer> ResolveTrackedAsync(
        Guid? requestedCustomerId,
        ICurrentUserService currentUser,
        ICustomerRepository customerRepository,
        CancellationToken cancellationToken)
    {
        if (requestedCustomerId is Guid customerId)
        {
            // Adına işlem yalnızca acente personeline (Admin ∪ Personel) açıktır.
            if (!Roles.StaffRoles.Any(currentUser.IsInRole))
            {
                throw new ForbiddenAccessException();
            }

            return await customerRepository.GetTrackedByIdAsync(customerId, cancellationToken)
                ?? throw new NotFoundException(nameof(Customer), customerId);
        }

        var appUserId = currentUser.UserId
            ?? throw new ForbiddenAccessException();

        return await customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);
    }

    // Teklife yazılacak "üreten personel" kimliği: on-behalf ise oturum sahibi personelin AppUser kimliği,
    // self-service ise null (müşteri kendi oluşturdu). Bu değer Quote.CreatedByStaffUserId'e geçirilir.
    public static Guid? ResolveProducingStaffId(Guid? requestedCustomerId, ICurrentUserService currentUser) =>
        requestedCustomerId is null ? null : currentUser.UserId;
}
