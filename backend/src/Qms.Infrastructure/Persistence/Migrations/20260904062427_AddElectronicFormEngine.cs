using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddElectronicFormEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "forms");

            migrationBuilder.CreateTable(
                name: "form_definition",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentPublishedVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    LatestVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_definition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_form_definition_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "form_version",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    EngineSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Schema = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    ChangeSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    WorkflowType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_version", x => x.Id);
                    table.ForeignKey(
                        name: "FK_form_version_form_definition_FormDefinitionId",
                        column: x => x.FormDefinitionId,
                        principalSchema: "forms",
                        principalTable: "form_definition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "output_template",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    TemplateType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Configuration = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_output_template", x => x.Id);
                    table.ForeignKey(
                        name: "FK_output_template_form_version_FormVersionId",
                        column: x => x.FormVersionId,
                        principalSchema: "forms",
                        principalTable: "form_version",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "form_record",
                schema: "forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormCodeSnapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FormNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FormVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_record", x => x.Id);
                    table.ForeignKey(
                        name: "FK_form_record_form_definition_FormDefinitionId",
                        column: x => x.FormDefinitionId,
                        principalSchema: "forms",
                        principalTable: "form_definition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_form_record_form_version_FormVersionId",
                        column: x => x.FormVersionId,
                        principalSchema: "forms",
                        principalTable: "form_version",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_form_record_output_template_OutputTemplateId",
                        column: x => x.OutputTemplateId,
                        principalSchema: "forms",
                        principalTable: "output_template",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_form_record_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_form_definition_Code",
                schema: "forms",
                table: "form_definition",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_definition_CurrentPublishedVersionId",
                schema: "forms",
                table: "form_definition",
                column: "CurrentPublishedVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_form_definition_IsActive_Category",
                schema: "forms",
                table: "form_definition",
                columns: new[] { "IsActive", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_form_definition_QualityRecordId",
                schema: "forms",
                table: "form_definition",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_record_CreatedAtUtc",
                schema: "forms",
                table: "form_record",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_form_record_FormDefinitionId_Status",
                schema: "forms",
                table: "form_record",
                columns: new[] { "FormDefinitionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_form_record_FormVersionId",
                schema: "forms",
                table: "form_record",
                column: "FormVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_form_record_OutputTemplateId",
                schema: "forms",
                table: "form_record",
                column: "OutputTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_form_record_QualityRecordId",
                schema: "forms",
                table: "form_record",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_version_FormDefinitionId_Status",
                schema: "forms",
                table: "form_version",
                columns: new[] { "FormDefinitionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_form_version_FormDefinitionId_VersionNumber",
                schema: "forms",
                table: "form_version",
                columns: new[] { "FormDefinitionId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_output_template_FormVersionId",
                schema: "forms",
                table: "output_template",
                column: "FormVersionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_form_definition_form_version_CurrentPublishedVersionId",
                schema: "forms",
                table: "form_definition",
                column: "CurrentPublishedVersionId",
                principalSchema: "forms",
                principalTable: "form_version",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE FUNCTION forms.protect_published_form_version() RETURNS trigger AS $$
                BEGIN
                    IF OLD."Status" = 'Published' THEN
                        RAISE EXCEPTION 'Published electronic form versions are immutable.' USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_protect_published_form_version
                BEFORE UPDATE OR DELETE ON forms.form_version
                FOR EACH ROW EXECUTE FUNCTION forms.protect_published_form_version();

                CREATE FUNCTION forms.protect_published_output_template() RETURNS trigger AS $$
                BEGIN
                    IF OLD."IsPublished" THEN
                        RAISE EXCEPTION 'Published electronic form output templates are immutable.' USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_protect_published_output_template
                BEFORE UPDATE OR DELETE ON forms.output_template
                FOR EACH ROW EXECUTE FUNCTION forms.protect_published_output_template();

                CREATE FUNCTION forms.protect_closed_form_record() RETURNS trigger AS $$
                BEGIN
                    IF OLD."Status" = 'Closed' THEN
                        RAISE EXCEPTION 'Closed electronic form records are immutable.' USING ERRCODE = '55000';
                    END IF;
                    IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_protect_closed_form_record
                BEFORE UPDATE OR DELETE ON forms.form_record
                FOR EACH ROW EXECUTE FUNCTION forms.protect_closed_form_record();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_form_definition_form_version_CurrentPublishedVersionId",
                schema: "forms",
                table: "form_definition");

            migrationBuilder.DropTable(
                name: "form_record",
                schema: "forms");

            migrationBuilder.DropTable(
                name: "output_template",
                schema: "forms");

            migrationBuilder.DropTable(
                name: "form_version",
                schema: "forms");

            migrationBuilder.DropTable(
                name: "form_definition",
                schema: "forms");

            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS forms.protect_published_form_version();
                DROP FUNCTION IF EXISTS forms.protect_published_output_template();
                DROP FUNCTION IF EXISTS forms.protect_closed_form_record();
                """);
        }
    }
}
