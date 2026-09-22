using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScopedRecordAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "record_access_grant",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_record_access_grant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_record_access_grant_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_access_grant_task_assignment_AssignmentId",
                        column: x => x.AssignmentId,
                        principalSchema: "workflow",
                        principalTable: "task_assignment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_record_access_grant_AssignmentId",
                schema: "identity",
                table: "record_access_grant",
                column: "AssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_record_access_grant_QualityRecordId",
                schema: "identity",
                table: "record_access_grant",
                column: "QualityRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_record_access_grant_UserId_QualityRecordId",
                schema: "identity",
                table: "record_access_grant",
                columns: new[] { "UserId", "QualityRecordId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "record_access_grant",
                schema: "identity");
        }
    }
}
