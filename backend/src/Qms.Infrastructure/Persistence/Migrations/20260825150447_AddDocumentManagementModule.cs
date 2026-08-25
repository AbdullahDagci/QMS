using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "document");

            migrationBuilder.CreateTable(
                name: "controlled_document",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceChangeControlId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Owner = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Department = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Confidentiality = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReviewPeriodMonths = table.Column<int>(type: "integer", nullable: false),
                    PlannedEffectiveDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NextReviewDateUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    WithdrawalReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controlled_document", x => x.Id);
                    table.ForeignKey(
                        name: "FK_controlled_document_change_control_SourceChangeControlId",
                        column: x => x.SourceChangeControlId,
                        principalSchema: "change_control",
                        principalTable: "change_control",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_controlled_document_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "controlled_copy",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CopyNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DueBackAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReturnedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DestroyedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controlled_copy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_controlled_copy_controlled_document_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalSchema: "document",
                        principalTable: "controlled_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "read_receipt",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SignatureMeaning = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_read_receipt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_read_receipt_controlled_document_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalSchema: "document",
                        principalTable: "controlled_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Department = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Reviewer = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review", x => x.Id);
                    table.ForeignKey(
                        name: "FK_review_controlled_document_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalSchema: "document",
                        principalTable: "controlled_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "revision",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MajorVersion = table.Column<int>(type: "integer", nullable: false),
                    MinorVersion = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PreparedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_revision_controlled_document_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalSchema: "document",
                        principalTable: "controlled_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_requirement",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AssignedUser = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Evidence = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_requirement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_training_requirement_controlled_document_ControlledDocument~",
                        column: x => x.ControlledDocumentId,
                        principalSchema: "document",
                        principalTable: "controlled_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_controlled_copy_ControlledDocumentId_CopyNumber",
                schema: "document",
                table: "controlled_copy",
                columns: new[] { "ControlledDocumentId", "CopyNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_controlled_document_DocumentCode",
                schema: "document",
                table: "controlled_document",
                column: "DocumentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_controlled_document_QualityRecordId",
                schema: "document",
                table: "controlled_document",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_controlled_document_SourceChangeControlId",
                schema: "document",
                table: "controlled_document",
                column: "SourceChangeControlId");

            migrationBuilder.CreateIndex(
                name: "IX_controlled_document_Status_NextReviewDateUtc",
                schema: "document",
                table: "controlled_document",
                columns: new[] { "Status", "NextReviewDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_read_receipt_ControlledDocumentId",
                schema: "document",
                table: "read_receipt",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_read_receipt_RevisionId_UserId",
                schema: "document",
                table: "read_receipt",
                columns: new[] { "RevisionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_ControlledDocumentId",
                schema: "document",
                table: "review",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_review_RevisionId_Department",
                schema: "document",
                table: "review",
                columns: new[] { "RevisionId", "Department" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revision_ControlledDocumentId_MajorVersion_MinorVersion",
                schema: "document",
                table: "revision",
                columns: new[] { "ControlledDocumentId", "MajorVersion", "MinorVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_requirement_ControlledDocumentId",
                schema: "document",
                table: "training_requirement",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_training_requirement_RevisionId_Position",
                schema: "document",
                table: "training_requirement",
                columns: new[] { "RevisionId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "controlled_copy",
                schema: "document");

            migrationBuilder.DropTable(
                name: "read_receipt",
                schema: "document");

            migrationBuilder.DropTable(
                name: "review",
                schema: "document");

            migrationBuilder.DropTable(
                name: "revision",
                schema: "document");

            migrationBuilder.DropTable(
                name: "training_requirement",
                schema: "document");

            migrationBuilder.DropTable(
                name: "controlled_document",
                schema: "document");
        }
    }
}
