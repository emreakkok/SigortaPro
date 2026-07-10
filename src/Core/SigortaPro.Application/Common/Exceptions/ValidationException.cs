using FluentValidation.Results;

namespace SigortaPro.Application.Common.Exceptions;

public sealed class ValidationException : SigortaProException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("Bir veya daha fazla doğrulama hatası oluştu.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(failure => failure.PropertyName, failure => failure.ErrorMessage)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }
}
