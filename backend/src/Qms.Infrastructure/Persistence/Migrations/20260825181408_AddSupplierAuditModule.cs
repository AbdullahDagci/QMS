using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierAuditModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "supplier_audit");

            migrationBuilder.CreateTable(
                name: "supplier_audit",
                schema: "supplier_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierEvaluationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    SupplierScope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaterialOrService = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Criticality = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PastPerformanceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    OpenFindingSnapshot = table.Column<int>(type: "integer", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    RiskBand = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RecommendedFrequencyMonths = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Site = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    LeadAuditorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadAuditor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LeadAuditorDepartment = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PurchasingOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PlannedStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlannedEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChecklistVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ChecklistLockedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    QualificationStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ResultRationale = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    QualificationValidUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequalificationRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_audit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_audit_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checklist_item",
                schema: "supplier_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Question = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    Reference = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Evidence = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Note = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    AnsweredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_item1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_checklist_item_supplier_audit_SupplierAuditId",
                        column: x => x.SupplierAuditId,
                        principalSchema: "supplier_audit",
                        principalTable: "supplier_audit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "finding",
                schema: "supplier_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    RequirementReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Classification = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CapaRequired = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedCapaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResponseDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SupplierResponse = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Commitment = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    CommitmentDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Evidence = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    VerificationNote = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_finding2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_finding_capa_LinkedCapaId",
                        column: x => x.LinkedCapaId,
                        principalSchema: "capa",
                        principalTable: "capa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_finding_supplier_audit_SupplierAuditId",
                        column: x => x.SupplierAuditId,
                        principalSchema: "supplier_audit",
                        principalTable: "supplier_audit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invitation",
                schema: "supplier_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invitation_supplier_audit_SupplierAuditId",
                        column: x => x.SupplierAuditId,
                        principalSchema: "supplier_audit",
                        principalTable: "supplier_audit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checklist_item_SupplierAuditId_Order",
                schema: "supplier_audit",
                table: "checklist_item",
                columns: new[] { "SupplierAuditId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_finding_LinkedCapaId2",
                schema: "supplier_audit",
                table: "finding",
                column: "LinkedCapaId");

            migrationBuilder.CreateIndex(
                name: "IX_finding_Number2",
                schema: "supplier_audit",
                table: "finding",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_finding_SupplierAuditId",
                schema: "supplier_audit",
                table: "finding",
                column: "SupplierAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_invitation_SupplierAuditId_ExpiresAtUtc",
                schema: "supplier_audit",
                table: "invitation",
                columns: new[] { "SupplierAuditId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_invitation_TokenHash",
                schema: "supplier_audit",
                table: "invitation",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_audit_QualityRecordId",
                schema: "supplier_audit",
                table: "supplier_audit",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_audit_RiskBand_Status",
                schema: "supplier_audit",
                table: "supplier_audit",
                columns: new[] { "RiskBand", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_audit_SupplierCode_SupplierScope",
                schema: "supplier_audit",
                table: "supplier_audit",
                columns: new[] { "SupplierCode", "SupplierScope" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_finding_capa_LinkedCapaId",
                schema: "external_audit",
                table: "finding");

            migrationBuilder.DropTable(
                name: "checklist_item",
                schema: "supplier_audit");

            migrationBuilder.DropTable(
                name: "finding",
                schema: "supplier_audit");

            migrationBuilder.DropTable(
                name: "invitation",
                schema: "supplier_audit");

            migrationBuilder.DropTable(
                name: "supplier_audit",
                schema: "supplier_audit");
        }
    }
}
