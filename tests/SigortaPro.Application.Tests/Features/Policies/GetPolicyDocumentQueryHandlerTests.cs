using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Documents;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Policies.Queries.GetPolicyDocument;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Policies;

public class GetPolicyDocumentQueryHandlerTests
{
    private readonly IPolicyRepository _policyRepository = Substitute.For<IPolicyRepository>();
    private readonly IWriteRepository<PolicyDocument> _policyDocumentRepository = Substitute.For<IWriteRepository<PolicyDocument>>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IPolicyDocumentService _documentService = Substitute.For<IPolicyDocumentService>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly IPricingEngine _pricingEngine = Substitute.For<IPricingEngine>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetPolicyDocumentQueryHandler _handler;

    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Customer _customer;
    private readonly DateTime _now = new(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);

    public GetPolicyDocumentQueryHandlerTests()
    {
        _customer = CustomerTestData.CreateCustomer(_appUserId, Guid.NewGuid());
        _currentUserService.UserId.Returns(_appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(_appUserId, Arg.Any<CancellationToken>()).Returns(_customer);
        _dateTimeProvider.UtcNow.Returns(_now);

        _handler = new GetPolicyDocumentQueryHandler(
            _policyRepository, _policyDocumentRepository, _customerRepository, _documentService, _fileStorage,
            _pricingEngine, _dateTimeProvider, _currentUserService, _unitOfWork,
            Substitute.For<ILogger<GetPolicyDocumentQueryHandler>>());
    }

    private Policy PolicyWithDocument()
    {
        var policy = new Policy("POL-2026-000002", _customer.Id, Guid.NewGuid(), _now, _now.AddYears(1), 20625m);
        policy.AttachDocument(new PolicyDocument(policy.Id, "policy-documents/x.pdf", "POL-2026-000002.pdf", _now));
        return policy;
    }

    [Fact]
    public async Task Handle_Should_ReturnStoredFile_When_DocumentAlreadyExists()
    {
        var policy = PolicyWithDocument();
        var stored = new byte[] { 9, 9, 9 };
        _policyRepository.GetDetailByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);
        _fileStorage.ReadAsync("policy-documents/x.pdf", Arg.Any<CancellationToken>()).Returns(stored);

        var result = await _handler.Handle(new GetPolicyDocumentQuery(policy.Id), CancellationToken.None);

        result.FileName.Should().Be("POL-2026-000002.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.Content.Should().Equal(stored);

        // Belge zaten var: yeniden üretilmez, yeni kayıt oluşmaz.
        _documentService.DidNotReceive().Generate(Arg.Any<PolicyDocumentModel>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_PolicyBelongsToAnotherCustomer()
    {
        var policy = new Policy("POL-2026-000003", Guid.NewGuid(), Guid.NewGuid(), _now, _now.AddYears(1), 100m);
        policy.AttachDocument(new PolicyDocument(policy.Id, "policy-documents/y.pdf", "POL-2026-000003.pdf", _now));
        _policyRepository.GetDetailByIdAsync(policy.Id, Arg.Any<CancellationToken>()).Returns(policy);

        var act = () => _handler.Handle(new GetPolicyDocumentQuery(policy.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _fileStorage.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_PolicyMissing()
    {
        _policyRepository.GetDetailByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Policy?)null);

        var act = () => _handler.Handle(new GetPolicyDocumentQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
