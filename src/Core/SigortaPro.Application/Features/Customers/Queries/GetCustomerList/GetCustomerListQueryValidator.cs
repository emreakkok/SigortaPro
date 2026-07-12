using FluentValidation;

namespace SigortaPro.Application.Features.Customers.Queries.GetCustomerList;

public sealed class GetCustomerListQueryValidator : AbstractValidator<GetCustomerListQuery>
{
    public GetCustomerListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1'den küçük olamaz.");

        RuleFor(query => query.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa boyutu 1'den küçük olamaz.");

        RuleFor(query => query.SearchTerm)
            .MaximumLength(100).WithMessage("Arama terimi en fazla 100 karakter olabilir.")
            .When(query => query.SearchTerm is not null);

        RuleFor(query => query.City)
            .MaximumLength(100).WithMessage("İl en fazla 100 karakter olabilir.")
            .When(query => query.City is not null);
    }
}
