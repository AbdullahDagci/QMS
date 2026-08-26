using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM01ActorSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvestigatorDepartmentSnapshot",
                schema: "deviation",
                table: "deviation_investigation",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InvestigatorNameSnapshot",
                schema: "deviation",
                table: "deviation_investigation",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssessedByDepartmentSnapshot",
                schema: "deviation",
                table: "deviation_batch_impact",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssessedByNameSnapshot",
                schema: "deviation",
                table: "deviation_batch_impact",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AssessedByUserId",
                schema: "deviation",
                table: "deviation_batch_impact",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                UPDATE deviation.deviation_investigation AS investigation
                SET "InvestigatorNameSnapshot" = COALESCE(actor."DisplayName", 'Geçmiş araştırmacı'),
                    "InvestigatorDepartmentSnapshot" = COALESCE(department."Name", 'Bölüm bilinmiyor')
                FROM identity."user" AS actor
                LEFT JOIN organization.department AS department ON department."Id" = actor."DepartmentId"
                WHERE actor."Id" = investigation."InvestigatorUserId";

                UPDATE deviation.deviation_batch_impact AS impact
                SET "AssessedByUserId" = COALESCE((
                        SELECT event."ActorUserId" FROM audit.audit_event AS event
                        WHERE event."AggregateType" = 'Deviation'
                          AND event."AggregateId" = impact."DeviationId"
                          AND event."EventType" = 'DeviationBatchImpactAssessed'
                        ORDER BY ABS(EXTRACT(EPOCH FROM (event."OccurredAtUtc" - impact."AssessedAtUtc")))
                        LIMIT 1), '00000000-0000-0000-0000-000000000000'::uuid),
                    "AssessedByNameSnapshot" = COALESCE((
                        SELECT event."ActorDisplayNameSnapshot" FROM audit.audit_event AS event
                        WHERE event."AggregateType" = 'Deviation'
                          AND event."AggregateId" = impact."DeviationId"
                          AND event."EventType" = 'DeviationBatchImpactAssessed'
                        ORDER BY ABS(EXTRACT(EPOCH FROM (event."OccurredAtUtc" - impact."AssessedAtUtc")))
                        LIMIT 1), 'Geçmiş değerlendirici'),
                    "AssessedByDepartmentSnapshot" = COALESCE((
                        SELECT department."Name"
                        FROM audit.audit_event AS event
                        JOIN identity."user" AS actor ON actor."Id" = event."ActorUserId"
                        LEFT JOIN organization.department AS department ON department."Id" = actor."DepartmentId"
                        WHERE event."AggregateType" = 'Deviation'
                          AND event."AggregateId" = impact."DeviationId"
                          AND event."EventType" = 'DeviationBatchImpactAssessed'
                        ORDER BY ABS(EXTRACT(EPOCH FROM (event."OccurredAtUtc" - impact."AssessedAtUtc")))
                        LIMIT 1), 'Bölüm bilinmiyor');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvestigatorDepartmentSnapshot",
                schema: "deviation",
                table: "deviation_investigation");

            migrationBuilder.DropColumn(
                name: "InvestigatorNameSnapshot",
                schema: "deviation",
                table: "deviation_investigation");

            migrationBuilder.DropColumn(
                name: "AssessedByDepartmentSnapshot",
                schema: "deviation",
                table: "deviation_batch_impact");

            migrationBuilder.DropColumn(
                name: "AssessedByNameSnapshot",
                schema: "deviation",
                table: "deviation_batch_impact");

            migrationBuilder.DropColumn(
                name: "AssessedByUserId",
                schema: "deviation",
                table: "deviation_batch_impact");
        }
    }
}
