using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Constants;
using SigortaPro.Domain.Entities;
using SigortaPro.Application.Common.Exceptions;

namespace SigortaPro.Application.Features.Quotes.Commands.CreateQuote;

public sealed class CreateQuoteCommandHandler : ICommandHandler<CreateQuoteCommand, QuoteDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IInsuranceProductRepository _productRepository;
    private readonly IReadRepository<Vehicle> _vehicleRepository;
    private readonly IReadRepository<Property> _propertyRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IPricingEngine _pricingEngine;
    private readonly IPricingRateResolver _pricingRateResolver;
    private readonly IQuotePricingInputBuilder _pricingInputBuilder;
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
        IPricingRateResolver pricingRateResolver,
        IQuotePricingInputBuilder pricingInputBuilder,
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
        _pricingRateResolver = pricingRateResolver;
        _pricingInputBuilder = pricingInputBuilder;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<QuoteDto> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        // Self-service (CustomerId null) → oturum sahibi müşteri; acente destekli (CustomerId dolu) → hedef
        // müşteri, yalnızca personel çağrısıyla (TargetCustomerResolver staff-only guard uygular).
        var customer = await TargetCustomerResolver.ResolveTrackedAsync(
            request.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        var product = await _productRepository.GetActiveByBranchAsync(request.Branch, cancellationToken)
            ?? throw new NotFoundException(nameof(InsuranceProduct), request.Branch);

        var (vehicle, property) = await QuoteRiskObjectResolver.ResolveAsync(
            request.Branch, request.VehicleId, request.PropertyId, customer.Id,
            _vehicleRepository, _propertyRepository, cancellationToken);

        var now = _dateTimeProvider.UtcNow;

        // ADR-048: Yeni teklif her zaman O AN yürürlükte olan tarifeyle fiyatlanır ve bu versiyon teklifte
        // sabitlenir; sonraki tarife değişiklikleri bu teklifin primini/dökümünü etkileyemez.
        var effectivePricing = await _pricingRateResolver.ResolveEffectiveAsync(now, cancellationToken);

        // ADR-053/056: Fiyatlama girdileri teklif oluşturulurken DONDURULUR. Girdi, karşılaştırma
        // önizlemesiyle AYNI builder'dan kurulur → önizlemede gösterilen fiyat ile oluşan teklifin fiyatı
        // yapısal olarak eşittir. Sonraki tüm yeniden hesaplar da bu snapshot'tan okur.
        var snapshot = await _pricingInputBuilder.BuildAsync(
            request.Branch, customer, vehicle, property, now,
            insuredBirthDate: request.InsuredPerson?.BirthDate,
            isSmoker: request.IsSmoker,
            cancellationToken: cancellationToken);

        var pricing = QuotePricingFactory.Compute(
            _pricingEngine, request.Branch, customer, vehicle, property,
            product.Coverages, request.CoveragePackage, now,
            insuredBirthDate: request.InsuredPerson?.BirthDate,
            rates: effectivePricing.Rates,
            snapshot: snapshot);

        // Acente destekli ise teklifin "üreten personel"i (agent of record) sabitlenir; sahibi yine müşteridir.
        var producingStaffId = TargetCustomerResolver.ResolveProducingStaffId(request.CustomerId, _currentUserService);

        var quote = new Quote(customer.Id, product.Id, request.Branch, vehicle?.Id, property?.Id, producingStaffId);
        quote.SelectCoveragePackage(request.CoveragePackage);
        quote.CapturePricingSnapshot(snapshot);

        if (effectivePricing.VersionId is not null)
        {
            quote.PinPricingVersion(effectivePricing.VersionId.Value);
        }

        if (request.InsuredPerson is not null)
        {
            quote.SetInsuredPerson(new InsuredPerson(
                request.InsuredPerson.FirstName,
                request.InsuredPerson.LastName,
                request.InsuredPerson.Tckn,
                request.InsuredPerson.BirthDate,
                request.InsuredPerson.PhoneNumber,
                request.InsuredPerson.Relationship));
        }

        quote.MarkAsPriced(pricing.TotalPremium, now.AddDays(BusinessConstants.MaxQuoteValidityDays));

        await _quoteRepository.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teklif oluşturuldu. QuoteId: {QuoteId}, CustomerId: {CustomerId}, Branş: {Branch}, Prim: {Premium}, ÜretenPersonel: {StaffId}",
            quote.Id, customer.Id, request.Branch, quote.TotalPremium, producingStaffId);

        return QuoteMappings.ToDto(quote, product, vehicle, property, pricing);
    }
}
