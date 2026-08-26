using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM03ManagedLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "change_lookup_definition",
                schema: "quality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_lookup_definition", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_change_lookup_definition_Category_Code",
                schema: "quality",
                table: "change_lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_change_lookup_definition_Category_IsActive_SortOrder",
                schema: "quality",
                table: "change_lookup_definition",
                columns: new[] { "Category", "IsActive", "SortOrder" });

            var seededAt = new DateTimeOffset(2026, 8, 25, 22, 42, 43, TimeSpan.Zero);
            migrationBuilder.InsertData(
                schema: "quality", table: "change_lookup_definition",
                columns: new[] { "Id", "Category", "Code", "Name", "SortOrder", "IsActive", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { Guid.Parse("01a03b00-0001-7000-8000-000000000001"), "ChangeType", "Süreç", "Süreç", 10, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0001-7000-8000-000000000002"), "ChangeType", "Ekipman", "Ekipman", 20, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0001-7000-8000-000000000003"), "ChangeType", "Tesis", "Tesis", 30, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0001-7000-8000-000000000004"), "ChangeType", "Bilgisayarlı Sistem", "Bilgisayarlı Sistem", 40, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0001-7000-8000-000000000005"), "ChangeType", "Doküman", "Doküman", 50, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0001-7000-8000-000000000006"), "ChangeType", "Organizasyon", "Organizasyon", 60, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0001-7000-8000-000000000007"), "ChangeType", "Tedarikçi", "Tedarikçi", 70, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0002-7000-8000-000000000001"), "RiskLevel", "Düşük", "Düşük", 10, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0002-7000-8000-000000000002"), "RiskLevel", "Orta", "Orta", 20, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0002-7000-8000-000000000003"), "RiskLevel", "Yüksek", "Yüksek", 30, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0002-7000-8000-000000000004"), "RiskLevel", "Kritik", "Kritik", 40, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0003-7000-8000-000000000001"), "RegulatoryImpact", "None", "Ruhsat etkisi yok", 10, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0003-7000-8000-000000000002"), "RegulatoryImpact", "Notification", "Bildirim", 20, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0003-7000-8000-000000000003"), "RegulatoryImpact", "Variation", "Varyasyon", 30, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0003-7000-8000-000000000004"), "RegulatoryImpact", "AuthorityApproval", "Otorite onayı", 40, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0004-7000-8000-000000000001"), "ActionCategory", "Uygulama", "Uygulama", 10, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0004-7000-8000-000000000002"), "ActionCategory", "Doküman", "Doküman", 20, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0004-7000-8000-000000000003"), "ActionCategory", "Eğitim", "Eğitim", 30, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0004-7000-8000-000000000004"), "ActionCategory", "Validasyon", "Validasyon", 40, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0004-7000-8000-000000000005"), "ActionCategory", "Risk", "Risk", 50, true, seededAt, seededAt },
                    { Guid.Parse("01a03b00-0004-7000-8000-000000000006"), "ActionCategory", "Ruhsat", "Ruhsat", 60, true, seededAt, seededAt }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "change_lookup_definition",
                schema: "quality");
        }
    }
}
