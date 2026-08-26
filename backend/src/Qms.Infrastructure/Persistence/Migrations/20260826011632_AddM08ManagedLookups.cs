using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM08ManagedLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition3",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition2",
                schema: "internal_audit",
                table: "lookup_definition");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder3",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder4");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code3",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code4");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder2",
                schema: "internal_audit",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder3");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code2",
                schema: "internal_audit",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code3");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition4",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition3",
                schema: "internal_audit",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "external_audit",
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
                schema: "external_audit",
                table: "lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder2",
                schema: "external_audit",
                table: "lookup_definition",
                columns: new[] { "Category", "IsActive", "SortOrder" });

            migrationBuilder.Sql("""
                INSERT INTO external_audit.lookup_definition ("Id","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc") VALUES
                ('01a03c00-0001-7000-8000-000000000001','AuditKind','Otorite denetimi','Otorite denetimi',10,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000002','AuditKind','Müşteri denetimi','Müşteri denetimi',20,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000003','AuditKind','Belgelendirme denetimi','Belgelendirme denetimi',30,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000004','Country','Türkiye','Türkiye',10,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000005','Country','Almanya','Almanya',20,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000006','Country','ABD','Amerika Birleşik Devletleri',30,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000007','Country','Birleşik Krallık','Birleşik Krallık',40,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000008','Country','İsviçre','İsviçre',50,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000009','Confidentiality','Kurum İçi','Kurum İçi',10,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000010','Confidentiality','Gizli','Gizli',20,true,NOW(),NOW()),
                ('01a03c00-0001-7000-8000-000000000011','Confidentiality','Çok Gizli','Çok Gizli',30,true,NOW(),NOW());
                INSERT INTO external_audit.lookup_definition ("Id","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc")
                SELECT gen_random_uuid(),'AuditKind',x."AuditKind",x."AuditKind",100,true,NOW(),NOW() FROM (SELECT DISTINCT "AuditKind" FROM external_audit.external_audit WHERE "AuditKind" IS NOT NULL AND BTRIM("AuditKind")<>'') x ON CONFLICT ("Category","Code") DO NOTHING;
                INSERT INTO external_audit.lookup_definition ("Id","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc")
                SELECT gen_random_uuid(),'Country',x."AuthorityCountry",x."AuthorityCountry",100,true,NOW(),NOW() FROM (SELECT DISTINCT "AuthorityCountry" FROM external_audit.external_audit WHERE "AuthorityCountry" IS NOT NULL AND BTRIM("AuthorityCountry")<>'') x ON CONFLICT ("Category","Code") DO NOTHING;
                INSERT INTO external_audit.lookup_definition ("Id","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc")
                SELECT gen_random_uuid(),'Confidentiality',x."Confidentiality",x."Confidentiality",100,true,NOW(),NOW() FROM (SELECT DISTINCT "Confidentiality" FROM external_audit.document_request WHERE "Confidentiality" IS NOT NULL AND BTRIM("Confidentiality")<>'') x ON CONFLICT ("Category","Code") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "external_audit");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition4",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition3",
                schema: "internal_audit",
                table: "lookup_definition");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder4",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder3");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code4",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code3");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder3",
                schema: "internal_audit",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder2");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code3",
                schema: "internal_audit",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code2");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition3",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition2",
                schema: "internal_audit",
                table: "lookup_definition",
                column: "Id");
        }
    }
}
