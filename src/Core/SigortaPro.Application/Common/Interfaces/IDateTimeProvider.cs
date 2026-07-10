namespace SigortaPro.Application.Common.Interfaces;

// Implementasyonu Infrastructure katmanında sağlanır — bkz. ARCHITECTURE_RULES.md §6.1.
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
