namespace SigortaPro.Application.Common.Exceptions;

public sealed class ForbiddenAccessException : SigortaProException
{
    public ForbiddenAccessException()
        : base("Bu kaynağa erişim yetkiniz yok.")
    {
    }

    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
