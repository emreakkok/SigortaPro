using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Queries.GetCustomerList;

// Admin müşteri listesi: sayfalama + ad/soyad/TCKN araması + il filtresi (TASKS.md).
public sealed record GetCustomerListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? City = null) : IQuery<PagedResult<CustomerSummaryDto>>;
