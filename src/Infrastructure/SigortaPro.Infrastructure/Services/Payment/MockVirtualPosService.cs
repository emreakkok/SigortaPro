using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Payments;
using SigortaPro.Application.Common.Security;

namespace SigortaPro.Infrastructure.Services.Payment;

// Mock sanal POS. Kartı Luhn ile doğrular, belirli test kartları için senaryo bazlı ret üretir,
// diğer geçerli kartlarda başarı döner. Saf/deterministik (dış çağrı/rastgele sonuç yok); ham kart log'lanmaz.
// Test kartları README.md'de belgelenir.
public sealed class MockVirtualPosService : IPaymentService
{
    // Senaryo kartları (her ikisi de Luhn-geçerli; Luhn aşamasından geçip iş kuralında reddedilir).
    private const string InsufficientFundsCard = "4000000000000002";
    private const string ThreeDSecureFailureCard = "4000000000000069";

    public Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken cancellationToken = default)
    {
        var digits = new string(request.CardNumber.Where(char.IsDigit).ToArray());
        var maskedCard = SensitiveDataMasker.MaskCardNumber(digits);

        if (!IsLuhnValid(digits))
        {
            return Task.FromResult(PaymentGatewayResult.Failure(maskedCard, "Geçersiz kart numarası."));
        }

        var result = digits switch
        {
            InsufficientFundsCard => PaymentGatewayResult.Failure(maskedCard, "Yetersiz bakiye."),
            ThreeDSecureFailureCard => PaymentGatewayResult.Failure(maskedCard, "3D Secure doğrulaması başarısız."),
            _ => PaymentGatewayResult.Success(maskedCard, GenerateReferenceCode()),
        };

        return Task.FromResult(result);
    }

    public IReadOnlyList<InstallmentOption> GetInstallmentOptions(decimal amount) =>
        PaymentOptions.AllowedInstallmentCounts
            .Select(count => new InstallmentOption(
                count,
                decimal.Round(amount / count, 2, MidpointRounding.AwayFromZero),
                amount)) // Faizsiz mock: toplam tutar sabit kalır.
            .ToList();

    // Standart Luhn (mod 10) algoritması.
    private static bool IsLuhnValid(string digits)
    {
        if (digits.Length is < 13 or > 19)
        {
            return false;
        }

        var sum = 0;
        var doubleDigit = false;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var value = digits[i] - '0';

            if (doubleDigit)
            {
                value *= 2;
                if (value > 9)
                {
                    value -= 9;
                }
            }

            sum += value;
            doubleDigit = !doubleDigit;
        }

        return sum % 10 == 0;
    }

    private static string GenerateReferenceCode() =>
        $"POS-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
}
