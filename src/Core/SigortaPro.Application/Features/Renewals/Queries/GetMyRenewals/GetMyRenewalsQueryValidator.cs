using FluentValidation;

namespace SigortaPro.Application.Features.Renewals.Queries.GetMyRenewals;

public sealed class GetMyRenewalsQueryValidator : AbstractValidator<GetMyRenewalsQuery>
{
    public GetMyRenewalsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1'den küçük olamaz.");

        RuleFor(query => query.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa boyutu 1'den küçük olamaz.");
    }
}
