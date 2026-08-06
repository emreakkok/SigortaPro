using FluentValidation;
using SigortaPro.Application.Common.Payments;

namespace SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;

// Yapısal kart/ödeme doğrulaması (400). Luhn ve senaryo bazlı ret gateway'in sorumluluğundadır (402).
public sealed class PurchaseQuoteCommandValidator : AbstractValidator<PurchaseQuoteCommand>
{
    public PurchaseQuoteCommandValidator()
    {
        RuleFor(command => command.QuoteId)
            .NotEmpty().WithMessage("Teklif kimliği zorunludur.");

        RuleFor(command => command.CardNumber)
            .NotEmpty().WithMessage("Kart numarası zorunludur.")
            .Must(HaveValidCardDigitCount).WithMessage("Kart numarası 13-19 haneli olmalıdır.");

        RuleFor(command => command.CardHolderName)
            .NotEmpty().WithMessage("Kart sahibi adı zorunludur.")
            .MaximumLength(100).WithMessage("Kart sahibi adı en fazla 100 karakter olabilir.");

        RuleFor(command => command.ExpiryMonth)
            .Matches("^(0[1-9]|1[0-2])$").WithMessage("Son kullanma ayı 01-12 aralığında olmalıdır.");

        RuleFor(command => command.ExpiryYear)
            .Matches(@"^\d{4}$").WithMessage("Son kullanma yılı 4 haneli olmalıdır.");

        RuleFor(command => command.Cvv)
            .Matches(@"^\d{3,4}$").WithMessage("CVV 3 veya 4 haneli olmalıdır.");

        RuleFor(command => command.InstallmentCount)
            .Must(PaymentOptions.AllowedInstallmentCounts.Contains)
            .WithMessage("Geçersiz taksit sayısı.");
    }

    private static bool HaveValidCardDigitCount(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return false;
        }

        // Boşluk/ayraçlar hariç yalnızca rakam ve 13-19 hane olmalı (Luhn kontrolü gateway'de).
        var digitCount = cardNumber.Count(char.IsDigit);
        var nonDigit = cardNumber.Any(character => !char.IsDigit(character) && !char.IsWhiteSpace(character));
        return !nonDigit && digitCount is >= 13 and <= 19;
    }
}
