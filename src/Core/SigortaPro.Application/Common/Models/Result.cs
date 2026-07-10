namespace SigortaPro.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<string>());

    public static Result Failure(params string[] errors) => new(false, errors);

    public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToList());
}

// CA1000 (generic tipte statik üye) bilinçli olarak suppress edilmiştir: Result<T>.Success/Failure
// factory pattern'i, çağıran tarafta "new Result<T>(...)" yerine okunabilir bir API sağlar; bu .NET
// ekosisteminde (örn. FluentResults, Ardalis.Result) yaygın ve kabul görmüş bir tasarımdır.
#pragma warning disable CA1000
public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, IReadOnlyList<string> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<string>());

    public static new Result<T> Failure(params string[] errors) => new(false, default, errors);

    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.ToList());
}
#pragma warning restore CA1000
