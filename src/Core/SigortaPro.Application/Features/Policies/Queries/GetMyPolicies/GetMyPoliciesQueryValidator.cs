using FluentValidation;

namespace SigortaPro.Application.Features.Policies.Queries.GetMyPolicies;

public sealed class GetMyPoliciesQueryValidator : AbstractValidator<GetMyPoliciesQuery>
{
    public GetMyPoliciesQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1'den küçük olamaz.");

        RuleFor(query => query.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa boyutu 1'den küçük olamaz.");
    }
}
