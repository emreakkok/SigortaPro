namespace SigortaPro.Application.Common.Exceptions;

public sealed class BusinessRuleException : SigortaProException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
