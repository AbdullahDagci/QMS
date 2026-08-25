using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalAuditModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "internal_audit");

            migrationBuilder.CreateTable(
                name: "internal_audit",
                schema: "internal_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanYear = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    AuditType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Scope = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    Objectives = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Criteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AuditeeDepartment = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LeadAuditorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadAuditor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LeadAuditorDepartment = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PlannedStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlannedEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsUnplanned = table.Column<bool>(type: "boolean", nullable: false),
                    UnplannedReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ChecklistVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ChecklistLockedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IndependenceConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    Summary = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internal_audit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_internal_audit_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checklist_item",
                schema: "internal_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Question = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Reference = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Evidence = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AnsweredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checklist_item_internal_audit_InternalAuditId",
                        column: x => x.InternalAuditId,
                        principalSchema: "internal_audit",
                        principalTable: "internal_audit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "finding",
                schema: "internal_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    RequirementReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Impact = table.Column<int>(type: "integer", nullable: false),
                    Likelihood = table.Column<int>(type: "integer", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    Classification = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CapaRequired = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedCapaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Response = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    CorrectiveAction = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    VerificationNote = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_finding", x => x.Id);
                    table.ForeignKey(
                        name: "FK_finding_capa_LinkedCapaId",
                        column: x => x.LinkedCapaId,
                        principalSchema: "capa",
                        principalTable: "capa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_finding_internal_audit_InternalAuditId",
                        column: x => x.InternalAuditId,
                        principalSchema: "internal_audit",
                        principalTable: "internal_audit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checklist_item_InternalAuditId_Order",
                schema: "internal_audit",
                table: "checklist_item",
                columns: new[] { "InternalAuditId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_finding_InternalAuditId",
                schema: "internal_audit",
                table: "finding",
                column: "InternalAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_finding_LinkedCapaId",
                schema: "internal_audit",
                table: "finding",
                column: "LinkedCapaId");

            migrationBuilder.CreateIndex(
                name: "IX_finding_Number",
                schema: "internal_audit",
                table: "finding",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_internal_audit_PlanYear_Status",
                schema: "internal_audit",
                table: "internal_audit",
                columns: new[] { "PlanYear", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_internal_audit_QualityRecordId",
                schema: "internal_audit",
                table: "internal_audit",
                column: "QualityRecordId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checklist_item",
                schema: "internal_audit");

            migrationBuilder.DropTable(
                name: "finding",
                schema: "internal_audit");

            migrationBuilder.DropTable(
                name: "internal_audit",
                schema: "internal_audit");
        }
    }
}
