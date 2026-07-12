using FluentValidation;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteList;

public sealed class GetQuoteListQueryValidator : AbstractValidator<GetQuoteListQuery>
{
    public GetQuoteListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1'den küçük olamaz.");

        RuleFor(query => query.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa boyutu 1'den küçük olamaz.");

        RuleFor(query => query.Status!.Value)
            .IsInEnum().WithMessage("Geçersiz teklif durumu.")
            .When(query => query.Status.HasValue);

        RuleFor(query => query.Branch!.Value)
            .IsInEnum().WithMessage("Geçersiz sigorta branşı.")
            .When(query => query.Branch.HasValue);
    }
}
