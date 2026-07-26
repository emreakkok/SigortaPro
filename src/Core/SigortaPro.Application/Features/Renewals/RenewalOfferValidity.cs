using SigortaPro.Domain.Constants;

namespace SigortaPro.Application.Features.Renewals;

// Yenileme teklifinin geçerlilik (son kabul) tarihini hesaplar. İş kuralı: müşteri MEVCUT poliçesi sona
// erene kadar yenilemeyi kabul edebilmelidir → teklif poliçe bitiş tarihine kadar geçerlidir; poliçe bitişine
// çok az kalmışsa en az standart teklif penceresi (BusinessConstants.MaxQuoteValidityDays) garanti edilir.
// (Önceki hata: teklif her zaman üretim + 7 gün geçerliydi ve poliçe hâlâ aktifken "süresi doldu" görünürdü.)
public static class RenewalOfferValidity
{
    public static DateTime Compute(DateTime now, DateTime policyEndDate)
    {
        var standardValidUntil = now.AddDays(BusinessConstants.MaxQuoteValidityDays);
        return policyEndDate > standardValidUntil ? policyEndDate : standardValidUntil;
    }
}
