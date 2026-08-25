using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "deviation");

            migrationBuilder.CreateTable(
                name: "deviation",
                schema: "deviation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ExpectedState = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ImmediateAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DeviationType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DetectedDepartment = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ProcessStage = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DetectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TargetDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Likelihood = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Detectability = table.Column<int>(type: "integer", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    Classification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CapaRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deviation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deviation_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "record_number_sequence",
                schema: "core",
                columns: table => new
                {
                    RecordType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CalendarYear = table.Column<int>(type: "integer", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_record_number_sequence", x => new { x.RecordType, x.CalendarYear });
                });

            migrationBuilder.CreateIndex(
                name: "IX_deviation_Classification_CreatedAtUtc",
                schema: "deviation",
                table: "deviation",
                columns: new[] { "Classification", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_deviation_QualityRecordId",
                schema: "deviation",
                table: "deviation",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deviation_Status_TargetDateUtc",
                schema: "deviation",
                table: "deviation",
                columns: new[] { "Status", "TargetDateUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deviation",
                schema: "deviation");

            migrationBuilder.DropTable(
                name: "record_number_sequence",
                schema: "core");
        }
    }
}
