using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "training");

            migrationBuilder.CreateTable(
                name: "matrix_rule",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CourseCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CourseTitle = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssessmentMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeliveryMethod = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PassingScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ValidityMonths = table.Column<int>(type: "integer", nullable: false),
                    IsCriticalQualification = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matrix_rule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_matrix_rule_controlled_document_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalSchema: "document",
                        principalTable: "controlled_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assignment",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatrixRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ControlledDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentTrainingRequirementId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CourseCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CourseTitle = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    AssessmentMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeliveryMethod = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PassingScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ValidityMonths = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    IsCriticalQualification = table.Column<bool>(type: "boolean", nullable: false),
                    PlannedYear = table.Column<int>(type: "integer", nullable: false),
                    SessionCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Trainer = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcknowledgementMeaning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TrainerApprovalNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignment_controlled_document_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalSchema: "document",
                        principalTable: "controlled_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assignment_matrix_rule_MatrixRuleId",
                        column: x => x.MatrixRuleId,
                        principalSchema: "training",
                        principalTable: "matrix_rule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assignment_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assessment_attempt",
                schema: "training",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PracticalPassed = table.Column<bool>(type: "boolean", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    Evidence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Evaluator = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AssessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessment_attempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assessment_attempt_assignment_TrainingAssignmentId",
                        column: x => x.TrainingAssignmentId,
                        principalSchema: "training",
                        principalTable: "assignment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assessment_attempt_TrainingAssignmentId_AttemptNumber",
                schema: "training",
                table: "assessment_attempt",
                columns: new[] { "TrainingAssignmentId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assignment_ControlledDocumentId",
                schema: "training",
                table: "assignment",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_DocumentTrainingRequirementId",
                schema: "training",
                table: "assignment",
                column: "DocumentTrainingRequirementId",
                unique: true,
                filter: "\"DocumentTrainingRequirementId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_EmployeeUserId_Status_DueAtUtc",
                schema: "training",
                table: "assignment",
                columns: new[] { "EmployeeUserId", "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_MatrixRuleId",
                schema: "training",
                table: "assignment",
                column: "MatrixRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_QualityRecordId",
                schema: "training",
                table: "assignment",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matrix_rule_ControlledDocumentId",
                schema: "training",
                table: "matrix_rule",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_rule_Position_CourseCode",
                schema: "training",
                table: "matrix_rule",
                columns: new[] { "Position", "CourseCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessment_attempt",
                schema: "training");

            migrationBuilder.DropTable(
                name: "assignment",
                schema: "training");

            migrationBuilder.DropTable(
                name: "matrix_rule",
                schema: "training");
        }
    }
}
