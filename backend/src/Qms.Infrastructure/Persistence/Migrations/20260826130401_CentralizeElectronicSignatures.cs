using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CentralizeElectronicSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AggregateId",
                schema: "core",
                table: "electronic_signature",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "AggregateType",
                schema: "core",
                table: "electronic_signature",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<string>(
                name: "Operation",
                schema: "core",
                table: "electronic_signature",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.AddColumn<string>(
                name: "ProviderType",
                schema: "core",
                table: "electronic_signature",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<string>(
                name: "SignatureMethod",
                schema: "core",
                table: "electronic_signature",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "LegacyApplicationSignature");

            migrationBuilder.AddColumn<JsonDocument>(
                name: "SignedSnapshot",
                schema: "core",
                table: "electronic_signature",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateIndex(
                name: "IX_electronic_signature_AggregateType_AggregateId",
                schema: "core",
                table: "electronic_signature",
                columns: new[] { "AggregateType", "AggregateId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_electronic_signature_AggregateType_AggregateId",
                schema: "core",
                table: "electronic_signature");

            migrationBuilder.DropColumn(
                name: "AggregateId",
                schema: "core",
                table: "electronic_signature");

            migrationBuilder.DropColumn(
                name: "AggregateType",
                schema: "core",
                table: "electronic_signature");

            migrationBuilder.DropColumn(
                name: "Operation",
                schema: "core",
                table: "electronic_signature");

            migrationBuilder.DropColumn(
                name: "ProviderType",
                schema: "core",
                table: "electronic_signature");

            migrationBuilder.DropColumn(
                name: "SignatureMethod",
                schema: "core",
                table: "electronic_signature");

            migrationBuilder.DropColumn(
                name: "SignedSnapshot",
                schema: "core",
                table: "electronic_signature");
        }
    }
}
