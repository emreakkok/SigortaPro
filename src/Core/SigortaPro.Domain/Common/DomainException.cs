namespace SigortaPro.Domain.Common;

/// <summary>
/// Domain entity'lerindeki durum geçişi ve iş kuralı ihlallerinde fırlatılır.
/// Application katmanı bu exception'ı yakalayıp uygun HTTP durum koduna çevirir (bkz. Task 3).
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
