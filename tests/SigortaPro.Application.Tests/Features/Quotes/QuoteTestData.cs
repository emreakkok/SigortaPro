using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Quotes;

// Teklif modülü testlerinde kullanılan entity kurucuları.
internal static class QuoteTestData
{
    public static InsuranceProduct CreateProduct(InsuranceBranch branch, Guid? id = null)
    {
        var product = new InsuranceProduct($"{branch} Ürünü", branch, "Test ürünü")
        {
            Id = id ?? Guid.NewGuid(),
        };

        product.AddCoverage(new Coverage(product.Id, "Teminat A", "Açıklama A", 100000m));
        product.AddCoverage(new Coverage(product.Id, "Teminat B", "Açıklama B", 50000m));

        return product;
    }
}
