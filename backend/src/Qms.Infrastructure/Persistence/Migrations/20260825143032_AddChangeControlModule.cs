using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeControlModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "change_control");

            migrationBuilder.CreateTable(
                name: "change_control",
                schema: "change_control",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCapaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ProposedState = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Justification = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    Scope = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    IsTemporary = table.Column<bool>(type: "boolean", nullable: false),
                    TemporaryUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Owner = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TargetDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RiskSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ProductImpact = table.Column<bool>(type: "boolean", nullable: false),
                    SiteImpact = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationRequired = table.Column<bool>(type: "boolean", nullable: false),
                    RegulatoryImpact = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RollbackPlan = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    AuthorityApprovalReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CommissionedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostImplementationResult = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    ClosureNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_control", x => x.Id);
                    table.ForeignKey(
                        name: "FK_change_control_capa_SourceCapaId",
                        column: x => x.SourceCapaId,
                        principalSchema: "capa",
                        principalTable: "capa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_change_control_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assessment",
                schema: "change_control",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeControlId = table.Column<Guid>(type: "uuid", nullable: false),
                    Department = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Reviewer = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ImpactSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequiredActions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assessment_change_control_ChangeControlId",
                        column: x => x.ChangeControlId,
                        principalSchema: "change_control",
                        principalTable: "change_control",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "implementation_action",
                schema: "change_control",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeControlId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Owner = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TargetDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsBlocking = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CompletionEvidence = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    VerificationNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_implementation_action", x => x.Id);
                    table.ForeignKey(
                        name: "FK_implementation_action_change_control_ChangeControlId",
                        column: x => x.ChangeControlId,
                        principalSchema: "change_control",
                        principalTable: "change_control",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assessment_ChangeControlId_Department",
                schema: "change_control",
                table: "assessment",
                columns: new[] { "ChangeControlId", "Department" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_change_control_QualityRecordId",
                schema: "change_control",
                table: "change_control",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_change_control_SourceCapaId",
                schema: "change_control",
                table: "change_control",
                column: "SourceCapaId");

            migrationBuilder.CreateIndex(
                name: "IX_change_control_Status_TargetDateUtc",
                schema: "change_control",
                table: "change_control",
                columns: new[] { "Status", "TargetDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_implementation_action_ChangeControlId_Status",
                schema: "change_control",
                table: "implementation_action",
                columns: new[] { "ChangeControlId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessment",
                schema: "change_control");

            migrationBuilder.DropTable(
                name: "implementation_action",
                schema: "change_control");

            migrationBuilder.DropTable(
                name: "change_control",
                schema: "change_control");
        }
    }
}
