using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupportPositionWideTrainingAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_assignment_DocumentTrainingRequirementId",
                schema: "training",
                table: "assignment");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_DocumentTrainingRequirementId",
                schema: "training",
                table: "assignment",
                column: "DocumentTrainingRequirementId",
                filter: "\"DocumentTrainingRequirementId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_assignment_training_requirement_DocumentTrainingRequirement~",
                schema: "training",
                table: "assignment",
                column: "DocumentTrainingRequirementId",
                principalSchema: "document",
                principalTable: "training_requirement",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assignment_training_requirement_DocumentTrainingRequirement~",
                schema: "training",
                table: "assignment");

            migrationBuilder.DropIndex(
                name: "IX_assignment_DocumentTrainingRequirementId",
                schema: "training",
                table: "assignment");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_DocumentTrainingRequirementId",
                schema: "training",
                table: "assignment",
                column: "DocumentTrainingRequirementId",
                unique: true,
                filter: "\"DocumentTrainingRequirementId\" IS NOT NULL");
        }
    }
}
