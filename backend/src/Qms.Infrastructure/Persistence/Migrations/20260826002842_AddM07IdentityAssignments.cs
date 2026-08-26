using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM07IdentityAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AuditeeDepartmentId",
                schema: "internal_audit",
                table: "internal_audit",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "internal_audit",
                table: "finding",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE internal_audit.internal_audit a SET "AuditeeDepartmentId" = d."Id" FROM organization.department d WHERE d."Name" = a."AuditeeDepartment";
                UPDATE internal_audit.internal_audit a SET "AuditeeDepartmentId" = qr."DepartmentId" FROM core.quality_record qr WHERE qr."Id" = a."QualityRecordId" AND a."AuditeeDepartmentId" IS NULL;
                UPDATE internal_audit.finding f SET "OwnerUserId" = u."Id" FROM identity."user" u WHERE u."DisplayName" = f."Owner";
                UPDATE internal_audit.finding f SET "OwnerUserId" = a."LeadAuditorUserId" FROM internal_audit.internal_audit a WHERE f."InternalAuditId" = a."Id" AND f."OwnerUserId" IS NULL;
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM internal_audit.internal_audit WHERE "AuditeeDepartmentId" IS NULL) THEN RAISE EXCEPTION 'M.07 denetlenen bölüm snapshotı organizasyonla eşleştirilemedi'; END IF;
                  IF EXISTS (SELECT 1 FROM internal_audit.finding WHERE "OwnerUserId" IS NULL) THEN RAISE EXCEPTION 'M.07 bulgu sahibi kimlikle eşleştirilemedi'; END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(name: "AuditeeDepartmentId", schema: "internal_audit", table: "internal_audit", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "OwnerUserId", schema: "internal_audit", table: "finding", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_internal_audit_AuditeeDepartmentId",
                schema: "internal_audit",
                table: "internal_audit",
                column: "AuditeeDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_internal_audit_LeadAuditorUserId",
                schema: "internal_audit",
                table: "internal_audit",
                column: "LeadAuditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_finding_OwnerUserId",
                schema: "internal_audit",
                table: "finding",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_finding_user_OwnerUserId",
                schema: "internal_audit",
                table: "finding",
                column: "OwnerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_internal_audit_department_AuditeeDepartmentId",
                schema: "internal_audit",
                table: "internal_audit",
                column: "AuditeeDepartmentId",
                principalSchema: "organization",
                principalTable: "department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_internal_audit_user_LeadAuditorUserId",
                schema: "internal_audit",
                table: "internal_audit",
                column: "LeadAuditorUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_finding_user_OwnerUserId",
                schema: "internal_audit",
                table: "finding");

            migrationBuilder.DropForeignKey(
                name: "FK_internal_audit_department_AuditeeDepartmentId",
                schema: "internal_audit",
                table: "internal_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_internal_audit_user_LeadAuditorUserId",
                schema: "internal_audit",
                table: "internal_audit");

            migrationBuilder.DropIndex(
                name: "IX_internal_audit_AuditeeDepartmentId",
                schema: "internal_audit",
                table: "internal_audit");

            migrationBuilder.DropIndex(
                name: "IX_internal_audit_LeadAuditorUserId",
                schema: "internal_audit",
                table: "internal_audit");

            migrationBuilder.DropIndex(
                name: "IX_finding_OwnerUserId",
                schema: "internal_audit",
                table: "finding");

            migrationBuilder.DropColumn(
                name: "AuditeeDepartmentId",
                schema: "internal_audit",
                table: "internal_audit");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "internal_audit",
                table: "finding");
        }
    }
}
