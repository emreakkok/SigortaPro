using FluentValidation;

namespace SigortaPro.Application.Features.Claims.Queries.GetClaimList;

public sealed class GetClaimListQueryValidator : AbstractValidator<GetClaimListQuery>
{
    public GetClaimListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1'den küçük olamaz.");

        RuleFor(query => query.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa boyutu 1'den küçük olamaz.");
    }
}
