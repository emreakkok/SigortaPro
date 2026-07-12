using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes;

// Branşa göre risk objesini (araç/konut) çözer ve müşteriye ait olduğunu doğrular (kaynak sahipliği).
// CreateQuote ve GetQuoteComparison tarafından paylaşılır. Sağlık branşında risk objesi yoktur.
internal static class QuoteRiskObjectResolver
{
    public static async Task<(Vehicle? Vehicle, Property? Property)> ResolveAsync(
        InsuranceBranch branch,
        Guid? vehicleId,
        Guid? propertyId,
        Guid customerId,
        IReadRepository<Vehicle> vehicleRepository,
        IReadRepository<Property> propertyRepository,
        CancellationToken cancellationToken)
    {
        if (branch is InsuranceBranch.Kasko or InsuranceBranch.Trafik)
        {
            var vehicle = await vehicleRepository.GetByIdAsync(vehicleId!.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Vehicle), vehicleId!.Value);

            if (vehicle.CustomerId != customerId)
            {
                throw new ForbiddenAccessException();
            }

            return (vehicle, null);
        }

        if (branch is InsuranceBranch.Konut or InsuranceBranch.Dask)
        {
            var property = await propertyRepository.GetByIdAsync(propertyId!.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Property), propertyId!.Value);

            if (property.CustomerId != customerId)
            {
                throw new ForbiddenAccessException();
            }

            return (null, property);
        }

        // Sağlık: risk objesi yok.
        return (null, null);
    }
}
