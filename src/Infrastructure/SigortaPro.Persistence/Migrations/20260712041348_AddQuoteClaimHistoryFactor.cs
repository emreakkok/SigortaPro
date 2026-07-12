using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SigortaPro.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteClaimHistoryFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ClaimHistoryFactor",
                table: "Quotes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 1.00m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimHistoryFactor",
                table: "Quotes");
        }
    }
}
