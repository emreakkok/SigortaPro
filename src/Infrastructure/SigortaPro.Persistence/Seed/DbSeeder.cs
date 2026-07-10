using Microsoft.EntityFrameworkCore;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Constants;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Seed;

// TASKS.md Task 4: ürünler, teminatlar, örnek müşteri/poliçe seed edilir.
// Admin kullanıcısı Identity gerektirdiği için Task 5'te seed edilecektir (ADR-014).
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.InsuranceProducts.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = CreateInsuranceProducts();
        await context.InsuranceProducts.AddRangeAsync(products, cancellationToken);

        var customer = CreateSampleCustomer();
        await context.Customers.AddAsync(customer, cancellationToken);

        var kaskoProductId = products.Single(p => p.Branch == InsuranceBranch.Kasko).Id;
        var (vehicle, quote, policy) = CreateSamplePolicy(customer.Id, kaskoProductId);

        await context.Vehicles.AddAsync(vehicle, cancellationToken);
        await context.Quotes.AddAsync(quote, cancellationToken);
        await context.Policies.AddAsync(policy, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<InsuranceProduct> CreateInsuranceProducts()
    {
        var kasko = new InsuranceProduct(
            "Kasko Sigortası",
            InsuranceBranch.Kasko,
            "Aracınızın çarpma, çarpışma, yangın, hırsızlık ve cam kırılması risklerine karşı geniş kapsamlı teminat.")
        {
            Id = SeedIds.KaskoProductId
        };
        kasko.AddCoverage(new Coverage(kasko.Id, "Çarpma/Çarpışma", "Aracın çarpışma sonucu hasarları", 300000m));
        kasko.AddCoverage(new Coverage(kasko.Id, "Yangın", "Yangın sonucu oluşan hasarlar", 300000m));
        kasko.AddCoverage(new Coverage(kasko.Id, "Hırsızlık", "Aracın çalınması veya çalınmaya teşebbüs hasarları", 300000m));
        kasko.AddCoverage(new Coverage(kasko.Id, "Cam Kırılması", "Ön/arka/yan cam hasarları", 15000m));

        var trafik = new InsuranceProduct(
            "Trafik Sigortası",
            InsuranceBranch.Trafik,
            "Zorunlu trafik sigortası — üçüncü şahıslara verilen maddi ve bedeni zararları karşılar.")
        {
            Id = SeedIds.TrafikProductId
        };
        trafik.AddCoverage(new Coverage(trafik.Id, "Maddi Hasar", "Karşı tarafın araç/eşya hasarı", 60000m));
        trafik.AddCoverage(new Coverage(trafik.Id, "Bedeni Hasar", "Karşı tarafın sağlık/tedavi giderleri", 2500000m));

        var konut = new InsuranceProduct(
            "Konut Sigortası",
            InsuranceBranch.Konut,
            "Konutunuzu yangın, hırsızlık ve su baskını gibi risklere karşı güvence altına alır.")
        {
            Id = SeedIds.KonutProductId
        };
        konut.AddCoverage(new Coverage(konut.Id, "Yangın", "Yangın, duman, infilak hasarları", 500000m));
        konut.AddCoverage(new Coverage(konut.Id, "Hırsızlık", "Hırsızlık sonucu eşya kaybı", 50000m));
        konut.AddCoverage(new Coverage(konut.Id, "Su Baskını", "Sel/su baskını hasarları", 50000m));

        var dask = new InsuranceProduct(
            "DASK (Zorunlu Deprem Sigortası)",
            InsuranceBranch.Dask,
            "Doğal Afet Sigortaları Kurumu güvencesiyle zorunlu deprem sigortası.")
        {
            Id = SeedIds.DaskProductId
        };
        dask.AddCoverage(new Coverage(dask.Id, "Deprem Teminatı", "Deprem sonucu bina hasarları", 640000m));

        var saglik = new InsuranceProduct(
            "Sağlık Sigortası",
            InsuranceBranch.Saglik,
            "Yatarak ve ayakta tedavi giderlerini karşılayan özel sağlık sigortası.")
        {
            Id = SeedIds.SaglikProductId
        };
        saglik.AddCoverage(new Coverage(saglik.Id, "Yatarak Tedavi", "Hastane yatış ve ameliyat giderleri", 1000000m));
        saglik.AddCoverage(new Coverage(saglik.Id, "Ayakta Tedavi", "Muayene, tahlil ve ilaç giderleri", 50000m));

        return new List<InsuranceProduct> { kasko, trafik, konut, dask, saglik };
    }

    private static Customer CreateSampleCustomer()
    {
        var address = new Address("İstanbul", "Kadıköy", "Caferağa", "34710");

        return new Customer(
            SeedIds.SampleCustomerAppUserId,
            "Ayşe",
            "Yılmaz",
            "11111111110",
            new DateTime(1990, 5, 12, 0, 0, 0, DateTimeKind.Utc),
            "+905551112233",
            address)
        {
            Id = SeedIds.SampleCustomerId
        };
    }

    private static (Vehicle Vehicle, Quote Quote, Policy Policy) CreateSamplePolicy(Guid customerId, Guid kaskoProductId)
    {
        var vehicle = new Vehicle(customerId, "34 ABC 123", "Toyota", "Corolla", 2022, 132)
        {
            Id = SeedIds.SampleVehicleId
        };

        var quote = new Quote(customerId, kaskoProductId, InsuranceBranch.Kasko, vehicle.Id, null)
        {
            Id = SeedIds.SampleQuoteId
        };
        quote.MarkAsPriced(24500m, DateTime.UtcNow.AddDays(BusinessConstants.MaxQuoteValidityDays));
        quote.Approve();
        quote.Purchase();

        var policy = new Policy(
            $"{BusinessConstants.PolicyNumberPrefix}-{DateTime.UtcNow.Year}-000001",
            customerId,
            quote.Id,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddYears(1),
            24500m)
        {
            Id = SeedIds.SamplePolicyId
        };

        return (vehicle, quote, policy);
    }
}
