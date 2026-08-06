using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Quotes;

// Teklif kaynak sahipliği kontrolü: müşteri yalnızca kendi teklifine erişir;
// Admin/Personel tüm tekliflere erişebilir. Sahiplik, teklifin CustomerId'si ile çağıranın müşteri
// kaydı karşılaştırılarak değerlendirilir (EF navigasyonuna bağlı değildir).
internal static class QuoteAuthorization
{
    public static bool IsStaff(ICurrentUserService currentUser) =>
        Roles.StaffRoles.Any(currentUser.IsInRole);

    public static async Task EnsureCanAccessAsync(
        Guid quoteCustomerId,
        ICurrentUserService currentUser,
        ICustomerRepository customerRepository,
        CancellationToken cancellationToken)
    {
        if (IsStaff(currentUser))
        {
            return;
        }

        var appUserId = currentUser.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new ForbiddenAccessException();

        if (customer.Id != quoteCustomerId)
        {
            throw new ForbiddenAccessException();
        }
    }
}
