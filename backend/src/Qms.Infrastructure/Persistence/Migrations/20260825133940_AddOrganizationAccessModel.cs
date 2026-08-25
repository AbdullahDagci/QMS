using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationAccessModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                schema: "identity",
                table: "user",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ProfileKey",
                schema: "identity",
                table: "user",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "delegation",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delegation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "department",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ManagerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department", x => x.Id);
                    table.ForeignKey(
                        name: "FK_department_department_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalSchema: "organization",
                        principalTable: "department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "position",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsManagement = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_position", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "task_assignment",
                schema: "workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DelegatedFromUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_assignment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_position",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_position", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_position_department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "organization",
                        principalTable: "department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_position_position_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "organization",
                        principalTable: "position",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_DepartmentId",
                schema: "identity",
                table: "user",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_user_ProfileKey",
                schema: "identity",
                table: "user",
                column: "ProfileKey",
                unique: true,
                filter: "\"ProfileKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_delegation_DelegateUserId_StartsAtUtc_EndsAtUtc",
                schema: "organization",
                table: "delegation",
                columns: new[] { "DelegateUserId", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_delegation_DelegatorUserId_StartsAtUtc_EndsAtUtc",
                schema: "organization",
                table: "delegation",
                columns: new[] { "DelegatorUserId", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_department_Code",
                schema: "organization",
                table: "department",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_department_ManagerUserId",
                schema: "organization",
                table: "department",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_department_ParentDepartmentId",
                schema: "organization",
                table: "department",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_position_Code",
                schema: "organization",
                table: "position",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_assignment_AggregateType_AggregateId_Status",
                schema: "workflow",
                table: "task_assignment",
                columns: new[] { "AggregateType", "AggregateId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_task_assignment_AssignedUserId_Status_DueAtUtc",
                schema: "workflow",
                table: "task_assignment",
                columns: new[] { "AssignedUserId", "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_position_DepartmentId",
                schema: "organization",
                table: "user_position",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_user_position_PositionId",
                schema: "organization",
                table: "user_position",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_position_UserId_PositionId_DepartmentId_EndsAtUtc",
                schema: "organization",
                table: "user_position",
                columns: new[] { "UserId", "PositionId", "DepartmentId", "EndsAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delegation",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "task_assignment",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "user_position",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "department",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "position",
                schema: "organization");

            migrationBuilder.DropIndex(
                name: "IX_user_DepartmentId",
                schema: "identity",
                table: "user");

            migrationBuilder.DropIndex(
                name: "IX_user_ProfileKey",
                schema: "identity",
                table: "user");

            migrationBuilder.DropColumn(
                name: "ProfileKey",
                schema: "identity",
                table: "user");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                schema: "identity",
                table: "user",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
