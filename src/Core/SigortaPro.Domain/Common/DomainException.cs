namespace SigortaPro.Domain.Common;

/// <summary>
/// Domain entity'lerindeki durum geçişi ve iş kuralı ihlallerinde fırlatılır.
/// Application katmanı bu exception'ı yakalayıp uygun HTTP durum koduna çevirir.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
