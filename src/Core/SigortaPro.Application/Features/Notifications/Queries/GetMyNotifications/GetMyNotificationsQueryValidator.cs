using FluentValidation;
using SigortaPro.Application.Common.Notifications;

namespace SigortaPro.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    private static readonly string[] AllowedSeverities =
    {
        NotificationSeverity.Success,
        NotificationSeverity.Info,
        NotificationSeverity.Warning,
        NotificationSeverity.Error,
    };

    public GetMyNotificationsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0).WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");

        RuleFor(query => query.Severity)
            .Must(severity => severity is null || AllowedSeverities.Contains(severity))
            .WithMessage("Geçersiz bildirim önem düzeyi.");

        RuleFor(query => query.SearchTerm)
            .MaximumLength(200).WithMessage("Arama metni en fazla 200 karakter olabilir.");

        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From!.Value)
            .When(query => query.From is not null && query.To is not null)
            .WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.");
    }
}
