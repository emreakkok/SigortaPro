using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SigortaPro.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteInsuredPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InsuredBirthDate",
                table: "Quotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuredFirstName",
                table: "Quotes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuredLastName",
                table: "Quotes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuredPhoneNumber",
                table: "Quotes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuredRelationship",
                table: "Quotes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuredTckn",
                table: "Quotes",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuredBirthDate",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "InsuredFirstName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "InsuredLastName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "InsuredPhoneNumber",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "InsuredRelationship",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "InsuredTckn",
                table: "Quotes");
        }
    }
}
