using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SigortaPro.Application.Common.Documents;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Infrastructure.Services.Documents;

// QuestPDF ile poliçe sertifikası üretimi. Saf render — DB/dosya I/O yapmaz, modelden PDF baytları üretir.
public sealed class PolicyPdfDocumentService : IPolicyDocumentService
{
    private static readonly CultureInfo Tr = new("tr-TR");

    // Kurumsal palet — web temasıyla (globals.css token'ları) aynı kimlik.
    // Bordo (primary), koyu gri (metin), nötr gri (ikincil) ve kırık beyaz (zemin).
    private static readonly Color Accent = Color.FromHex("#7A1F2B");      // bordo / koyu kırmızı
    private static readonly Color DarkText = Color.FromHex("#26262B");    // koyu gri (gövde metni)
    private static readonly Color Muted = Color.FromHex("#6E6E76");       // nötr gri (ikincil metin)
    private static readonly Color LightBg = Color.FromHex("#F7F5F2");     // kırık beyaz (kutu zeminleri)
    private static readonly Color BorderGray = Color.FromHex("#DCDAD5");  // açık gri (çizgi/çerçeve)

    public byte[] Generate(PolicyDocumentModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(DarkText));

                page.Header().Element(header => ComposeHeader(header, model));
                page.Content().PaddingVertical(12).Element(content => ComposeContent(content, model));
                page.Footer().Element(footer => ComposeFooter(footer, model));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, PolicyDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(model.AgencyName).FontSize(16).Bold().FontColor(Accent);
                    left.Item().Text(model.AgencyAddress).FontSize(8).FontColor(Muted);
                });

                row.ConstantItem(200).Column(right =>
                {
                    right.Item().AlignRight().Text("POLİÇE SERTİFİKASI").FontSize(14).Bold().FontColor(Accent);
                    right.Item().AlignRight().Text($"Poliçe No: {model.PolicyNumber}").FontSize(10).Bold();
                    right.Item().AlignRight().Text($"Düzenlenme: {Date(model.IssuedAt)}").FontSize(8).FontColor(Muted);
                });
            });

            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Accent);
        });
    }

    private static void ComposeContent(IContainer container, PolicyDocumentModel model)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            column.Item().Row(row =>
            {
                row.Spacing(12);
                row.RelativeItem().Element(box => InfoBox(box, "POLİÇE BİLGİLERİ", new[]
                {
                    ("Branş", BranchLabel(model.Branch)),
                    ("Ürün", model.ProductName),
                    ("Teminat Paketi", PackageLabel(model.CoveragePackage)),
                    ("Durum", StatusLabel(model.PolicyStatus)),
                    ("Başlangıç", Date(model.StartDate)),
                    ("Bitiş", Date(model.EndDate)),
                }));

                // "Başkası adına" poliçede sigorta ettiren ile sigortalı ayrı gösterilir;
                // sigortalı poliçe sahibinin kendisiyse kutu başlığı birleşik kalır.
                row.RelativeItem().Element(box => InfoBox(
                    box,
                    model.InsuredPersonSummary is null ? "SİGORTA ETTİREN / SİGORTALI" : "SİGORTA ETTİREN",
                    BuildPolicyholderRows(model)));
            });

            column.Item().Element(section => CoverageTable(section, model));
            column.Item().Element(section => PremiumTable(section, model));
        });
    }

    // Sigorta ettiren satırları; "başkası adına" poliçede sigortalı ayrı satır olarak eklenir.
    private static (string Label, string Value)[] BuildPolicyholderRows(PolicyDocumentModel model)
    {
        var rows = new List<(string Label, string Value)>
        {
            ("Ad Soyad", model.CustomerFullName),
            ("TCKN", model.CustomerMaskedTckn),
            ("Telefon", model.CustomerPhone),
            ("Adres", model.CustomerAddress),
        };

        if (model.InsuredPersonSummary is not null)
        {
            rows.Add(("Sigortalı", model.InsuredPersonSummary));
        }

        rows.Add((model.RiskObjectKind, model.RiskObjectDisplay));
        rows.Add(("Risk Skoru", RiskLabel(model.RiskScore)));
        return rows.ToArray();
    }

    private static void InfoBox(IContainer container, string title, IEnumerable<(string Label, string Value)> rows)
    {
        container.Border(1).BorderColor(BorderGray).Column(column =>
        {
            column.Item().Background(LightBg).PaddingHorizontal(8).PaddingVertical(4)
                .Text(title).Bold().FontSize(9).FontColor(Accent);

            column.Item().PaddingHorizontal(8).PaddingVertical(6).Column(body =>
            {
                body.Spacing(3);
                foreach (var (label, value) in rows)
                {
                    body.Item().Row(row =>
                    {
                        row.ConstantItem(90).Text(label).FontSize(8).FontColor(Muted);
                        row.RelativeItem().Text(value).FontSize(9);
                    });
                }
            });
        });
    }

    private static void CoverageTable(IContainer container, PolicyDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(4).Text("TEMİNATLAR").Bold().FontSize(10).FontColor(Accent);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(5);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "Teminat");
                    HeaderCell(header, "Açıklama");
                    HeaderCell(header, "Limit", alignRight: true);
                });

                foreach (var coverage in model.Coverages)
                {
                    BodyCell(table, coverage.Name);
                    BodyCell(table, coverage.Description, Muted);
                    BodyCell(table, Money(coverage.Limit), alignRight: true);
                }
            });
        });
    }

    private static void PremiumTable(IContainer container, PolicyDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(4).Text("PRİM DÖKÜMÜ").Bold().FontSize(10).FontColor(Accent);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(6);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "Faktör");
                    HeaderCell(header, "Çarpan", alignRight: true);
                    HeaderCell(header, "Açıklama");
                });

                foreach (var item in model.PremiumBreakdown)
                {
                    BodyCell(table, item.Factor);
                    BodyCell(table, item.Multiplier.ToString("0.00", Tr), alignRight: true);
                    BodyCell(table, item.Description, Muted);
                }
            });

            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(220).Border(1).BorderColor(BorderGray).Column(box =>
                {
                    SummaryRow(box, "Baz Prim", Money(model.BasePremium));
                    SummaryRow(box, "Risk Skoru", RiskLabel(model.RiskScore));
                    box.Item().Background(Accent).PaddingHorizontal(8).PaddingVertical(6).Row(total =>
                    {
                        total.RelativeItem().Text("TOPLAM PRİM").Bold().FontSize(10).FontColor(Colors.White);
                        total.RelativeItem().AlignRight().Text(Money(model.TotalPremium)).Bold().FontSize(11).FontColor(Colors.White);
                    });
                });
            });
        });
    }

    private static void SummaryRow(ColumnDescriptor column, string label, string value)
    {
        column.Item().PaddingHorizontal(8).PaddingVertical(4).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(9).FontColor(Muted);
            row.RelativeItem().AlignRight().Text(value).FontSize(9);
        });
    }

    private static void ComposeFooter(IContainer container, PolicyDocumentModel model)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(BorderGray);
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(model.AgencyContact).FontSize(7).FontColor(Muted);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Bu belge SigortaPro tarafından otomatik üretilmiştir. · Sayfa ").FontSize(7).FontColor(Muted);
                    text.CurrentPageNumber().FontSize(7).FontColor(Muted);
                    text.Span(" / ").FontSize(7).FontColor(Muted);
                    text.TotalPages().FontSize(7).FontColor(Muted);
                });
            });
        });
    }

    private static void HeaderCell(TableCellDescriptor header, string text, bool alignRight = false)
    {
        var cell = header.Cell().Background(Accent).PaddingHorizontal(6).PaddingVertical(4);
        var aligned = alignRight ? cell.AlignRight() : cell;
        aligned.Text(text).Bold().FontSize(9).FontColor(Colors.White);
    }

    private static void BodyCell(TableDescriptor table, string text, Color? color = null, bool alignRight = false)
    {
        var cell = table.Cell().BorderBottom(0.5f).BorderColor(BorderGray).PaddingHorizontal(6).PaddingVertical(4);
        var aligned = alignRight ? cell.AlignRight() : cell;
        aligned.Text(text).FontSize(9).FontColor(color ?? DarkText);
    }

    private static string Money(decimal value) => $"{value.ToString("N2", Tr)} ₺";

    private static string Date(DateTime value) => value.ToString("dd.MM.yyyy", Tr);

    private static string BranchLabel(InsuranceBranch branch) => branch switch
    {
        InsuranceBranch.Kasko => "Kasko",
        InsuranceBranch.Trafik => "Trafik",
        InsuranceBranch.Konut => "Konut",
        InsuranceBranch.Dask => "DASK",
        InsuranceBranch.Saglik => "Sağlık",
        _ => branch.ToString(),
    };

    private static string PackageLabel(CoveragePackage package) => package switch
    {
        CoveragePackage.Standart => "Standart",
        CoveragePackage.Genisletilmis => "Genişletilmiş",
        CoveragePackage.Premium => "Premium",
        _ => package.ToString(),
    };

    private static string StatusLabel(PolicyStatus status) => status switch
    {
        PolicyStatus.Active => "Aktif",
        PolicyStatus.Expired => "Süresi Doldu",
        PolicyStatus.Cancelled => "İptal Edildi",
        _ => status.ToString(),
    };

    private static string RiskLabel(RiskScore score) => score switch
    {
        RiskScore.Low => "Düşük",
        RiskScore.Medium => "Orta",
        RiskScore.High => "Yüksek",
        _ => score.ToString(),
    };
}
