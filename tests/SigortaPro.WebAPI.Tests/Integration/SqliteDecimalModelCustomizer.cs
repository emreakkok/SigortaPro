using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SigortaPro.WebAPI.Tests.Integration;

/// <summary>
/// YALNIZCA TEST ALTYAPISI. SQLite, <c>decimal</c> üzerinde <c>SUM</c>/<c>AVG</c> gibi toplama
/// operatörlerini desteklemez (decimal'ı TEXT olarak saklar) — üretimdeki SQL Server destekler.
/// Dashboard'ın SQL tarafı toplamlarının (prim üretimi vb.) entegrasyon testlerinde de çalışabilmesi için
/// test bağlamında tüm decimal özellikler double'a dönüştürülür.
/// <para>
/// Üretim kodu DEĞİŞMEZ: şema SQL Server'da <c>decimal(18,2)</c> kalır. Test verileri en fazla 2 ondalıklı ve
/// küçük büyüklükte olduğundan double dönüşümü kayıpsızdır (bu aralıkta double↔decimal gidiş-dönüşü tamdır),
/// dolayısıyla tutar eşitliği bekleyen mevcut testler (ör. fiyatlandırma determinizmi) etkilenmez.
/// </para>
/// </summary>
internal sealed class SqliteDecimalModelCustomizer : RelationalModelCustomizer
{
    public SqliteDecimalModelCustomizer(ModelCustomizerDependencies dependencies)
        : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entityType => entityType.GetProperties()))
        {
            if (property.ClrType == typeof(decimal))
            {
                property.SetValueConverter(new CastingConverter<decimal, double>());
            }
            else if (property.ClrType == typeof(decimal?))
            {
                property.SetValueConverter(new CastingConverter<decimal?, double?>());
            }
        }
    }
}
