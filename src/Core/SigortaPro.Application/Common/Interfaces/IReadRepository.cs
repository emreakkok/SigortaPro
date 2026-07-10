using SigortaPro.Application.Common.Models;
using SigortaPro.Domain.Common;

namespace SigortaPro.Application.Common.Interfaces;

public interface IReadRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<T>> GetPagedAsync(PaginationParams paging, CancellationToken cancellationToken = default);

    // Yalnızca projection ihtiyacı için; döndürülen IQueryable handler dışına sızdırılmaz.
    IQueryable<T> AsQueryable();
}
