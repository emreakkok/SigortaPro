using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Queries.GetCustomerById;

// Belirli bir müşterinin profil detayını döner (admin/personel görünümü).
public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDto>;
