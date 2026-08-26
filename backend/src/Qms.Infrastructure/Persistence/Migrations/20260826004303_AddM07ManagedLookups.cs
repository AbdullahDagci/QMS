using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM07ManagedLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition2",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder2",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder3");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code2",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code3");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition3",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "internal_audit",
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
                    table.PrimaryKey("PK_lookup_definition2", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_Code2",
                schema: "internal_audit",
                table: "lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder2",
                schema: "internal_audit",
                table: "lookup_definition",
                columns: new[] { "Category", "IsActive", "SortOrder" });

            migrationBuilder.Sql("""
                INSERT INTO internal_audit.lookup_definition ("Id","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc") VALUES
                ('01a03b84-0001-7000-8000-000000000001','AuditType','Routine','Rutin denetim',10,true,NOW(),NOW()),
                ('01a03b84-0001-7000-8000-000000000002','AuditType','ForCause','Neden odaklı denetim',20,true,NOW(),NOW()),
                ('01a03b84-0001-7000-8000-000000000003','AuditType','FollowUp','Takip denetimi',30,true,NOW(),NOW()),
                ('01a03b84-0001-7000-8000-000000000004','AuditType','Unplanned','Plansız denetim',40,true,NOW(),NOW());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "internal_audit");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition3",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder3",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder2");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code3",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code2");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition2",
                schema: "training",
                table: "lookup_definition",
                column: "Id");
        }
    }
}
