using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Policies.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Policies.Queries.GetPolicyById;

public sealed class GetPolicyByIdQueryHandler : IQueryHandler<GetPolicyByIdQuery, PolicyDetailDto>
{
    private readonly IPolicyRepository _policyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPricingEngine _pricingEngine;
    private readonly ICurrentUserService _currentUserService;

    public GetPolicyByIdQueryHandler(
        IPolicyRepository policyRepository,
        ICustomerRepository customerRepository,
        IPricingEngine pricingEngine,
        ICurrentUserService currentUserService)
    {
        _policyRepository = policyRepository;
        _customerRepository = customerRepository;
        _pricingEngine = pricingEngine;
        _currentUserService = currentUserService;
    }

    public async Task<PolicyDetailDto> Handle(GetPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        var policy = await _policyRepository.GetReadDetailByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Policy), request.PolicyId);

        // Kaynak sahipliği: müşteri yalnızca kendi poliçesini görür; Admin/Personel muaf (QuoteAuthorization).
        await QuoteAuthorization.EnsureCanAccessAsync(
            policy.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        var quote = policy.Quote!;

        // Teminatlar (ölçekli limitler), poliçenin kaynaklandığı teklifin saklanan seçiminden (CoveragePackage)
        // teklifin oluşturma referansıyla (CreatedAt) deterministik yeniden hesaplanır — teklif detayı ve poliçe
        // PDF'iyle birebir tutarlıdır (ADR-021).
        var pricing = QuotePricingFactory.Compute(
            _pricingEngine,
            quote.Branch,
            policy.Customer!,
            quote.Vehicle,
            quote.Property,
            quote.InsuranceProduct!.Coverages,
            quote.CoveragePackage,
            quote.CreatedAt,
            quote.ClaimHistoryFactor);

        return PolicyMappings.ToDetailDto(policy, pricing);
    }
}
