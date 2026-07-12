using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Policies.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Policies.Queries.GetPolicyDocument;

// Poliçe belgesini indirir; ilk erişimde yoksa üretip saklar (lazy, idempotent — ADR-023).
public sealed class GetPolicyDocumentQueryHandler : IQueryHandler<GetPolicyDocumentQuery, PolicyDocumentFileDto>
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IWriteRepository<PolicyDocument> _policyDocumentRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPolicyDocumentService _documentService;
    private readonly IFileStorageService _fileStorage;
    private readonly IPricingEngine _pricingEngine;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPolicyDocumentQueryHandler> _logger;

    public GetPolicyDocumentQueryHandler(
        IPolicyRepository policyRepository,
        IWriteRepository<PolicyDocument> policyDocumentRepository,
        ICustomerRepository customerRepository,
        IPolicyDocumentService documentService,
        IFileStorageService fileStorage,
        IPricingEngine pricingEngine,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<GetPolicyDocumentQueryHandler> logger)
    {
        _policyRepository = policyRepository;
        _policyDocumentRepository = policyDocumentRepository;
        _customerRepository = customerRepository;
        _documentService = documentService;
        _fileStorage = fileStorage;
        _pricingEngine = pricingEngine;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PolicyDocumentFileDto> Handle(GetPolicyDocumentQuery request, CancellationToken cancellationToken)
    {
        var policy = await _policyRepository.GetDetailByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Policy), request.PolicyId);

        // Kaynak sahipliği: müşteri yalnızca kendi poliçesini indirir; Admin/Personel muaf (QuoteAuthorization).
        await QuoteAuthorization.EnsureCanAccessAsync(
            policy.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        // Belge kaydı yoksa üret, sakla ve poliçeye ekle (ilk indirmede).
        if (policy.PolicyDocument is null)
        {
            var key = PolicyDocumentStorage.KeyFor(policy.Id);
            var fileName = PolicyDocumentStorage.FileNameFor(policy.PolicyNumber);
            var content = await GenerateAndStore(policy, key, cancellationToken);

            // Yeni belge kaydı explicit AddAsync ile eklenir (durumu Added olur → INSERT). Salt navigation
            // üzerinden bırakılırsa, istemci-üretimli Guid anahtarı nedeniyle EF onu mevcut kayıt sanıp
            // Modified'e işaretler ve INSERT yerine 0-satır UPDATE'e düşer. AttachDocument aggregate tutarlılığı içindir.
            var document = new PolicyDocument(policy.Id, key, fileName, _dateTimeProvider.UtcNow);
            policy.AttachDocument(document);
            await _policyDocumentRepository.AddAsync(document, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Poliçe belgesi üretildi. PolicyId: {PolicyId}, PoliçeNo: {PolicyNumber}",
                policy.Id, policy.PolicyNumber);

            return new PolicyDocumentFileDto(content, fileName, PolicyDocumentStorage.PdfContentType);
        }

        // Kayıt var: saklanan dosyayı döndür. Dosya diskte yoksa (ör. temizlenmiş) aynı anahtara yeniden üret.
        var stored = await _fileStorage.ReadAsync(policy.PolicyDocument.FilePath, cancellationToken)
            ?? await GenerateAndStore(policy, policy.PolicyDocument.FilePath, cancellationToken);

        return new PolicyDocumentFileDto(
            stored, policy.PolicyDocument.FileName, PolicyDocumentStorage.PdfContentType);
    }

    private async Task<byte[]> GenerateAndStore(Policy policy, string key, CancellationToken cancellationToken)
    {
        var model = PolicyDocumentModelFactory.Build(policy, _pricingEngine, _dateTimeProvider.UtcNow);
        var content = _documentService.Generate(model);
        await _fileStorage.SaveAsync(key, content, cancellationToken);
        return content;
    }
}
