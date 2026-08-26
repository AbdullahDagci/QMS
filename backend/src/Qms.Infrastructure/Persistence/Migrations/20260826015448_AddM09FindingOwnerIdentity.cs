using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM09FindingOwnerIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "supplier_audit",
                table: "finding",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_finding_OwnerUserId2",
                schema: "supplier_audit",
                table: "finding",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_finding_user_OwnerUserId",
                schema: "supplier_audit",
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
                name: "FK_finding_user_OwnerUserId",
                schema: "external_audit",
                table: "finding");

            migrationBuilder.DropIndex(
                name: "IX_finding_OwnerUserId2",
                schema: "supplier_audit",
                table: "finding");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "supplier_audit",
                table: "finding");
        }
    }
}
