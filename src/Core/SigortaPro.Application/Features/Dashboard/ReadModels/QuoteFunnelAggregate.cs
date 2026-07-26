namespace SigortaPro.Application.Features.Dashboard.ReadModels;

// Bir tarih aralığında OLUŞTURULAN tekliflerin (Quote.CreatedAt) güncel durumlarına göre kırılımı —
// satış hunisi ve dönüşüm oranının kaynağı. Draft kalıcı değildir (CreateQuote anında MarkAsPriced çağırır),
// bu yüzden huni "Fiyatlandı"dan başlar. API DTO'su değil; salt okunur sorgu sonucudur (ADR-026).
public sealed record QuoteFunnelAggregate(
    int Priced,
    int Approved,
    int Purchased,
    int Expired,
    int Rejected);
