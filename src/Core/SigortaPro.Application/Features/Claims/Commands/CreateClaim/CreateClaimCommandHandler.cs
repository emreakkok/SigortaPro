using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Claims.Commands.CreateClaim;

public sealed class CreateClaimCommandHandler : ICommandHandler<CreateClaimCommand, ClaimDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly IClaimRepository _claimRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateClaimCommandHandler> _logger;

    public CreateClaimCommandHandler(
        ICustomerRepository customerRepository,
        IPolicyRepository policyRepository,
        IClaimRepository claimRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CreateClaimCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _policyRepository = policyRepository;
        _claimRepository = claimRepository;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClaimDto> Handle(CreateClaimCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var policy = await _policyRepository.GetByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Policy), request.PolicyId);

        // Kaynak sahipliği: müşteri yalnızca kendi poliçesine hasar açabilir (uç zaten yalnızca Customer'a açık).
        if (policy.CustomerId != customer.Id)
        {
            throw new ForbiddenAccessException();
        }

        // İş kuralı (TASKS.md Task 12): yalnızca aktif poliçeye, poliçe dönemi içindeki bir olaya hasar açılabilir.
        if (policy.Status != PolicyStatus.Active)
        {
            throw new BusinessRuleException("Yalnızca aktif poliçe için hasar bildirilebilir.");
        }

        if (request.IncidentDate < policy.StartDate || request.IncidentDate > policy.EndDate)
        {
            throw new BusinessRuleException("Olay tarihi poliçe döneminin dışında olamaz.");
        }

        if (request.IncidentDate > now)
        {
            throw new BusinessRuleException("Olay tarihi gelecekte olamaz.");
        }

        var claim = new Claim(policy.Id, customer.Id, request.IncidentDate, request.Description, request.EstimatedAmount);

        await _claimRepository.AddAsync(claim, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Mock foto yükleme: gerçek depolama MVP dışı olduğundan (PROJECT_CONTEXT §9) yalnızca kabul edilip loglanır.
        if (request.PhotoFileNames is { Count: > 0 } photos)
        {
            _logger.LogInformation(
                "Hasar bildirimi için {PhotoCount} foto alındı (mock; saklanmaz). ClaimId: {ClaimId}",
                photos.Count, claim.Id);
        }

        _logger.LogInformation(
            "Hasar bildirildi. ClaimId: {ClaimId}, PolicyId: {PolicyId}, CustomerId: {CustomerId}",
            claim.Id, policy.Id, customer.Id);

        return ClaimMappings.ToDto(claim, policy.PolicyNumber);
    }
}
