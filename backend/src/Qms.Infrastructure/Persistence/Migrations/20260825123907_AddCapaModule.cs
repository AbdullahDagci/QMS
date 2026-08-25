using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCapaModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "capa");

            migrationBuilder.CreateTable(
                name: "capa",
                schema: "capa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDeviationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RootCause = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ImmediateActions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Owner = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TargetDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectivenessRequired = table.Column<bool>(type: "boolean", nullable: false),
                    EffectivenessMethod = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EffectivenessSample = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ObservationPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    SuccessCriteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EffectivenessEvaluator = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EffectivenessDueDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsEffective = table.Column<bool>(type: "boolean", nullable: true),
                    EffectivenessResult = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ClosureNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_capa_deviation_SourceDeviationId",
                        column: x => x.SourceDeviationId,
                        principalSchema: "deviation",
                        principalTable: "deviation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_capa_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "capa_action",
                schema: "capa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Owner = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TargetDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CompletionEvidence = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    VerificationNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capa_action", x => x.Id);
                    table.ForeignKey(
                        name: "FK_capa_action_capa_CapaId",
                        column: x => x.CapaId,
                        principalSchema: "capa",
                        principalTable: "capa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_capa_QualityRecordId",
                schema: "capa",
                table: "capa",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_capa_SourceDeviationId",
                schema: "capa",
                table: "capa",
                column: "SourceDeviationId",
                unique: true,
                filter: "\"SourceDeviationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_capa_Status_TargetDateUtc",
                schema: "capa",
                table: "capa",
                columns: new[] { "Status", "TargetDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_capa_action_CapaId_TargetDateUtc",
                schema: "capa",
                table: "capa_action",
                columns: new[] { "CapaId", "TargetDateUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "capa_action",
                schema: "capa");

            migrationBuilder.DropTable(
                name: "capa",
                schema: "capa");
        }
    }
}
