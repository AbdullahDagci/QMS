using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalAuditModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_finding",
                schema: "internal_audit",
                table: "finding");

            migrationBuilder.EnsureSchema(
                name: "external_audit");

            migrationBuilder.RenameIndex(
                name: "IX_finding_Number",
                schema: "internal_audit",
                table: "finding",
                newName: "IX_finding_Number1");

            migrationBuilder.RenameIndex(
                name: "IX_finding_LinkedCapaId",
                schema: "internal_audit",
                table: "finding",
                newName: "IX_finding_LinkedCapaId1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_finding1",
                schema: "internal_audit",
                table: "finding",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "external_audit",
                schema: "external_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    AuditKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AuditorOrganization = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    IsGovernmentAuthority = table.Column<bool>(type: "boolean", nullable: false),
                    AuthorityCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OfficialReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Site = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NotifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlannedStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlannedEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResponseDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AuthorizedCloserUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizedCloser = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClosureLetterReference = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ClosureEvidence = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AuthorityAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    ClosureLetterReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosureNote = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_audit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_external_audit_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document_request",
                schema: "external_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlledDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Confidentiality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExportVersion = table.Column<int>(type: "integer", nullable: false),
                    ExportedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_request", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_request_controlled_document_ControlledDocumentId",
                        column: x => x.ControlledDocumentId,
                        principalSchema: "document",
                        principalTable: "controlled_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_request_external_audit_ExternalAuditId",
                        column: x => x.ExternalAuditId,
                        principalSchema: "external_audit",
                        principalTable: "external_audit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "finding",
                schema: "external_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    OfficialReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Classification = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CapaRequired = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedCapaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResponseDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OfficialResponse = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Commitment = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    CommitmentDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                        name: "FK_finding_external_audit_ExternalAuditId",
                        column: x => x.ExternalAuditId,
                        principalSchema: "external_audit",
                        principalTable: "external_audit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "package_access",
                schema: "external_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalAuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExportedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Evidence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExportVersion = table.Column<int>(type: "integer", nullable: false),
                    AccessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_package_access", x => x.Id);
                    table.ForeignKey(
                        name: "FK_package_access_document_request_DocumentRequestId",
                        column: x => x.DocumentRequestId,
                        principalSchema: "external_audit",
                        principalTable: "document_request",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_request_ControlledDocumentId",
                schema: "external_audit",
                table: "document_request",
                column: "ControlledDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_document_request_ExternalAuditId_DocumentCode",
                schema: "external_audit",
                table: "document_request",
                columns: new[] { "ExternalAuditId", "DocumentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_external_audit_IsGovernmentAuthority_Status",
                schema: "external_audit",
                table: "external_audit",
                columns: new[] { "IsGovernmentAuthority", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_external_audit_QualityRecordId",
                schema: "external_audit",
                table: "external_audit",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_finding_ExternalAuditId",
                schema: "external_audit",
                table: "finding",
                column: "ExternalAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_finding_LinkedCapaId",
                schema: "external_audit",
                table: "finding",
                column: "LinkedCapaId");

            migrationBuilder.CreateIndex(
                name: "IX_finding_Number",
                schema: "external_audit",
                table: "finding",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_package_access_DocumentRequestId",
                schema: "external_audit",
                table: "package_access",
                column: "DocumentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_package_access_ExternalAuditId_AccessedAtUtc",
                schema: "external_audit",
                table: "package_access",
                columns: new[] { "ExternalAuditId", "AccessedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "finding",
                schema: "external_audit");

            migrationBuilder.DropTable(
                name: "package_access",
                schema: "external_audit");

            migrationBuilder.DropTable(
                name: "document_request",
                schema: "external_audit");

            migrationBuilder.DropTable(
                name: "external_audit",
                schema: "external_audit");

            migrationBuilder.DropPrimaryKey(
                name: "PK_finding1",
                schema: "internal_audit",
                table: "finding");

            migrationBuilder.RenameIndex(
                name: "IX_finding_Number1",
                schema: "internal_audit",
                table: "finding",
                newName: "IX_finding_Number");

            migrationBuilder.RenameIndex(
                name: "IX_finding_LinkedCapaId1",
                schema: "internal_audit",
                table: "finding",
                newName: "IX_finding_LinkedCapaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_finding",
                schema: "internal_audit",
                table: "finding",
                column: "Id");
        }
    }
}
