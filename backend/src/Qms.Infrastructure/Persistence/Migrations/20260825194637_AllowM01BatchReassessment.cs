using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowM01BatchReassessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deviation_batch_impact_DeviationId_BatchNumber",
                schema: "deviation",
                table: "deviation_batch_impact");

            migrationBuilder.CreateIndex(
                name: "IX_deviation_batch_impact_DeviationId_BatchNumber_AssessedAtUtc",
                schema: "deviation",
                table: "deviation_batch_impact",
                columns: new[] { "DeviationId", "BatchNumber", "AssessedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deviation_batch_impact_DeviationId_BatchNumber_AssessedAtUtc",
                schema: "deviation",
                table: "deviation_batch_impact");

            migrationBuilder.CreateIndex(
                name: "IX_deviation_batch_impact_DeviationId_BatchNumber",
                schema: "deviation",
                table: "deviation_batch_impact",
                columns: new[] { "DeviationId", "BatchNumber" },
                unique: true);
        }
    }
}
