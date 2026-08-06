namespace SigortaPro.Application.Common.Interfaces;

// Implementasyonu Infrastructure katmanında sağlanır
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
