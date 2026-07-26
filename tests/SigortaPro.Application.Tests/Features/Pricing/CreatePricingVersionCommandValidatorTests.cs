using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Pricing;

// ADR-048: Geçersiz tarife kabul edilmez; en kritik kural geçmişe tarihlemenin yasak olmasıdır.
public class CreatePricingVersionCommandValidatorTests
{
    private static readonly DateTime Now = new(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);

    private readonly CreatePricingVersionCommandValidator _validator;

    public CreatePricingVersionCommandValidatorTests()
    {
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(Now);
        _validator = new CreatePricingVersionCommandValidator(dateTimeProvider);
    }

    private static List<BranchRateInput> AllBranches(decimal basePremium = 10000m) =>
        Enum.GetValues<InsuranceBranch>().Select(branch => new BranchRateInput(branch, basePremium)).ToList();

    [Fact]
    public void Should_Reject_BackdatedEffectiveFrom()
    {
        // Geçmişe tarihleme, admin'in geçmiş teklifleri etkileyebileceği yanılgısını doğurur → yasak.
        var command = new CreatePricingVersionCommand(Now.AddDays(-1), null, AllBranches());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.EffectiveFrom));
    }

    [Fact]
    public void Should_Accept_ImmediateEffectiveFrom()
    {
        var command = new CreatePricingVersionCommand(Now, "Enflasyon güncellemesi", AllBranches());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Accept_FutureEffectiveFrom()
    {
        var command = new CreatePricingVersionCommand(Now.AddDays(30), null, AllBranches());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Should_Reject_NonPositiveBasePremium(decimal basePremium)
    {
        var rates = AllBranches();
        rates[0] = new BranchRateInput(rates[0].Branch, basePremium);
        var command = new CreatePricingVersionCommand(Now, null, rates);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Reject_AbsurdlyLargeBasePremium()
    {
        var rates = AllBranches();
        rates[0] = new BranchRateInput(rates[0].Branch, 99_000_000m);
        var command = new CreatePricingVersionCommand(Now, null, rates);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Reject_PartialTariff()
    {
        var command = new CreatePricingVersionCommand(
            Now, null, [new BranchRateInput(InsuranceBranch.Kasko, 15000m)]);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Reject_DuplicateBranch()
    {
        var rates = AllBranches();
        rates.Add(new BranchRateInput(InsuranceBranch.Kasko, 20000m));
        var command = new CreatePricingVersionCommand(Now, null, rates);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Reject_EmptyTariff()
    {
        var command = new CreatePricingVersionCommand(Now, null, []);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
