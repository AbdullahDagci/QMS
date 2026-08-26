using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM11RiskManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition6",
                schema: "work_tracking",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition4",
                schema: "supplier_audit",
                table: "lookup_definition");

            migrationBuilder.EnsureSchema(
                name: "risk_management");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code6",
                schema: "work_tracking",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code7");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code5",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code6");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code4",
                schema: "supplier_audit",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code5");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition7",
                schema: "work_tracking",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition6",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "supplier_audit",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "risk_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_definition4", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "risk_assessment",
                schema: "risk_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Process = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Scope = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Methodology = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MatrixVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ActionThreshold = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerDepartment = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ApproverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Approver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_assessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_risk_assessment_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_risk_assessment_user_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_risk_assessment_user_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "risk_item",
                schema: "risk_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskAssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FailureMode = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Effect = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Cause = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExistingControls = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Occurrence = table.Column<int>(type: "integer", nullable: false),
                    Detectability = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    ActionOwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ActionDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActionEvidence = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    ResidualSeverity = table.Column<int>(type: "integer", nullable: true),
                    ResidualOccurrence = table.Column<int>(type: "integer", nullable: true),
                    ResidualDetectability = table.Column<int>(type: "integer", nullable: true),
                    ResidualRationale = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_risk_item_risk_assessment_RiskAssessmentId",
                        column: x => x.RiskAssessmentId,
                        principalSchema: "risk_management",
                        principalTable: "risk_assessment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_risk_item_user_ActionOwnerUserId",
                        column: x => x.ActionOwnerUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_Code4",
                schema: "risk_management",
                table: "lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_risk_assessment_ApproverUserId",
                schema: "risk_management",
                table: "risk_assessment",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_risk_assessment_OwnerUserId",
                schema: "risk_management",
                table: "risk_assessment",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_risk_assessment_QualityRecordId",
                schema: "risk_management",
                table: "risk_assessment",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_risk_item_ActionOwnerUserId",
                schema: "risk_management",
                table: "risk_item",
                column: "ActionOwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_risk_item_RiskAssessmentId",
                schema: "risk_management",
                table: "risk_item",
                column: "RiskAssessmentId");

            migrationBuilder.Sql("""
                INSERT INTO risk_management.lookup_definition ("Id","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc") VALUES
                (gen_random_uuid(),'Category','Process','Proses riski',10,true,NOW(),NOW()),
                (gen_random_uuid(),'Category','Product','Ürün riski',20,true,NOW(),NOW()),
                (gen_random_uuid(),'Category','System','Sistem riski',30,true,NOW(),NOW()),
                (gen_random_uuid(),'Category','Supplier','Tedarikçi riski',40,true,NOW(),NOW()),
                (gen_random_uuid(),'Methodology','FMEA','Hata Türleri ve Etkileri Analizi (FMEA)',10,true,NOW(),NOW()),
                (gen_random_uuid(),'Methodology','FMECA','Kritiklik Analizli FMEA (FMECA)',20,true,NOW(),NOW()),
                (gen_random_uuid(),'MatrixVersion','M11-FMEA-1.0','M.11 FMEA Matrisi v1.0',10,true,NOW(),NOW());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "risk_management");

            migrationBuilder.DropTable(
                name: "risk_item",
                schema: "risk_management");

            migrationBuilder.DropTable(
                name: "risk_assessment",
                schema: "risk_management");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition7",
                schema: "work_tracking",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition6",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "supplier_audit",
                table: "lookup_definition");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code7",
                schema: "work_tracking",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code6");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code6",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code5");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code5",
                schema: "supplier_audit",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition6",
                schema: "work_tracking",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition4",
                schema: "supplier_audit",
                table: "lookup_definition",
                column: "Id");
        }
    }
}
