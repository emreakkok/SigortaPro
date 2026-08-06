using SigortaPro.Domain.Constants;

namespace SigortaPro.Application.Common.Validation;

// T.C. Kimlik Numarası algoritmik doğrulaması. Kayıt sırasında
// oluşturulan müşterinin TCKN'si burada doğrulanır; 'nin müşteri modülü de bu ortak kuralı kullanır.
public static class TcknValidation
{
    public static bool IsValid(string? tckn)
    {
        if (string.IsNullOrWhiteSpace(tckn) || tckn.Length != BusinessConstants.TcknLength)
        {
            return false;
        }

        var digits = new int[BusinessConstants.TcknLength];
        for (var i = 0; i < BusinessConstants.TcknLength; i++)
        {
            if (!char.IsDigit(tckn[i]))
            {
                return false;
            }

            digits[i] = tckn[i] - '0';
        }

        // İlk hane 0 olamaz.
        if (digits[0] == 0)
        {
            return false;
        }

        var sumOdd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var sumEven = digits[1] + digits[3] + digits[5] + digits[7];

        // 10. hane: ((tek indeksli toplamlar × 7) − çift indeksli toplamlar) mod 10.
        var tenthDigit = ((sumOdd * 7) - sumEven) % 10;
        if (tenthDigit < 0)
        {
            tenthDigit += 10;
        }

        if (tenthDigit != digits[9])
        {
            return false;
        }

        // 11. hane: ilk 10 hanenin toplamı mod 10.
        var firstTenSum = 0;
        for (var i = 0; i < 10; i++)
        {
            firstTenSum += digits[i];
        }

        return firstTenSum % 10 == digits[10];
    }
}
