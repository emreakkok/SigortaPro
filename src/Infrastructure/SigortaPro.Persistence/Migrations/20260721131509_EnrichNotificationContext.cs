using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SigortaPro.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnrichNotificationContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "Notifications",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceCode",
                table: "Notifications",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                table: "Notifications");
        }
    }
}
