using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Customers;

// Müşteri modülü testlerinde kullanılan entity kurucuları.
internal static class CustomerTestData
{
    public static Customer CreateCustomer(Guid appUserId, Guid customerId)
    {
        var customer = new Customer(
            appUserId,
            "Ayşe",
            "Yılmaz",
            "11111111110",
            new DateTime(1990, 5, 12, 0, 0, 0, DateTimeKind.Utc),
            "+905551112233",
            new Address("İstanbul", "Kadıköy", "Caferağa", "34710"))
        {
            Id = customerId,
        };

        return customer;
    }

    public static Vehicle CreateVehicle(Guid customerId, Guid vehicleId)
    {
        return new Vehicle(customerId, "34 ABC 123", "Toyota", "Corolla", 2022, 132, VehicleUsage.Hususi)
        {
            Id = vehicleId,
        };
    }
}
