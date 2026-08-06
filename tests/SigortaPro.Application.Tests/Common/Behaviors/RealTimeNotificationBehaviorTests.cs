using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Behaviors;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Common.Notifications;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Auth.Commands.Register;
using SigortaPro.Application.Features.Auth.DTOs;
using SigortaPro.Application.Features.Claims.Commands.CreateClaim;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Common.Behaviors;

public class RealTimeNotificationBehaviorTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly string[] CustomerRoles = ["Customer"];

    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly INotificationContextResolver _contextResolver = Substitute.For<INotificationContextResolver>();

    public RealTimeNotificationBehaviorTests()
    {
        // Varsayılan bağlam: müşteri kendi işlemini yapıyor (personel değil).
        _contextResolver.ResolveActorAsync(Arg.Any<CancellationToken>())
            .Returns(new NotificationActor(Guid.NewGuid(), "Mehmet Demir", false));
        _contextResolver.GetCustomerNameAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns("Mehmet Demir");
    }

    private RealTimeNotificationBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>()
        where TRequest : notnull =>
        new(_dispatcher, _contextResolver,
            Substitute.For<ILogger<RealTimeNotificationBehavior<TRequest, TResponse>>>());

    private static RegisterCommand SampleRegisterCommand() => new(
        "kullanici@ornek.com", "Gecerli!2345", "Ahmet", "Yilmaz", "10000000146",
        new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc), "+905321234567",
        "Ankara", "Cankaya", "Kizilay", "06420");

    private static QuoteDto SampleQuote(QuoteInsuredPersonDto? insuredPerson = null) => new(
        Guid.NewGuid(),
        CustomerId,
        InsuranceBranch.Saglik,
        "Sağlık Sigortası",
        QuoteStatus.Priced,
        CoveragePackage.Standart,
        RiskScore.Low,
        1000m,
        1850m,
        null,
        DateTime.UtcNow,
        new QuoteRiskObjectDto("person", "Sigortalı"),
        Array.Empty<QuoteCoverageDto>(),
        Array.Empty<PricingBreakdownItem>(),
        insuredPerson);

    [Fact]
    public async Task Handle_Should_NotifyStaff_When_QuoteIsCreated()
    {
        var behavior = CreateBehavior<CreateQuoteCommand, QuoteDto>();
        var command = new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart);
        var quote = SampleQuote();

        await behavior.Handle(command, () => Task.FromResult(quote), CancellationToken.None);

        await _dispatcher.Received(1).PublishToStaffAsync(
            Arg.Is<RealTimeNotification>(n => n.Type == "quote-created"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_EnrichQuoteNotification_WithCustomerAmountAndNavigation()
    {
        // bildirim "kim/kimin için/ne/hangi kayıt" sorularını tek başına cevaplamalıdır.
        var behavior = CreateBehavior<CreateQuoteCommand, QuoteDto>();
        var command = new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart);
        var quote = SampleQuote();

        await behavior.Handle(command, () => Task.FromResult(quote), CancellationToken.None);

        await _dispatcher.Received(1).PublishToStaffAsync(
            Arg.Is<RealTimeNotification>(n =>
                n.Message.Contains("Mehmet Demir")
                && n.Message.Contains("Sağlık Sigortası")
                && n.Message.Contains("1.850,00")
                && n.ActorName == "Mehmet Demir"
                && n.RelatedEntityId == quote.Id
                && n.RelatedEntityType == "Quote"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DistinguishActorAndCustomer_When_StaffCreatesQuoteOnBehalf()
    {
        // Personel bir müşteri adına işlem yaptığında "kim yaptı" ile "kimin için" ayrışmalıdır.
        _contextResolver.ResolveActorAsync(Arg.Any<CancellationToken>())
            .Returns(new NotificationActor(Guid.NewGuid(), "personel@sigortapro.com", true));
        var behavior = CreateBehavior<CreateQuoteCommand, QuoteDto>();
        var command = new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart);

        await behavior.Handle(command, () => Task.FromResult(SampleQuote()), CancellationToken.None);

        await _dispatcher.Received(1).PublishToStaffAsync(
            Arg.Is<RealTimeNotification>(n =>
                n.Message.Contains("personel@sigortapro.com tarafından")
                && n.Message.Contains("Mehmet Demir adına")
                && n.ActorName == "personel@sigortapro.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NameInsuredPerson_ButNeverLeakTckn_When_HealthQuoteIsForSomeoneElse()
    {
        // KVKK: sigortalının adı operasyon için taşınır; TCKN (maskeli dahi olsa) bildirime yazılmaz.
        var behavior = CreateBehavior<CreateQuoteCommand, QuoteDto>();
        var command = new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart);
        var insured = new QuoteInsuredPersonDto("Ali Demir", "123******78", new DateTime(2000, 5, 5, 0, 0, 0, DateTimeKind.Utc), "Çocuk");

        await behavior.Handle(command, () => Task.FromResult(SampleQuote(insured)), CancellationToken.None);

        await _dispatcher.Received(1).PublishToStaffAsync(
            Arg.Is<RealTimeNotification>(n =>
                n.Message.Contains("Ali Demir") && !n.Message.Contains("123******78")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_MakeClaimNotificationNavigable_When_ClaimIsCreated()
    {
        // öncesi hasar bildirimlerinde RelatedEntityId set edilmiyordu → tıkla-git yapılamıyordu.
        var behavior = CreateBehavior<CreateClaimCommand, ClaimDto>();
        var command = new CreateClaimCommand(Guid.NewGuid(), DateTime.UtcNow, "Çarpma", 5000m);
        var claim = new ClaimDto(
            Guid.NewGuid(), Guid.NewGuid(), "POL-2026-000123", CustomerId,
            DateTime.UtcNow, "Çarpma", 5000m, null, ClaimStatus.Submitted, null, DateTime.UtcNow,
            Array.Empty<ClaimDocumentDto>());

        await behavior.Handle(command, () => Task.FromResult(claim), CancellationToken.None);

        await _dispatcher.Received(1).PublishToStaffAsync(
            Arg.Is<RealTimeNotification>(n =>
                n.Type == "claim-created"
                && n.RelatedEntityId == claim.Id
                && n.RelatedEntityType == "Claim"
                && n.ReferenceCode == "POL-2026-000123"
                && n.Message.Contains("Mehmet Demir")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotifyStaff_When_RegisterSucceeds()
    {
        var behavior = CreateBehavior<RegisterCommand, Result<AuthResponse>>();
        var success = Result<AuthResponse>.Success(new AuthResponse(
            Guid.NewGuid(), "kullanici@ornek.com", CustomerRoles,
            "access", DateTime.UtcNow, "refresh", DateTime.UtcNow));

        await behavior.Handle(SampleRegisterCommand(), () => Task.FromResult(success), CancellationToken.None);

        await _dispatcher.Received(1).PublishToStaffAsync(
            Arg.Is<RealTimeNotification>(n =>
                n.Type == "customer-registered"
                && n.Message.Contains("Ahmet Yilmaz")
                && n.ActorName == "Ahmet Yilmaz"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotNotify_When_RegisterSoftFails()
    {
        var behavior = CreateBehavior<RegisterCommand, Result<AuthResponse>>();
        var failure = Result<AuthResponse>.Failure("Bu TCKN ile daha önce bir kayıt oluşturulmuş.");

        await behavior.Handle(SampleRegisterCommand(), () => Task.FromResult(failure), CancellationToken.None);

        await _dispatcher.DidNotReceiveWithAnyArgs().PublishToStaffAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_NotNotify_When_RequestIsUnmapped()
    {
        var behavior = CreateBehavior<string, int>();

        await behavior.Handle("herhangi-bir-istek", () => Task.FromResult(42), CancellationToken.None);

        await _dispatcher.DidNotReceiveWithAnyArgs().PublishToStaffAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnResponse_When_NotifierThrows()
    {
        // Bildirim bir yan kanaldır: yayın hatası tamamlanan iş operasyonunun sonucunu bozamaz.
        _dispatcher.PublishToStaffAsync(Arg.Any<RealTimeNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("hub kapalı")));
        var behavior = CreateBehavior<CreateQuoteCommand, QuoteDto>();
        var command = new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart);

        var act = () => behavior.Handle(command, () => Task.FromResult(SampleQuote()), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_Should_ReturnResponse_When_ContextResolutionThrows()
    {
        // Bağlam çözümü de yan kanaldır (DB okuması) — hatası iş sonucunu bozmamalıdır.
        _contextResolver.ResolveActorAsync(Arg.Any<CancellationToken>())
            .Returns<Task<NotificationActor>>(_ => throw new InvalidOperationException("veritabanı yok"));
        var behavior = CreateBehavior<CreateQuoteCommand, QuoteDto>();
        var command = new CreateQuoteCommand(InsuranceBranch.Saglik, null, null, CoveragePackage.Standart);
        var quote = SampleQuote();

        var result = await behavior.Handle(command, () => Task.FromResult(quote), CancellationToken.None);

        result.Should().BeSameAs(quote);
    }
}
