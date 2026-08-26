using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM01AssignmentMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assignment_rule",
                schema: "deviation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectedDepartment = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    DeviationType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    MinimumRiskScore = table.Column<int>(type: "integer", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_rule", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "deviation", table: "assignment_rule",
                columns: new[] { "Id", "TaskRole", "AssignedUserId", "DetectedDepartment", "DeviationType", "MinimumRiskScore", "Priority", "IsActive" },
                values: new object[,]
                {
                    { Guid.Parse("019d2f20-0001-7000-8000-000000000001"), "ProcessAuthority", Guid.Parse("01991f70-6f40-7000-8000-000000000018"), null!, null!, null!, 10, true },
                    { Guid.Parse("019d2f20-0002-7000-8000-000000000002"), "Investigator", Guid.Parse("01991f70-6f40-7000-8000-000000000012"), null!, null!, null!, 10, true },
                    { Guid.Parse("019d2f20-0003-7000-8000-000000000003"), "Evaluator", Guid.Parse("01991f70-6f40-7000-8000-000000000001"), null!, null!, null!, 10, true },
                    { Guid.Parse("019d2f20-0004-7000-8000-000000000004"), "Approver", Guid.Parse("01991f70-6f40-7000-8000-000000000010"), null!, null!, null!, 10, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_rule_AssignedUserId",
                schema: "deviation",
                table: "assignment_rule",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_rule_TaskRole_IsActive_Priority",
                schema: "deviation",
                table: "assignment_rule",
                columns: new[] { "TaskRole", "IsActive", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_rule",
                schema: "deviation");
        }
    }
}
