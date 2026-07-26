namespace SigortaPro.Application.Common.Search;

// Telefonla aramayı FORMAT BAĞIMSIZ yapar. Müşteri telefonu kanonik saklanır
// (ValidationPatterns.PhoneNumber → "+90XXXXXXXXXX"). Arama girdisi ise serbest olabilir:
// "05551111111", "0555 111 11 11", "0555-111-11-11", "+90 555 111 11 11".
// Girdiyi yalnızca rakamlara indirger ve ülke/ulusal ön ekini (baştaki "90" veya "0") atarak ABONE
// numarasına çevirir. Saklanan değerin rakamları ("905551111111") bu abone son ekini İÇERDİĞİNDEN
// arama, veritabanında `PhoneNumber.Replace("+","").Contains(<son ek>)` ile eşleşir → migration/yeni alan yok.
public static class PhoneNumberSearch
{
    // Bu uzunluğun altındaki abone son eki "çok kısa" sayılır; telefon eşleşmesi uygulanmaz (her şeye eşleşmesin).
    public const int MinSubscriberDigits = 3;

    /// <summary>Serbest telefon girdisini abone son ekine indirger (ör. "0555 111 11 11" → "5551111111").</summary>
    public static string ToSubscriberDigits(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return string.Empty;
        }

        var digits = new string(term.Where(char.IsDigit).ToArray());

        // Uluslararası ön ek (90...) → ulusal; ardından baştaki "0" (ulusal trunk) atılır.
        if (digits.Length > 10 && digits.StartsWith("90", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.StartsWith('0'))
        {
            digits = digits[1..];
        }

        return digits;
    }

    /// <summary>Telefon eşleşmesi uygulanacak kadar anlamlı bir abone son eki var mı?</summary>
    public static bool HasUsablePhoneQuery(string? term) =>
        ToSubscriberDigits(term).Length >= MinSubscriberDigits;
}
