using System.Globalization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff.Queries.GetStaffList;

public sealed class GetStaffListQueryHandler : IQueryHandler<GetStaffListQuery, PagedResult<StaffListItemDto>>
{
    // Türkçe İ/ı duyarlı, büyük/küçük harf bağımsız arama.
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private readonly IIdentityService _identityService;

    public GetStaffListQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<PagedResult<StaffListItemDto>> Handle(GetStaffListQuery request, CancellationToken cancellationToken)
    {
        var paging = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        // MVP ölçeğinde personel sayısı düşüktür; filtreleme ve sayfalama bellek içinde yapılır.
        var all = await _identityService.GetStaffUsersAsync(cancellationToken);

        IEnumerable<StaffUserInfo> filtered = all;

        if (request.IsActive is bool isActive)
        {
            filtered = filtered.Where(user => user.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            filtered = filtered.Where(user =>
                Contains(user.Email, term) || Contains(user.FullName, term));
        }

        var ordered = filtered
            .OrderBy(user => user.FullName ?? user.Email, StringComparer.Create(TurkishCulture, ignoreCase: true))
            .ToList();

        var pageItems = ordered
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .Select(user => user.ToListItemDto())
            .ToList();

        return new PagedResult<StaffListItemDto>(pageItems, paging.Page, paging.PageSize, ordered.Count);
    }

    private static bool Contains(string? source, string term) =>
        source is not null && TurkishCulture.CompareInfo.IndexOf(source, term, CompareOptions.IgnoreCase) >= 0;
}
