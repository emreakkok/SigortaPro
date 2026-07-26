using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SigortaPro.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingVersionsAndQuoteSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PricingBuildingAge",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PricingCapturedAt",
                table: "Quotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingDriverAge",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingEarthquakeZone",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingEnginePowerHp",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingInsuredAge",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PricingIsSmoker",
                table: "Quotes",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingNoClaimTier",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricingRiskCity",
                table: "Quotes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingSquareMeters",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingVehicleAge",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PricingVersionId",
                table: "Quotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PricingVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingBranchRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PricingVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Branch = table.Column<int>(type: "int", nullable: false),
                    BasePremium = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingBranchRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingBranchRates_PricingVersions_PricingVersionId",
                        column: x => x.PricingVersionId,
                        principalTable: "PricingVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PricingBranchRates_Version_Branch",
                table: "PricingBranchRates",
                columns: new[] { "PricingVersionId", "Branch" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingVersions_EffectiveFrom",
                table: "PricingVersions",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_PricingVersions_VersionNumber",
                table: "PricingVersions",
                column: "VersionNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingBranchRates");

            migrationBuilder.DropTable(
                name: "PricingVersions");

            migrationBuilder.DropColumn(
                name: "PricingBuildingAge",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingCapturedAt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingDriverAge",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingEarthquakeZone",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingEnginePowerHp",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingInsuredAge",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingIsSmoker",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingNoClaimTier",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingRiskCity",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingSquareMeters",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingVehicleAge",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PricingVersionId",
                table: "Quotes");
        }
    }
}
