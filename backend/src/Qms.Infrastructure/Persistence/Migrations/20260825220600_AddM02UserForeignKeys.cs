using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM02UserForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_capa_user_EffectivenessEvaluatorUserId",
                schema: "capa",
                table: "capa",
                column: "EffectivenessEvaluatorUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_capa_user_OwnerUserId",
                schema: "capa",
                table: "capa",
                column: "OwnerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_capa_action_user_OwnerUserId",
                schema: "capa",
                table: "capa_action",
                column: "OwnerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_capa_user_EffectivenessEvaluatorUserId",
                schema: "capa",
                table: "capa");

            migrationBuilder.DropForeignKey(
                name: "FK_capa_user_OwnerUserId",
                schema: "capa",
                table: "capa");

            migrationBuilder.DropForeignKey(
                name: "FK_capa_action_user_OwnerUserId",
                schema: "capa",
                table: "capa_action");
        }
    }
}
