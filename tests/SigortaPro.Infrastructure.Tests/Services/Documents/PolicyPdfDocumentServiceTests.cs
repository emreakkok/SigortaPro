using System.Text;
using FluentAssertions;
using QuestPDF.Infrastructure;
using SigortaPro.Application.Common.Documents;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;
using SigortaPro.Infrastructure.Services.Documents;

namespace SigortaPro.Infrastructure.Tests.Services.Documents;

public class PolicyPdfDocumentServiceTests
{
    private readonly PolicyPdfDocumentService _service = new();

    public PolicyPdfDocumentServiceTests()
    {
        // Lisans normalde DI kaydında ayarlanır; birim testte render öncesi ayrıca ayarlanır.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static PolicyDocumentModel SampleModel() => new(
        AgencyName: "SigortaPro Sigorta Acentesi",
        AgencyAddress: "Levent Mah. No:1, İstanbul",
        AgencyContact: "0212 000 00 00",
        PolicyNumber: "POL-2026-000002",
        PolicyStatus: PolicyStatus.Active,
        StartDate: new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
        EndDate: new DateTime(2027, 7, 10, 0, 0, 0, DateTimeKind.Utc),
        IssuedAt: new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
        CustomerFullName: "Ayşe Yılmaz",
        CustomerMaskedTckn: "*********10",
        CustomerPhone: "+905551112233",
        CustomerAddress: "Caferağa Kadıköy/İstanbul 34710",
        Branch: InsuranceBranch.Kasko,
        ProductName: "Kasko Sigortası",
        CoveragePackage: CoveragePackage.Standart,
        RiskObjectKind: "Araç",
        RiskObjectDisplay: "34 ABC 123 · Toyota Corolla (2022)",
        RiskScore: RiskScore.Medium,
        BasePremium: 15000m,
        TotalPremium: 20625m,
        Coverages: new List<PolicyCoverageLine>
        {
            new("Çarpma/Çarpışma", "Aracın çarpışma hasarları", 300000m),
            new("Hırsızlık", "Aracın çalınması", 300000m),
        },
        PremiumBreakdown: new List<PricingBreakdownItem>
        {
            new("Motor Gücü", 1.10m, "Orta motor gücü (101-160 HP)."),
            new("İl Risk Katsayısı", 1.25m, "İstanbul ili risk katsayısı."),
        });

    [Fact]
    public void Generate_Should_ReturnNonEmptyPdf()
    {
        var bytes = _service.Generate(SampleModel());

        bytes.Should().NotBeNullOrEmpty();
        // PDF dosya imzası (magic number) "%PDF".
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void Generate_Should_ProduceStablePdf_ForSameModel()
    {
        var first = _service.Generate(SampleModel());
        var second = _service.Generate(SampleModel());

        // Aynı model için üretilen belge boyutu deterministik olmalı.
        second.Length.Should().Be(first.Length);
    }
}
