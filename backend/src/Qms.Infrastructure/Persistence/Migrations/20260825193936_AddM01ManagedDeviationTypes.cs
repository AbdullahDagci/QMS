using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM01ManagedDeviationTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "quality");

            migrationBuilder.CreateTable(
                name: "deviation_type_definition",
                schema: "quality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deviation_type_definition", x => x.Id);
                });

            var seededAt = new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero);
            migrationBuilder.InsertData(
                schema: "quality",
                table: "deviation_type_definition",
                columns: new[] { "Id", "Code", "Name", "SortOrder", "IsActive", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { Guid.Parse("019d2f10-0001-7000-8000-000000000001"), "PROSES", "Proses", 10, true, seededAt, seededAt },
                    { Guid.Parse("019d2f10-0002-7000-8000-000000000002"), "URUN", "Ürün", 20, true, seededAt, seededAt },
                    { Guid.Parse("019d2f10-0003-7000-8000-000000000003"), "EKIPMAN", "Ekipman", 30, true, seededAt, seededAt },
                    { Guid.Parse("019d2f10-0004-7000-8000-000000000004"), "DOKUMAN", "Doküman", 40, true, seededAt, seededAt },
                    { Guid.Parse("019d2f10-0005-7000-8000-000000000005"), "DIGER", "Diğer", 50, true, seededAt, seededAt }
                });

            migrationBuilder.CreateIndex(
                name: "IX_deviation_type_definition_Code",
                schema: "quality",
                table: "deviation_type_definition",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deviation_type_definition_IsActive_SortOrder",
                schema: "quality",
                table: "deviation_type_definition",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_deviation_type_definition_Name",
                schema: "quality",
                table: "deviation_type_definition",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deviation_type_definition",
                schema: "quality");
        }
    }
}
