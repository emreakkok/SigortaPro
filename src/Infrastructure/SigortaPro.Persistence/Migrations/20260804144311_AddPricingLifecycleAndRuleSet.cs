using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SigortaPro.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingLifecycleAndRuleSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PricingVersions_EffectiveFrom",
                table: "PricingVersions");

            migrationBuilder.AddColumn<decimal>(
                name: "RenewalDiscountFactor",
                table: "Quotes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 1.00m);

            migrationBuilder.AddColumn<string>(
                name: "RuleSet",
                table: "PricingVersions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PricingVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PricingVersions_Status",
                table: "PricingVersions",
                column: "Status");

            // Geçiş: bu migration öncesi ZAMAN-tabanlı modelde oluşmuş versiyonların yaşam döngüsü durumunu belirle.
            // Eskiden "yürürlükte" olan (EffectiveFrom <= şimdi, en yeni) versiyon AKTİF (1) olur; diğerleri ARŞİV (2).
            // Böylece mevcut kurulumlarda yürürlükteki tarife kesintisiz korunur (RuleSet null → baseline katsayılar,
            // baz prim versiyonun kendi oranlarından → fiyatlar birebir aynı). Tek aktif invariant'ı sağlanır.
            migrationBuilder.Sql(
                "UPDATE [PricingVersions] SET [Status] = 2; " +
                "UPDATE [PricingVersions] SET [Status] = 1 WHERE [Id] = (" +
                "SELECT TOP 1 [Id] FROM [PricingVersions] WHERE [EffectiveFrom] <= SYSUTCDATETIME() " +
                "ORDER BY [EffectiveFrom] DESC, [VersionNumber] DESC);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PricingVersions_Status",
                table: "PricingVersions");

            migrationBuilder.DropColumn(
                name: "RenewalDiscountFactor",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "RuleSet",
                table: "PricingVersions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PricingVersions");

            migrationBuilder.CreateIndex(
                name: "IX_PricingVersions_EffectiveFrom",
                table: "PricingVersions",
                column: "EffectiveFrom");
        }
    }
}
