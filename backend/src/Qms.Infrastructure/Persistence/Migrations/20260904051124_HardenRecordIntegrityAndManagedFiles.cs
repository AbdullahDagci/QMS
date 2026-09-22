using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenRecordIntegrityAndManagedFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "files");

            migrationBuilder.AddColumn<string>(
                name: "IntegrityMac",
                schema: "core",
                table: "electronic_signature",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                schema: "audit",
                table: "audit_event",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IntegrityMac",
                schema: "audit",
                table: "audit_event",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreviousIntegrityHash",
                schema: "audit",
                table: "audit_event",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "managed_file",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IntegrityMac = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByDisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RetainUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_managed_file", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_managed_file_AggregateType_AggregateId_UploadedAtUtc",
                schema: "files",
                table: "managed_file",
                columns: new[] { "AggregateType", "AggregateId", "UploadedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_managed_file_ContentHash",
                schema: "files",
                table: "managed_file",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_managed_file_StoragePath",
                schema: "files",
                table: "managed_file",
                column: "StoragePath",
                unique: true);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION audit.prevent_sealed_record_mutation()
                RETURNS trigger AS $qms$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'sealed QMS records cannot be deleted';
                    END IF;
                    IF TG_TABLE_SCHEMA = 'audit'
                       AND OLD."IntegrityHash" = '' AND NEW."IntegrityHash" <> ''
                       AND (to_jsonb(NEW) - ARRAY['PreviousIntegrityHash','IntegrityHash','IntegrityMac'])
                           = (to_jsonb(OLD) - ARRAY['PreviousIntegrityHash','IntegrityHash','IntegrityMac']) THEN
                        RETURN NEW;
                    END IF;
                    IF TG_TABLE_SCHEMA = 'core'
                       AND OLD."IntegrityMac" = '' AND NEW."IntegrityMac" <> ''
                       AND (to_jsonb(NEW) - 'IntegrityMac') = (to_jsonb(OLD) - 'IntegrityMac') THEN
                        RETURN NEW;
                    END IF;
                    RAISE EXCEPTION 'sealed QMS records cannot be modified';
                END;
                $qms$ LANGUAGE plpgsql;

                CREATE TRIGGER audit_event_append_only
                    BEFORE UPDATE OR DELETE ON audit.audit_event
                    FOR EACH ROW EXECUTE FUNCTION audit.prevent_sealed_record_mutation();
                CREATE TRIGGER electronic_signature_append_only
                    BEFORE UPDATE OR DELETE ON core.electronic_signature
                    FOR EACH ROW EXECUTE FUNCTION audit.prevent_sealed_record_mutation();
                CREATE TRIGGER managed_file_append_only
                    BEFORE UPDATE OR DELETE ON files.managed_file
                    FOR EACH ROW EXECUTE FUNCTION audit.prevent_sealed_record_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS managed_file_append_only ON files.managed_file;
                DROP TRIGGER IF EXISTS electronic_signature_append_only ON core.electronic_signature;
                DROP TRIGGER IF EXISTS audit_event_append_only ON audit.audit_event;
                DROP FUNCTION IF EXISTS audit.prevent_sealed_record_mutation();
                """);
            migrationBuilder.DropTable(
                name: "managed_file",
                schema: "files");

            migrationBuilder.DropColumn(
                name: "IntegrityMac",
                schema: "core",
                table: "electronic_signature");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                schema: "audit",
                table: "audit_event");

            migrationBuilder.DropColumn(
                name: "IntegrityMac",
                schema: "audit",
                table: "audit_event");

            migrationBuilder.DropColumn(
                name: "PreviousIntegrityHash",
                schema: "audit",
                table: "audit_event");
        }
    }
}
