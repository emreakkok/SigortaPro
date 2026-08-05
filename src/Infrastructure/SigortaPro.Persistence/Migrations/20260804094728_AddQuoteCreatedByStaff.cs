using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SigortaPro.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteCreatedByStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByStaffUserId",
                table: "Quotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CreatedByStaffUserId",
                table: "Quotes",
                column: "CreatedByStaffUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotes_CreatedByStaffUserId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "CreatedByStaffUserId",
                table: "Quotes");
        }
    }
}
