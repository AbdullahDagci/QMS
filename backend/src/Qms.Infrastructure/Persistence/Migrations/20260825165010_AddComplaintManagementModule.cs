using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintManagementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "complaint");

            migrationBuilder.CreateTable(
                name: "complaint",
                schema: "complaint",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BatchNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    EventAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ComplaintType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HasHealthImpact = table.Column<bool>(type: "boolean", nullable: false),
                    SuspectedAdverseEvent = table.Column<bool>(type: "boolean", nullable: false),
                    SampleExpected = table.Column<bool>(type: "boolean", nullable: false),
                    ReturnExpected = table.Column<bool>(type: "boolean", nullable: false),
                    AttachmentSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PreliminaryResponseDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinalResponseDueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SimilarComplaintCount = table.Column<int>(type: "integer", nullable: false),
                    TrendFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedDeviationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedCapaId = table.Column<Guid>(type: "uuid", nullable: true),
                    PharmacovigilanceRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    PharmacovigilanceStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ImpactAssessment = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ConfirmedRootCause = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CapaRequired = table.Column<bool>(type: "boolean", nullable: true),
                    ClosureNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_capa_LinkedCapaId",
                        column: x => x.LinkedCapaId,
                        principalSchema: "capa",
                        principalTable: "capa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaint_deviation_LinkedDeviationId",
                        column: x => x.LinkedDeviationId,
                        principalSchema: "deviation",
                        principalTable: "deviation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaint_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "investigation",
                schema: "complaint",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    Department = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Investigator = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Findings = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RootCauseContribution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investigation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_investigation_complaint_ComplaintId",
                        column: x => x.ComplaintId,
                        principalSchema: "complaint",
                        principalTable: "complaint",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "response_version",
                schema: "complaint",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplaintId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreparedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_response_version", x => x.Id);
                    table.ForeignKey(
                        name: "FK_response_version_complaint_ComplaintId",
                        column: x => x.ComplaintId,
                        principalSchema: "complaint",
                        principalTable: "complaint",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_LinkedCapaId",
                schema: "complaint",
                table: "complaint",
                column: "LinkedCapaId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_LinkedDeviationId",
                schema: "complaint",
                table: "complaint",
                column: "LinkedDeviationId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_Product_ComplaintType_CreatedAtUtc",
                schema: "complaint",
                table: "complaint",
                columns: new[] { "Product", "ComplaintType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_QualityRecordId",
                schema: "complaint",
                table: "complaint",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_complaint_Status_FinalResponseDueAtUtc",
                schema: "complaint",
                table: "complaint",
                columns: new[] { "Status", "FinalResponseDueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_investigation_ComplaintId_Department",
                schema: "complaint",
                table: "investigation",
                columns: new[] { "ComplaintId", "Department" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_response_version_ComplaintId_ResponseType_VersionNumber",
                schema: "complaint",
                table: "response_version",
                columns: new[] { "ComplaintId", "ResponseType", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "investigation",
                schema: "complaint");

            migrationBuilder.DropTable(
                name: "response_version",
                schema: "complaint");

            migrationBuilder.DropTable(
                name: "complaint",
                schema: "complaint");
        }
    }
}
