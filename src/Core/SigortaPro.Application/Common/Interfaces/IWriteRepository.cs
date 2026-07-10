using SigortaPro.Domain.Common;

namespace SigortaPro.Application.Common.Interfaces;

public interface IWriteRepository<T> where T : BaseEntity
{
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);

    // Soft delete — IsDeleted = true olarak işaretler, fiziksel silme yapmaz (ADR-010).
    void Delete(T entity);
}
