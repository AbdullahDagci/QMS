using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM05IdentityAndManagedLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_matrix_rule_Position_CourseCode",
                schema: "training",
                table: "matrix_rule");

            migrationBuilder.AddColumn<Guid>(
                name: "PositionId",
                schema: "training",
                table: "matrix_rule",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PositionId",
                schema: "training",
                table: "assignment",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_definition1", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO training.lookup_definition ("Id", "Category", "Code", "Name", "SortOrder", "IsActive", "CreatedAtUtc", "UpdatedAtUtc") VALUES
                ('01a05b00-0001-7000-8000-000000000001','AssessmentMode','ReadAndAcknowledge','Oku ve Anla',10,TRUE,TIMESTAMPTZ '2026-08-25T23:32:12+00:00',TIMESTAMPTZ '2026-08-25T23:32:12+00:00'),
                ('01a05b00-0001-7000-8000-000000000002','AssessmentMode','Exam','Sınav',20,TRUE,TIMESTAMPTZ '2026-08-25T23:32:12+00:00',TIMESTAMPTZ '2026-08-25T23:32:12+00:00'),
                ('01a05b00-0001-7000-8000-000000000003','AssessmentMode','Practical','Pratik Yeterlilik',30,TRUE,TIMESTAMPTZ '2026-08-25T23:32:12+00:00',TIMESTAMPTZ '2026-08-25T23:32:12+00:00'),
                ('01a05b00-0001-7000-8000-000000000004','AssessmentMode','ExamAndPractical','Sınav + Pratik',40,TRUE,TIMESTAMPTZ '2026-08-25T23:32:12+00:00',TIMESTAMPTZ '2026-08-25T23:32:12+00:00'),
                ('01a05b00-0002-7000-8000-000000000001','DeliveryMethod','Electronic','Elektronik',10,TRUE,TIMESTAMPTZ '2026-08-25T23:32:12+00:00',TIMESTAMPTZ '2026-08-25T23:32:12+00:00'),
                ('01a05b00-0002-7000-8000-000000000002','DeliveryMethod','Classroom','Sınıf',20,TRUE,TIMESTAMPTZ '2026-08-25T23:32:12+00:00',TIMESTAMPTZ '2026-08-25T23:32:12+00:00'),
                ('01a05b00-0002-7000-8000-000000000003','DeliveryMethod','OnTheJob','İş Başı',30,TRUE,TIMESTAMPTZ '2026-08-25T23:32:12+00:00',TIMESTAMPTZ '2026-08-25T23:32:12+00:00'),
                ('01a05b00-0002-7000-8000-000000000004','DeliveryMethod','Hybrid','Hibrit',40,TRUE,TIMESTAMPTZ '2026-08-25T23:32:12+00:00',TIMESTAMPTZ '2026-08-25T23:32:12+00:00');
                UPDATE training.matrix_rule m SET "PositionId"=p."Id" FROM organization.position p WHERE m."Position"=p."Name" AND p."IsActive";
                UPDATE training.assignment a SET "PositionId"=p."Id" FROM organization.position p WHERE a."Position"=p."Name" AND p."IsActive";
                UPDATE training.assignment a SET "PositionId"=up."PositionId" FROM organization.user_position up WHERE a."PositionId" IS NULL AND up."UserId"=a."EmployeeUserId" AND up."EndsAtUtc" IS NULL AND up."IsPrimary";
                """);

            migrationBuilder.AlterColumn<Guid>(name: "PositionId", schema: "training", table: "matrix_rule", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
            migrationBuilder.AlterColumn<Guid>(name: "PositionId", schema: "training", table: "assignment", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_matrix_rule_PositionId_CourseCode",
                schema: "training",
                table: "matrix_rule",
                columns: new[] { "PositionId", "CourseCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assignment_PositionId",
                schema: "training",
                table: "assignment",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_Code1",
                schema: "training",
                table: "lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder1",
                schema: "training",
                table: "lookup_definition",
                columns: new[] { "Category", "IsActive", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_assignment_position_PositionId",
                schema: "training",
                table: "assignment",
                column: "PositionId",
                principalSchema: "organization",
                principalTable: "position",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_assignment_user_EmployeeUserId",
                schema: "training",
                table: "assignment",
                column: "EmployeeUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_matrix_rule_position_PositionId",
                schema: "training",
                table: "matrix_rule",
                column: "PositionId",
                principalSchema: "organization",
                principalTable: "position",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assignment_position_PositionId",
                schema: "training",
                table: "assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_assignment_user_EmployeeUserId",
                schema: "training",
                table: "assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_matrix_rule_position_PositionId",
                schema: "training",
                table: "matrix_rule");

            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "training");

            migrationBuilder.DropIndex(
                name: "IX_matrix_rule_PositionId_CourseCode",
                schema: "training",
                table: "matrix_rule");

            migrationBuilder.DropIndex(
                name: "IX_assignment_PositionId",
                schema: "training",
                table: "assignment");

            migrationBuilder.DropColumn(
                name: "PositionId",
                schema: "training",
                table: "matrix_rule");

            migrationBuilder.DropColumn(
                name: "PositionId",
                schema: "training",
                table: "assignment");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_rule_Position_CourseCode",
                schema: "training",
                table: "matrix_rule",
                columns: new[] { "Position", "CourseCode" },
                unique: true);
        }
    }
}
