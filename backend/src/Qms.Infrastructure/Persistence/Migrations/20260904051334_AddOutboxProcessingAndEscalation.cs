using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxProcessingAndEscalation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EscalationLevel",
                schema: "workflow",
                table: "task_assignment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastEscalatedAtUtc",
                schema: "workflow",
                table: "task_assignment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockId",
                schema: "integration",
                table: "outbox_message",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntilUtc",
                schema: "integration",
                table: "outbox_message",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_assignment_Status_DueAtUtc_LastEscalatedAtUtc",
                schema: "workflow",
                table: "task_assignment",
                columns: new[] { "Status", "DueAtUtc", "LastEscalatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_message_Status_LockedUntilUtc",
                schema: "integration",
                table: "outbox_message",
                columns: new[] { "Status", "LockedUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_task_assignment_Status_DueAtUtc_LastEscalatedAtUtc",
                schema: "workflow",
                table: "task_assignment");

            migrationBuilder.DropIndex(
                name: "IX_outbox_message_Status_LockedUntilUtc",
                schema: "integration",
                table: "outbox_message");

            migrationBuilder.DropColumn(
                name: "EscalationLevel",
                schema: "workflow",
                table: "task_assignment");

            migrationBuilder.DropColumn(
                name: "LastEscalatedAtUtc",
                schema: "workflow",
                table: "task_assignment");

            migrationBuilder.DropColumn(
                name: "LockId",
                schema: "integration",
                table: "outbox_message");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                schema: "integration",
                table: "outbox_message");
        }
    }
}
