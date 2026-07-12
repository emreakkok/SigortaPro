namespace SigortaPro.Domain.Constants;

public static class BusinessConstants
{
    public const int TcknLength = 11;
    public const int MaxQuoteValidityDays = 7;
    public const int RenewalNoticeWindowDays = 30;
    public const string PolicyNumberPrefix = "POL";
    public const int MaskedCardVisibleDigits = 4;

    // Satın alınan poliçenin varsayılan vade süresi (yıl). Poliçe bitiş tarihi başlangıç + bu süre olarak set edilir.
    public const int PolicyTermYears = 1;
}
