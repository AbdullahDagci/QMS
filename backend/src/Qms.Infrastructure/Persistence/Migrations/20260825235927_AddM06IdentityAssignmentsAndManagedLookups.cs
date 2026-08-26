using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM06IdentityAssignmentsAndManagedLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition1",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition",
                schema: "document",
                table: "lookup_definition");

            migrationBuilder.DropIndex(
                name: "IX_investigation_ComplaintId_Department",
                schema: "complaint",
                table: "investigation");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder1",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder2");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code1",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code2");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder",
                schema: "document",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder1");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code",
                schema: "document",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code1");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                schema: "complaint",
                table: "response_version",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreparedByUserId",
                schema: "complaint",
                table: "response_version",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "complaint",
                table: "investigation",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InvestigatorUserId",
                schema: "complaint",
                table: "investigation",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "complaint",
                table: "complaint",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition2",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition1",
                schema: "document",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "complaint",
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
                    table.PrimaryKey("PK_lookup_definition", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO complaint.lookup_definition ("Id","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc") VALUES
                ('01a06b00-0001-7000-8000-000000000001','Channel','Email','E-posta',10,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0001-7000-8000-000000000002','Channel','Phone','Telefon',20,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0001-7000-8000-000000000003','Channel','Portal','Müşteri portalı',30,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0001-7000-8000-000000000004','Channel','Letter','Yazılı bildirim',40,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0002-7000-8000-000000000001','Country','TR','Türkiye',10,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0002-7000-8000-000000000002','Country','DE','Almanya',20,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0002-7000-8000-000000000003','Country','US','Amerika Birleşik Devletleri',30,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0002-7000-8000-000000000004','Country','OTHER','Diğer',999,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0003-7000-8000-000000000001','ComplaintType','ProductQuality','Ürün kalitesi',10,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0003-7000-8000-000000000002','ComplaintType','Packaging','Ambalaj',20,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0003-7000-8000-000000000003','ComplaintType','Delivery','Teslimat',30,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0003-7000-8000-000000000004','ComplaintType','MedicalInformation','Tıbbi bilgi',40,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00'),
                ('01a06b00-0003-7000-8000-000000000005','ComplaintType','Other','Diğer',999,TRUE,TIMESTAMPTZ '2026-08-25T23:59:27+00:00',TIMESTAMPTZ '2026-08-25T23:59:27+00:00');

                UPDATE complaint.complaint c SET "OwnerUserId"=u."Id" FROM identity."user" u WHERE c."Owner"=u."DisplayName" AND u."IsActive";
                UPDATE complaint.complaint c SET "OwnerUserId"=q."CreatedByUserId" FROM core.quality_record q WHERE c."OwnerUserId" IS NULL AND c."QualityRecordId"=q."Id";
                UPDATE complaint.investigation i SET "DepartmentId"=d."Id" FROM organization.department d WHERE i."Department"=d."Name" AND d."IsActive";
                UPDATE complaint.investigation i SET "InvestigatorUserId"=u."Id" FROM identity."user" u WHERE i."Investigator"=u."DisplayName" AND u."IsActive";
                UPDATE complaint.investigation i SET "InvestigatorUserId"=c."OwnerUserId" FROM complaint.complaint c WHERE i."InvestigatorUserId" IS NULL AND i."ComplaintId"=c."Id";
                UPDATE complaint.response_version r SET "PreparedByUserId"=u."Id" FROM identity."user" u WHERE r."PreparedBy"=u."DisplayName" AND u."IsActive";
                UPDATE complaint.response_version r SET "PreparedByUserId"=c."OwnerUserId" FROM complaint.complaint c WHERE r."PreparedByUserId" IS NULL AND r."ComplaintId"=c."Id";
                UPDATE complaint.response_version r SET "ApprovedByUserId"=u."Id" FROM identity."user" u WHERE r."ApprovedBy"=u."DisplayName" AND u."IsActive";
                """);

            migrationBuilder.AlterColumn<Guid>(name: "OwnerUserId", schema: "complaint", table: "complaint", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "DepartmentId", schema: "complaint", table: "investigation", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "InvestigatorUserId", schema: "complaint", table: "investigation", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PreparedByUserId", schema: "complaint", table: "response_version", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_response_version_ApprovedByUserId",
                schema: "complaint",
                table: "response_version",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_response_version_PreparedByUserId",
                schema: "complaint",
                table: "response_version",
                column: "PreparedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_investigation_ComplaintId_DepartmentId",
                schema: "complaint",
                table: "investigation",
                columns: new[] { "ComplaintId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_investigation_DepartmentId",
                schema: "complaint",
                table: "investigation",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_investigation_InvestigatorUserId",
                schema: "complaint",
                table: "investigation",
                column: "InvestigatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_OwnerUserId",
                schema: "complaint",
                table: "complaint",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_Code",
                schema: "complaint",
                table: "lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder",
                schema: "complaint",
                table: "lookup_definition",
                columns: new[] { "Category", "IsActive", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_complaint_user_OwnerUserId",
                schema: "complaint",
                table: "complaint",
                column: "OwnerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_investigation_department_DepartmentId",
                schema: "complaint",
                table: "investigation",
                column: "DepartmentId",
                principalSchema: "organization",
                principalTable: "department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_investigation_user_InvestigatorUserId",
                schema: "complaint",
                table: "investigation",
                column: "InvestigatorUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_response_version_user_ApprovedByUserId",
                schema: "complaint",
                table: "response_version",
                column: "ApprovedByUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_response_version_user_PreparedByUserId",
                schema: "complaint",
                table: "response_version",
                column: "PreparedByUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_complaint_user_OwnerUserId",
                schema: "complaint",
                table: "complaint");

            migrationBuilder.DropForeignKey(
                name: "FK_investigation_department_DepartmentId",
                schema: "complaint",
                table: "investigation");

            migrationBuilder.DropForeignKey(
                name: "FK_investigation_user_InvestigatorUserId",
                schema: "complaint",
                table: "investigation");

            migrationBuilder.DropForeignKey(
                name: "FK_response_version_user_ApprovedByUserId",
                schema: "complaint",
                table: "response_version");

            migrationBuilder.DropForeignKey(
                name: "FK_response_version_user_PreparedByUserId",
                schema: "complaint",
                table: "response_version");

            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "complaint");

            migrationBuilder.DropIndex(
                name: "IX_response_version_ApprovedByUserId",
                schema: "complaint",
                table: "response_version");

            migrationBuilder.DropIndex(
                name: "IX_response_version_PreparedByUserId",
                schema: "complaint",
                table: "response_version");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition2",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition1",
                schema: "document",
                table: "lookup_definition");

            migrationBuilder.DropIndex(
                name: "IX_investigation_ComplaintId_DepartmentId",
                schema: "complaint",
                table: "investigation");

            migrationBuilder.DropIndex(
                name: "IX_investigation_DepartmentId",
                schema: "complaint",
                table: "investigation");

            migrationBuilder.DropIndex(
                name: "IX_investigation_InvestigatorUserId",
                schema: "complaint",
                table: "investigation");

            migrationBuilder.DropIndex(
                name: "IX_complaint_OwnerUserId",
                schema: "complaint",
                table: "complaint");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "complaint",
                table: "response_version");

            migrationBuilder.DropColumn(
                name: "PreparedByUserId",
                schema: "complaint",
                table: "response_version");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "complaint",
                table: "investigation");

            migrationBuilder.DropColumn(
                name: "InvestigatorUserId",
                schema: "complaint",
                table: "investigation");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "complaint",
                table: "complaint");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder2",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder1");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code2",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code1");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder1",
                schema: "document",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_IsActive_SortOrder");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code1",
                schema: "document",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition1",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition",
                schema: "document",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_investigation_ComplaintId_Department",
                schema: "complaint",
                table: "investigation",
                columns: new[] { "ComplaintId", "Department" },
                unique: true);
        }
    }
}
