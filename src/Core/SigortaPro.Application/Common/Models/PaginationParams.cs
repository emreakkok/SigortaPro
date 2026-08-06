namespace SigortaPro.Application.Common.Models;

public sealed class PaginationParams
{
    // : varsayılan pageSize=20, maksimum=100.
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private int _pageSize = DefaultPageSize;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            <= 0 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
