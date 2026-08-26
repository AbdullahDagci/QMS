using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM01TaskAndRiskSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDepartmentNameSnapshot",
                schema: "workflow",
                table: "task_assignment",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedUserNameSnapshot",
                schema: "workflow",
                table: "task_assignment",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskMatrixVersion",
                schema: "deviation",
                table: "deviation",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "M01-RISK-1.0");

            migrationBuilder.Sql("""
                UPDATE workflow.task_assignment AS task
                SET "AssignedUserNameSnapshot" = (SELECT usr."DisplayName" FROM identity."user" AS usr WHERE usr."Id" = task."AssignedUserId"),
                    "AssignedDepartmentNameSnapshot" = (SELECT department."Name" FROM organization.department AS department WHERE department."Id" = task."AssignedDepartmentId")
                WHERE task."AggregateType" = 'Deviation';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedDepartmentNameSnapshot",
                schema: "workflow",
                table: "task_assignment");

            migrationBuilder.DropColumn(
                name: "AssignedUserNameSnapshot",
                schema: "workflow",
                table: "task_assignment");

            migrationBuilder.DropColumn(
                name: "RiskMatrixVersion",
                schema: "deviation",
                table: "deviation");
        }
    }
}
