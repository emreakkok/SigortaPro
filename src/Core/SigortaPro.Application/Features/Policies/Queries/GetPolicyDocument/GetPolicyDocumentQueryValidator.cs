using FluentValidation;

namespace SigortaPro.Application.Features.Policies.Queries.GetPolicyDocument;

public sealed class GetPolicyDocumentQueryValidator : AbstractValidator<GetPolicyDocumentQuery>
{
    public GetPolicyDocumentQueryValidator()
    {
        RuleFor(query => query.PolicyId)
            .NotEmpty().WithMessage("Poliçe kimliği zorunludur.");
    }
}
