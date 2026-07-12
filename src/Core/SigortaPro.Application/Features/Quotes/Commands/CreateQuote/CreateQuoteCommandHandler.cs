using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Constants;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Quotes.Commands.CreateQuote;

public sealed class CreateQuoteCommandHandler : ICommandHandler<CreateQuoteCommand, QuoteDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IInsuranceProductRepository _productRepository;
    private readonly IReadRepository<Vehicle> _vehicleRepository;
    private readonly IReadRepository<Property> _propertyRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IPricingEngine _pricingEngine;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateQuoteCommandHandler> _logger;

    public CreateQuoteCommandHandler(
        ICustomerRepository customerRepository,
        IInsuranceProductRepository productRepository,
        IReadRepository<Vehicle> vehicleRepository,
        IReadRepository<Property> propertyRepository,
        IQuoteRepository quoteRepository,
        IPricingEngine pricingEngine,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CreateQuoteCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _vehicleRepository = vehicleRepository;
        _propertyRepository = propertyRepository;
        _quoteRepository = quoteRepository;
        _pricingEngine = pricingEngine;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<QuoteDto> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var product = await _productRepository.GetActiveByBranchAsync(request.Branch, cancellationToken)
            ?? throw new NotFoundException(nameof(InsuranceProduct), request.Branch);

        var (vehicle, property) = await QuoteRiskObjectResolver.ResolveAsync(
            request.Branch, request.VehicleId, request.PropertyId, customer.Id,
            _vehicleRepository, _propertyRepository, cancellationToken);

        var now = _dateTimeProvider.UtcNow;

        var pricing = QuotePricingFactory.Compute(
            _pricingEngine, request.Branch, customer, vehicle, property,
            product.Coverages, request.CoveragePackage, now);

        var quote = new Quote(customer.Id, product.Id, request.Branch, vehicle?.Id, property?.Id);
        quote.SelectCoveragePackage(request.CoveragePackage);
        quote.MarkAsPriced(pricing.TotalPremium, now.AddDays(BusinessConstants.MaxQuoteValidityDays));

        await _quoteRepository.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teklif oluşturuldu. QuoteId: {QuoteId}, CustomerId: {CustomerId}, Branş: {Branch}, Prim: {Premium}",
            quote.Id, customer.Id, request.Branch, quote.TotalPremium);

        return QuoteMappings.ToDto(quote, product, vehicle, property, pricing);
    }
}
