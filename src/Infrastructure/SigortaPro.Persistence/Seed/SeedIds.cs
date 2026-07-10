namespace SigortaPro.Persistence.Seed;

// DEVELOPMENT_RULES.md §5.4: Seed data sabit Guid'ler kullanır (test verileriyle tutarlılık için).
// SampleCustomerAppUserId, Task 5'te örnek müşterinin Identity kullanıcısı seed edilirken
// AppUser.Id olarak aynen kullanılmalıdır; aksi halde Customer.AppUserId hiçbir Identity
// kaydına karşılık gelmez.
public static class SeedIds
{
    public static readonly Guid KaskoProductId = new("10000000-0000-0000-0000-000000000001");
    public static readonly Guid TrafikProductId = new("10000000-0000-0000-0000-000000000002");
    public static readonly Guid KonutProductId = new("10000000-0000-0000-0000-000000000003");
    public static readonly Guid DaskProductId = new("10000000-0000-0000-0000-000000000004");
    public static readonly Guid SaglikProductId = new("10000000-0000-0000-0000-000000000005");

    public static readonly Guid SampleCustomerAppUserId = new("20000000-0000-0000-0000-000000000001");
    public static readonly Guid SampleCustomerId = new("20000000-0000-0000-0000-000000000002");
    public static readonly Guid SampleVehicleId = new("20000000-0000-0000-0000-000000000003");
    public static readonly Guid SampleQuoteId = new("20000000-0000-0000-0000-000000000004");
    public static readonly Guid SamplePolicyId = new("20000000-0000-0000-0000-000000000005");
}
