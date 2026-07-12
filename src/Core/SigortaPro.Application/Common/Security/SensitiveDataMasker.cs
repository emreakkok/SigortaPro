using SigortaPro.Domain.Constants;

namespace SigortaPro.Application.Common.Security;

// Hassas verilerin (TCKN vb.) DTO'lara ve log'lara maskeli taşınmasını sağlar.
// CODING_STANDARDS.md §4.2: DTO'lar asla tam TCKN içermez; §8.3: TCKN log'a maskeli yazılır.
public static class SensitiveDataMasker
{
    private const char MaskChar = '*';

    // Son 2 haneyi görünür bırakır, kalanı maskeler. Örn: "11111111110" → "*********10".
    private const int TcknVisibleDigits = 2;

    public static string MaskTckn(string? tckn)
    {
        if (string.IsNullOrWhiteSpace(tckn) || tckn.Length <= TcknVisibleDigits)
        {
            return new string(MaskChar, BusinessConstants.TcknLength);
        }

        var maskedLength = tckn.Length - TcknVisibleDigits;
        return new string(MaskChar, maskedLength) + tckn[maskedLength..];
    }

    // Kart numarasının yalnızca son 4 hanesini görünür bırakır (ADR-007). Boşluklar/ayraçlar temizlenir.
    // Örn: "4111 1111 1111 1111" → "************1111". Kart numarası asla tam saklanmaz (CLAUDE.md §4.5).
    public static string MaskCardNumber(string? cardNumber)
    {
        var digits = new string((cardNumber ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length <= BusinessConstants.MaskedCardVisibleDigits)
        {
            return new string(MaskChar, BusinessConstants.MaskedCardVisibleDigits);
        }

        var maskedLength = digits.Length - BusinessConstants.MaskedCardVisibleDigits;
        return new string(MaskChar, maskedLength) + digits[maskedLength..];
    }
}
