using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceConnect.Api.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ServiceCatalogOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Services",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceOptionId",
                table: "Requests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceOptions", x => x.Id);
                    table.UniqueConstraint("AK_ServiceOptions_Id_ServiceId", x => new { x.Id, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_ServiceOptions_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ServiceOptionId_ServiceId",
                table: "Requests",
                columns: new[] { "ServiceOptionId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_ServiceId_Name",
                table: "ServiceOptions",
                columns: new[] { "ServiceId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_ServiceOptions_ServiceOptionId_ServiceId",
                table: "Requests",
                columns: new[] { "ServiceOptionId", "ServiceId" },
                principalTable: "ServiceOptions",
                principalColumns: new[] { "Id", "ServiceId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_ServiceOptions_ServiceOptionId_ServiceId",
                table: "Requests");

            migrationBuilder.DropTable(
                name: "ServiceOptions");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ServiceOptionId_ServiceId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ServiceOptionId",
                table: "Requests");
        }
    }
}
