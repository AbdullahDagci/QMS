using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM08IdentityForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_finding_OwnerUserId",
                schema: "internal_audit",
                table: "finding",
                newName: "IX_finding_OwnerUserId1");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "external_audit",
                table: "finding",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "external_audit",
                table: "external_audit",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE external_audit.external_audit a SET "OwnerUserId" = COALESCE(
                    (SELECT t."AssignedUserId" FROM workflow.task_assignment t WHERE t."AggregateType"='ExternalAudit' AND t."AggregateId"=a."Id" AND t."TaskRole"='ExternalAuditCoordinator' ORDER BY t."AssignedAtUtc" LIMIT 1),
                    (SELECT u."Id" FROM identity."user" u WHERE u."IsActive" AND u."DisplayName"=a."Owner" ORDER BY u."Id" LIMIT 1));
                UPDATE external_audit.finding f SET "OwnerUserId" = COALESCE(
                    (SELECT t."AssignedUserId" FROM workflow.task_assignment t WHERE t."AggregateType"='ExternalAudit' AND t."AggregateId"=f."ExternalAuditId" AND t."TaskRole"=('ExternalAuditFinding:' || f."Id"::text) ORDER BY t."AssignedAtUtc" LIMIT 1),
                    (SELECT u."Id" FROM identity."user" u WHERE u."IsActive" AND u."DisplayName"=f."Owner" ORDER BY u."Id" LIMIT 1));
                DO $$ BEGIN IF EXISTS(SELECT 1 FROM external_audit.external_audit WHERE "OwnerUserId" IS NULL) OR EXISTS(SELECT 1 FROM external_audit.finding WHERE "OwnerUserId" IS NULL) THEN RAISE EXCEPTION 'M.08 kimlik backfill tamamlanamadı; migration güvenli biçimde durduruldu.'; END IF; END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(name: "OwnerUserId", schema: "external_audit", table: "finding", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "OwnerUserId", schema: "external_audit", table: "external_audit", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_finding_OwnerUserId",
                schema: "external_audit",
                table: "finding",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_external_audit_AuthorizedCloserUserId",
                schema: "external_audit",
                table: "external_audit",
                column: "AuthorizedCloserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_external_audit_OwnerUserId",
                schema: "external_audit",
                table: "external_audit",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_external_audit_user_AuthorizedCloserUserId",
                schema: "external_audit",
                table: "external_audit",
                column: "AuthorizedCloserUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_external_audit_user_OwnerUserId",
                schema: "external_audit",
                table: "external_audit",
                column: "OwnerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_finding_user_OwnerUserId",
                schema: "external_audit",
                table: "finding",
                column: "OwnerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_external_audit_user_AuthorizedCloserUserId",
                schema: "external_audit",
                table: "external_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_external_audit_user_OwnerUserId",
                schema: "external_audit",
                table: "external_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_finding_user_OwnerUserId",
                schema: "external_audit",
                table: "finding");

            migrationBuilder.DropIndex(
                name: "IX_finding_OwnerUserId",
                schema: "external_audit",
                table: "finding");

            migrationBuilder.DropIndex(
                name: "IX_external_audit_AuthorizedCloserUserId",
                schema: "external_audit",
                table: "external_audit");

            migrationBuilder.DropIndex(
                name: "IX_external_audit_OwnerUserId",
                schema: "external_audit",
                table: "external_audit");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "external_audit",
                table: "finding");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "external_audit",
                table: "external_audit");

            migrationBuilder.RenameIndex(
                name: "IX_finding_OwnerUserId1",
                schema: "internal_audit",
                table: "finding",
                newName: "IX_finding_OwnerUserId");
        }
    }
}
