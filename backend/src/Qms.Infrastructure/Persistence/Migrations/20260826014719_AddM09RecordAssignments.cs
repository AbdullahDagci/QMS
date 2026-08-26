using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM09RecordAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchasingOwnerUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "QualityApprover",
                schema: "supplier_audit",
                table: "supplier_audit",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "QualityApproverUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Verifier",
                schema: "supplier_audit",
                table: "supplier_audit",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "VerifierUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_supplier_audit_LeadAuditorUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                column: "LeadAuditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_audit_PurchasingOwnerUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                column: "PurchasingOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_audit_QualityApproverUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                column: "QualityApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_audit_VerifierUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                column: "VerifierUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_audit_user_LeadAuditorUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                column: "LeadAuditorUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_audit_user_PurchasingOwnerUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                column: "PurchasingOwnerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_audit_user_QualityApproverUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                column: "QualityApproverUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_audit_user_VerifierUserId",
                schema: "supplier_audit",
                table: "supplier_audit",
                column: "VerifierUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_supplier_audit_user_LeadAuditorUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_supplier_audit_user_PurchasingOwnerUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_supplier_audit_user_QualityApproverUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropForeignKey(
                name: "FK_supplier_audit_user_VerifierUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropIndex(
                name: "IX_supplier_audit_LeadAuditorUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropIndex(
                name: "IX_supplier_audit_PurchasingOwnerUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropIndex(
                name: "IX_supplier_audit_QualityApproverUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropIndex(
                name: "IX_supplier_audit_VerifierUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropColumn(
                name: "PurchasingOwnerUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropColumn(
                name: "QualityApprover",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropColumn(
                name: "QualityApproverUserId",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropColumn(
                name: "Verifier",
                schema: "supplier_audit",
                table: "supplier_audit");

            migrationBuilder.DropColumn(
                name: "VerifierUserId",
                schema: "supplier_audit",
                table: "supplier_audit");
        }
    }
}
