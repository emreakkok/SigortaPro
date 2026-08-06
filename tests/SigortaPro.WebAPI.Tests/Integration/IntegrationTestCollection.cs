namespace SigortaPro.WebAPI.Tests.Integration;

// Tüm entegrasyon test sınıfları TEK factory/host'u paylaşır (collection fixture) ve sıralı çalışır. Nedenleri:
// (1) Program.cs'in Serilog bootstrap logger'ı (CreateBootstrapLogger) ilk host kurulumunda dondurulur;
//     aynı test sürecinde ikinci bir WebApplicationFactory host'u "The logger is already frozen" ile çöker.
// (2) Paylaşılan SQLite in-memory bağlantısı eşzamanlı erişime uygun değildir; sıralı çalışma bunu da çözer.
// (3) Auth uçları IP başına 10 istek/dk rate limit'lidir; koleksiyondaki toplam HTTP auth çağrısı
//     bu bütçenin altında tutulmalıdır (arrange için TestAccountFactory/ISender kullanın, HTTP değil).
// Yeni entegrasyon test sınıfları da bu koleksiyona eklenmelidir; ayrı factory oluşturmayın.
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<SigortaProWebApplicationFactory>
{
    public const string Name = "SigortaPro Integration";
}
