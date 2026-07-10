namespace SigortaPro.Application.Common.Exceptions;

public sealed class NotFoundException : SigortaProException
{
    public NotFoundException(string entityName, object key)
        : base($"'{entityName}' ({key}) bulunamadı.")
    {
    }
}
