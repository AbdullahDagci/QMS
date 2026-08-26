using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM10WorkTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "work_tracking");

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "work_tracking",
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
                    table.PrimaryKey("PK_lookup_definition6", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "work_item",
                schema: "work_tracking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceModule = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SourceRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceRecordNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Description = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerDepartment = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    VerifierUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Verifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletionEvidence = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    VerificationNote = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_item_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_item_user_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_item_user_VerifierUserId",
                        column: x => x.VerifierUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_Code6",
                schema: "work_tracking",
                table: "lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_item_DueAtUtc_Status",
                schema: "work_tracking",
                table: "work_item",
                columns: new[] { "DueAtUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_work_item_OwnerUserId_Status",
                schema: "work_tracking",
                table: "work_item",
                columns: new[] { "OwnerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_work_item_QualityRecordId",
                schema: "work_tracking",
                table: "work_item",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_item_VerifierUserId",
                schema: "work_tracking",
                table: "work_item",
                column: "VerifierUserId");

            migrationBuilder.Sql("""
                INSERT INTO work_tracking.lookup_definition ("Id","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc") VALUES
                (gen_random_uuid(),'Category','GeneralAction','Genel aksiyon',10,true,NOW(),NOW()),
                (gen_random_uuid(),'Category','FollowUp','Takip işi',20,true,NOW(),NOW()),
                (gen_random_uuid(),'Category','Commitment','Taahhüt',30,true,NOW(),NOW()),
                (gen_random_uuid(),'Category','Improvement','İyileştirme',40,true,NOW(),NOW()),
                (gen_random_uuid(),'Priority','Low','Düşük',10,true,NOW(),NOW()),
                (gen_random_uuid(),'Priority','Normal','Normal',20,true,NOW(),NOW()),
                (gen_random_uuid(),'Priority','High','Yüksek',30,true,NOW(),NOW()),
                (gen_random_uuid(),'Priority','Critical','Kritik',40,true,NOW(),NOW());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "work_tracking");

            migrationBuilder.DropTable(
                name: "work_item",
                schema: "work_tracking");
        }
    }
}
