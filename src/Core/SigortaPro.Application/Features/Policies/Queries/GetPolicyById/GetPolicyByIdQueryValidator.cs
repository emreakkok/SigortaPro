using FluentValidation;

namespace SigortaPro.Application.Features.Policies.Queries.GetPolicyById;

public sealed class GetPolicyByIdQueryValidator : AbstractValidator<GetPolicyByIdQuery>
{
    public GetPolicyByIdQueryValidator()
    {
        RuleFor(query => query.PolicyId)
            .NotEmpty().WithMessage("Poliçe kimliği zorunludur.");
    }
}
