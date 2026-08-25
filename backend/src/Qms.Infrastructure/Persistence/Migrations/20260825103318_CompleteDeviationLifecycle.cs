using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDeviationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAtUtc",
                schema: "deviation",
                table: "deviation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosureJustification",
                schema: "deviation",
                table: "deviation",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EffectivenessAssessmentNote",
                schema: "deviation",
                table: "deviation",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EffectivenessRequired",
                schema: "deviation",
                table: "deviation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreliminaryReviewNote",
                schema: "deviation",
                table: "deviation",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityAssessmentNote",
                schema: "deviation",
                table: "deviation",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "deviation_batch_impact",
                schema: "deviation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsAffected = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Disposition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rationale = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AssessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deviation_batch_impact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deviation_batch_impact_deviation_DeviationId",
                        column: x => x.DeviationId,
                        principalSchema: "deviation",
                        principalTable: "deviation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deviation_investigation",
                schema: "deviation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RootCauseCategory = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RootCauseDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Conclusion = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    InvestigatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deviation_investigation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deviation_investigation_deviation_DeviationId",
                        column: x => x.DeviationId,
                        principalSchema: "deviation",
                        principalTable: "deviation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deviation_batch_impact_DeviationId_BatchNumber",
                schema: "deviation",
                table: "deviation_batch_impact",
                columns: new[] { "DeviationId", "BatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deviation_batch_impact_Disposition_IsLocked",
                schema: "deviation",
                table: "deviation_batch_impact",
                columns: new[] { "Disposition", "IsLocked" });

            migrationBuilder.CreateIndex(
                name: "IX_deviation_investigation_DeviationId_CompletedAtUtc",
                schema: "deviation",
                table: "deviation_investigation",
                columns: new[] { "DeviationId", "CompletedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deviation_batch_impact",
                schema: "deviation");

            migrationBuilder.DropTable(
                name: "deviation_investigation",
                schema: "deviation");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                schema: "deviation",
                table: "deviation");

            migrationBuilder.DropColumn(
                name: "ClosureJustification",
                schema: "deviation",
                table: "deviation");

            migrationBuilder.DropColumn(
                name: "EffectivenessAssessmentNote",
                schema: "deviation",
                table: "deviation");

            migrationBuilder.DropColumn(
                name: "EffectivenessRequired",
                schema: "deviation",
                table: "deviation");

            migrationBuilder.DropColumn(
                name: "PreliminaryReviewNote",
                schema: "deviation",
                table: "deviation");

            migrationBuilder.DropColumn(
                name: "QualityAssessmentNote",
                schema: "deviation",
                table: "deviation");
        }
    }
}
