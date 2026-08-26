using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM02UserAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "capa",
                table: "capa_action",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EffectivenessEvaluatorUserId",
                schema: "capa",
                table: "capa",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "capa",
                table: "capa",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_capa_action_OwnerUserId",
                schema: "capa",
                table: "capa_action",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_capa_EffectivenessEvaluatorUserId",
                schema: "capa",
                table: "capa",
                column: "EffectivenessEvaluatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_capa_OwnerUserId",
                schema: "capa",
                table: "capa",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_capa_action_OwnerUserId",
                schema: "capa",
                table: "capa_action");

            migrationBuilder.DropIndex(
                name: "IX_capa_EffectivenessEvaluatorUserId",
                schema: "capa",
                table: "capa");

            migrationBuilder.DropIndex(
                name: "IX_capa_OwnerUserId",
                schema: "capa",
                table: "capa");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "capa",
                table: "capa_action");

            migrationBuilder.DropColumn(
                name: "EffectivenessEvaluatorUserId",
                schema: "capa",
                table: "capa");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "capa",
                table: "capa");
        }
    }
}
