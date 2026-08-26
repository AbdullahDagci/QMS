using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM09ManagedLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition4",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder4",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder5");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code4",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code5");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "supplier_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_definition4", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_Code4",
                schema: "supplier_audit",
                table: "lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder4",
                schema: "supplier_audit",
                table: "lookup_definition",
                columns: new[] { "Category", "IsActive", "SortOrder" });

            var seededAt = new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero);
            migrationBuilder.InsertData(
                schema: "supplier_audit", table: "lookup_definition",
                columns: new[] { "Id", "Category", "Code", "Name", "SortOrder", "IsActive", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { Guid.Parse("019b0009-0000-7000-8000-000000000001"), "Country", "Türkiye", "Türkiye", 10, true, seededAt, seededAt },
                    { Guid.Parse("019b0009-0000-7000-8000-000000000002"), "Country", "Almanya", "Almanya", 20, true, seededAt, seededAt },
                    { Guid.Parse("019b0009-0000-7000-8000-000000000003"), "Country", "ABD", "Amerika Birleşik Devletleri", 30, true, seededAt, seededAt },
                    { Guid.Parse("019b0009-0000-7000-8000-000000000004"), "Country", "Birleşik Krallık", "Birleşik Krallık", 40, true, seededAt, seededAt },
                    { Guid.Parse("019b0009-0000-7000-8000-000000000005"), "Country", "İsviçre", "İsviçre", 50, true, seededAt, seededAt },
                    { Guid.Parse("019b0009-0000-7000-8000-000000000011"), "Criticality", "Kritik", "Kritik", 10, true, seededAt, seededAt },
                    { Guid.Parse("019b0009-0000-7000-8000-000000000012"), "Criticality", "Yüksek", "Yüksek", 20, true, seededAt, seededAt },
                    { Guid.Parse("019b0009-0000-7000-8000-000000000013"), "Criticality", "Orta", "Orta", 30, true, seededAt, seededAt },
                    { Guid.Parse("019b0009-0000-7000-8000-000000000014"), "Criticality", "Düşük", "Düşük", 40, true, seededAt, seededAt }
                });
            migrationBuilder.Sql("""
                INSERT INTO supplier_audit.lookup_definition ("Id", "Category", "Code", "Name", "SortOrder", "IsActive", "CreatedAtUtc", "UpdatedAtUtc")
                SELECT gen_random_uuid(), 'Country', value, value, 100, true, now(), now()
                FROM (SELECT DISTINCT "Country" AS value FROM supplier_audit.supplier_audit WHERE btrim("Country") <> '') values
                ON CONFLICT ("Category", "Code") DO NOTHING;
                INSERT INTO supplier_audit.lookup_definition ("Id", "Category", "Code", "Name", "SortOrder", "IsActive", "CreatedAtUtc", "UpdatedAtUtc")
                SELECT gen_random_uuid(), 'Criticality', value, value, 100, true, now(), now()
                FROM (SELECT DISTINCT "Criticality" AS value FROM supplier_audit.supplier_audit WHERE btrim("Criticality") <> '') values
                ON CONFLICT ("Category", "Code") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "supplier_audit");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder5",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder4");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code5",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition4",
                schema: "training",
                table: "lookup_definition",
                column: "Id");
        }
    }
}
