using FluentValidation;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetPolicyReport;

public sealed class GetPolicyReportQueryValidator : AbstractValidator<GetPolicyReportQuery>
{
    public GetPolicyReportQueryValidator()
    {
        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From)
            .WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1'den küçük olamaz.");

        RuleFor(query => query.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Sayfa boyutu 1'den küçük olamaz.");
    }
}
