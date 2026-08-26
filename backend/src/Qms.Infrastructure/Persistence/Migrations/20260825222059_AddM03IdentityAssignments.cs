using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM03IdentityAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "change_control",
                table: "implementation_action",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "change_control",
                table: "change_control",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "change_control",
                table: "assessment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerUserId",
                schema: "change_control",
                table: "assessment",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_implementation_action_OwnerUserId",
                schema: "change_control",
                table: "implementation_action",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_change_control_OwnerUserId",
                schema: "change_control",
                table: "change_control",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_assessment_DepartmentId",
                schema: "change_control",
                table: "assessment",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_assessment_ReviewerUserId",
                schema: "change_control",
                table: "assessment",
                column: "ReviewerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_assessment_department_DepartmentId",
                schema: "change_control",
                table: "assessment",
                column: "DepartmentId",
                principalSchema: "organization",
                principalTable: "department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_assessment_user_ReviewerUserId",
                schema: "change_control",
                table: "assessment",
                column: "ReviewerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_change_control_user_OwnerUserId",
                schema: "change_control",
                table: "change_control",
                column: "OwnerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_implementation_action_user_OwnerUserId",
                schema: "change_control",
                table: "implementation_action",
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
                name: "FK_assessment_department_DepartmentId",
                schema: "change_control",
                table: "assessment");

            migrationBuilder.DropForeignKey(
                name: "FK_assessment_user_ReviewerUserId",
                schema: "change_control",
                table: "assessment");

            migrationBuilder.DropForeignKey(
                name: "FK_change_control_user_OwnerUserId",
                schema: "change_control",
                table: "change_control");

            migrationBuilder.DropForeignKey(
                name: "FK_implementation_action_user_OwnerUserId",
                schema: "change_control",
                table: "implementation_action");

            migrationBuilder.DropIndex(
                name: "IX_implementation_action_OwnerUserId",
                schema: "change_control",
                table: "implementation_action");

            migrationBuilder.DropIndex(
                name: "IX_change_control_OwnerUserId",
                schema: "change_control",
                table: "change_control");

            migrationBuilder.DropIndex(
                name: "IX_assessment_DepartmentId",
                schema: "change_control",
                table: "assessment");

            migrationBuilder.DropIndex(
                name: "IX_assessment_ReviewerUserId",
                schema: "change_control",
                table: "assessment");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "change_control",
                table: "implementation_action");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "change_control",
                table: "change_control");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "change_control",
                table: "assessment");

            migrationBuilder.DropColumn(
                name: "ReviewerUserId",
                schema: "change_control",
                table: "assessment");
        }
    }
}
