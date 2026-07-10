namespace SigortaPro.Application.Common.Exceptions;

public abstract class SigortaProException : Exception
{
    protected SigortaProException(string message) : base(message)
    {
    }
}
