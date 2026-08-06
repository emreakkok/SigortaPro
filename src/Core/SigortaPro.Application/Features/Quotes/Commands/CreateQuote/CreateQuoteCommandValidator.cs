using FluentValidation;
using SigortaPro.Application.Common.Validation;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.Commands.CreateQuote;

public sealed class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    // Telefon formatı ile aynı: +90 + 10 hane (Register/Profil kurallarının aynası).
    private const string PhoneRegex = @"^\+90\d{10}$";

    public CreateQuoteCommandValidator()
    {
        RuleFor(command => command.Branch)
            .IsInEnum().WithMessage("Geçersiz sigorta branşı.");

        RuleFor(command => command.CoveragePackage)
            .IsInEnum().WithMessage("Geçersiz teminat paketi.");

        // Kasko/Trafik → araç zorunlu; Konut/DASK → konut zorunlu; Sağlık → risk objesi gerekmez.
        When(command => command.Branch is InsuranceBranch.Kasko or InsuranceBranch.Trafik, () =>
        {
            RuleFor(command => command.VehicleId)
                .NotEmpty().WithMessage("Kasko/Trafik teklifi için araç seçimi zorunludur.");
        });

        When(command => command.Branch is InsuranceBranch.Konut or InsuranceBranch.Dask, () =>
        {
            RuleFor(command => command.PropertyId)
                .NotEmpty().WithMessage("Konut/DASK teklifi için konut seçimi zorunludur.");
        });

        // Sağlıkta sigara beyanı ZORUNLUDUR. Önceden hiç sorulmadan "false" varsayılıyor ve prim
        // yanlış hesaplanıyordu; artık beyan alınmadan teklif oluşturulamaz (sessiz varsayım yasak).
        When(command => command.Branch == InsuranceBranch.Saglik, () =>
        {
            RuleFor(command => command.IsSmoker)
                .NotNull().WithMessage("Sağlık teklifi için sigara kullanım beyanı zorunludur.");
        });

        // Sağlık dışı branşlarda beyan anlamsızdır; sessizce yok saymak yerine açıkça reddedilir.
        When(command => command.Branch != InsuranceBranch.Saglik, () =>
        {
            RuleFor(command => command.IsSmoker)
                .Null().WithMessage("Sigara beyanı yalnızca Sağlık branşında gönderilebilir.");
        });

        // "Başkası adına" sigortalı beyanı yalnızca Sağlık branşında anlamlıdır ve verildiğinde
        // alanları Register/Profil kurallarını aynalar. Branş uyumsuzluğunun son sözü domain guard'ındadır (409).
        When(command => command.InsuredPerson is not null, () =>
        {
            RuleFor(command => command.Branch)
                .Equal(InsuranceBranch.Saglik)
                .WithMessage("Sigortalı bilgisi yalnızca Sağlık branşında girilebilir.");

            RuleFor(command => command.InsuredPerson!.FirstName)
                .NotEmpty().WithMessage("Sigortalı adı zorunludur.")
                .MaximumLength(100).WithMessage("Sigortalı adı en fazla 100 karakter olabilir.");

            RuleFor(command => command.InsuredPerson!.LastName)
                .NotEmpty().WithMessage("Sigortalı soyadı zorunludur.")
                .MaximumLength(100).WithMessage("Sigortalı soyadı en fazla 100 karakter olabilir.");

            RuleFor(command => command.InsuredPerson!.Tckn)
                .NotEmpty().WithMessage("Sigortalı TCKN zorunludur.")
                .Must(TcknValidation.IsValid).WithMessage("Geçerli bir sigortalı TCKN giriniz.");

            RuleFor(command => command.InsuredPerson!.BirthDate)
                .NotEmpty().WithMessage("Sigortalı doğum tarihi zorunludur.")
                .LessThan(DateTime.UtcNow).WithMessage("Sigortalı doğum tarihi bugünden sonra olamaz.");

            RuleFor(command => command.InsuredPerson!.PhoneNumber)
                .NotEmpty().WithMessage("Sigortalı telefon numarası zorunludur.")
                .Matches(PhoneRegex).WithMessage("Sigortalı telefonu +90 ile başlamalı ve 10 haneli olmalıdır.");

            RuleFor(command => command.InsuredPerson!.Relationship)
                .NotEmpty().WithMessage("Yakınlık derecesi zorunludur.")
                .MaximumLength(50).WithMessage("Yakınlık derecesi en fazla 50 karakter olabilir.");
        });
    }
}
