namespace SigortaPro.Application.Features.Quotes.DTOs;

// Teklifin oluşturulma kaynağı. TÜRETİLMİŞ değerdir — kalıcı saklanmaz; Quote.CreatedByStaffUserId'den
// hesaplanır (null → SelfService, dolu → AgentAssisted). Böylece müşteriye/panele temiz bir semantik sunulur
// ve personel kimliği müşteri yüzeyine sızmaz ("Oluşturan: Acente" — personelin adı değil).
public enum QuoteSource
{
    // Müşteri teklifi kendi hesabından oluşturdu (online self-servis).
    SelfService = 0,

    // Acente personeli (Admin/Personel) teklifi müşteri ADINA oluşturdu (telefonla arayan müşteri senaryosu).
    AgentAssisted = 1,
}
