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
    private readonly IFileStorageService _fileStorage;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateClaimCommandHandler> _logger;

    public CreateClaimCommandHandler(
        ICustomerRepository customerRepository,
        IPolicyRepository policyRepository,
        IClaimRepository claimRepository,
        IFileStorageService fileStorage,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CreateClaimCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _policyRepository = policyRepository;
        _claimRepository = claimRepository;
        _fileStorage = fileStorage;
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

        // Saat hassasiyetli teminat penceresi (StartDate/EndDate satın alma anını taşır): aynı gün, poliçe
        // başlangıç saatinden sonraki olay geçerlidir; önceki olay değildir. Kural Policy aggregate'indedir.
        if (!policy.CoversIncidentAt(request.IncidentDate))
        {
            throw new BusinessRuleException("Olay tarihi poliçe döneminin dışında olamaz.");
        }

        if (request.IncidentDate > now)
        {
            throw new BusinessRuleException("Olay tarihi gelecekte olamaz.");
        }

        var claim = new Claim(policy.Id, customer.Id, request.IncidentDate, request.Description, request.EstimatedAmount);

        // Belgeler (foto/PDF): baytlar dosya depolamaya yazılır (ADR-023), metadata aggregate'e eklenir.
        // Doğrulama (adet/boyut/tür) CreateClaimCommandValidator'dadır; burada kalıcılaştırılır.
        if (request.Documents is { Count: > 0 } documents)
        {
            foreach (var upload in documents)
            {
                var documentId = Guid.NewGuid();
                var storageKey = ClaimDocumentStorage.KeyFor(claim.Id, documentId);
                await _fileStorage.SaveAsync(storageKey, upload.Content, cancellationToken);

                claim.AddDocument(new ClaimDocument(
                    documentId, claim.Id, upload.FileName, upload.ContentType, upload.Content.LongLength, storageKey));
            }
        }

        await _claimRepository.AddAsync(claim, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Hasar bildirildi. ClaimId: {ClaimId}, PolicyId: {PolicyId}, CustomerId: {CustomerId}, Belge: {DocumentCount}",
            claim.Id, policy.Id, customer.Id, claim.Documents.Count);

        return ClaimMappings.ToDto(claim, policy.PolicyNumber);
    }
}
