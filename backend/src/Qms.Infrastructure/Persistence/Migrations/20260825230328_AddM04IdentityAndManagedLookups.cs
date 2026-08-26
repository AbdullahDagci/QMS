using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM04IdentityAndManagedLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PositionId",
                schema: "document",
                table: "training_requirement",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "document",
                table: "review",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerUserId",
                schema: "document",
                table: "review",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "document",
                table: "controlled_document",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "document",
                table: "controlled_document",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_definition", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_training_requirement_PositionId",
                schema: "document",
                table: "training_requirement",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_review_DepartmentId",
                schema: "document",
                table: "review",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_review_ReviewerUserId",
                schema: "document",
                table: "review",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_controlled_document_DepartmentId",
                schema: "document",
                table: "controlled_document",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_controlled_document_OwnerUserId",
                schema: "document",
                table: "controlled_document",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_Code",
                schema: "document",
                table: "lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_IsActive_SortOrder",
                schema: "document",
                table: "lookup_definition",
                columns: new[] { "Category", "IsActive", "SortOrder" });

            var seededAt = new DateTimeOffset(2026, 8, 25, 23, 3, 28, TimeSpan.Zero);
            migrationBuilder.InsertData(schema: "document", table: "lookup_definition", columns: new[] { "Id", "Category", "Code", "Name", "SortOrder", "IsActive", "CreatedAtUtc", "UpdatedAtUtc" }, values: new object[,]
            {
                { Guid.Parse("01a04b00-0001-7000-8000-000000000001"), "DocumentType", "SOP", "SOP", 10, true, seededAt, seededAt },
                { Guid.Parse("01a04b00-0001-7000-8000-000000000002"), "DocumentType", "Talimat", "Talimat", 20, true, seededAt, seededAt },
                { Guid.Parse("01a04b00-0001-7000-8000-000000000003"), "DocumentType", "Spesifikasyon", "Spesifikasyon", 30, true, seededAt, seededAt },
                { Guid.Parse("01a04b00-0001-7000-8000-000000000004"), "DocumentType", "Politika", "Politika", 40, true, seededAt, seededAt },
                { Guid.Parse("01a04b00-0001-7000-8000-000000000005"), "DocumentType", "Form / Şablon", "Form / Şablon", 50, true, seededAt, seededAt },
                { Guid.Parse("01a04b00-0001-7000-8000-000000000006"), "DocumentType", "Prosedür", "Prosedür", 60, true, seededAt, seededAt },
                { Guid.Parse("01a04b00-0002-7000-8000-000000000001"), "Confidentiality", "Internal", "Kurum İçi", 10, true, seededAt, seededAt },
                { Guid.Parse("01a04b00-0002-7000-8000-000000000002"), "Confidentiality", "Confidential", "Gizli", 20, true, seededAt, seededAt },
                { Guid.Parse("01a04b00-0002-7000-8000-000000000003"), "Confidentiality", "Public", "Halka Açık", 30, true, seededAt, seededAt }
            });

            migrationBuilder.Sql("""
                UPDATE document.controlled_document d SET "OwnerUserId" = u."Id" FROM identity."user" u WHERE d."Owner" = u."DisplayName" AND u."IsActive";
                UPDATE document.controlled_document d SET "DepartmentId" = dep."Id" FROM organization.department dep WHERE d."Department" = dep."Name" AND dep."IsActive";
                UPDATE document.review r SET "DepartmentId" = dep."Id" FROM organization.department dep WHERE r."Department" = dep."Name" AND dep."IsActive";
                UPDATE document.review r SET "ReviewerUserId" = u."Id" FROM identity."user" u WHERE r."Reviewer" = u."DisplayName" AND u."IsActive";
                UPDATE document.training_requirement t SET "PositionId" = p."Id" FROM organization.position p WHERE t."Position" = p."Name" AND p."IsActive";
                UPDATE document.controlled_document SET "Confidentiality" = CASE "Confidentiality" WHEN 'Kurum İçi' THEN 'Internal' WHEN 'Gizli' THEN 'Confidential' WHEN 'Halka Açık' THEN 'Public' ELSE "Confidentiality" END;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_controlled_document_department_DepartmentId",
                schema: "document",
                table: "controlled_document",
                column: "DepartmentId",
                principalSchema: "organization",
                principalTable: "department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_controlled_document_user_OwnerUserId",
                schema: "document",
                table: "controlled_document",
                column: "OwnerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_review_department_DepartmentId",
                schema: "document",
                table: "review",
                column: "DepartmentId",
                principalSchema: "organization",
                principalTable: "department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_review_user_ReviewerUserId",
                schema: "document",
                table: "review",
                column: "ReviewerUserId",
                principalSchema: "identity",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_training_requirement_position_PositionId",
                schema: "document",
                table: "training_requirement",
                column: "PositionId",
                principalSchema: "organization",
                principalTable: "position",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_controlled_document_department_DepartmentId",
                schema: "document",
                table: "controlled_document");

            migrationBuilder.DropForeignKey(
                name: "FK_controlled_document_user_OwnerUserId",
                schema: "document",
                table: "controlled_document");

            migrationBuilder.DropForeignKey(
                name: "FK_review_department_DepartmentId",
                schema: "document",
                table: "review");

            migrationBuilder.DropForeignKey(
                name: "FK_review_user_ReviewerUserId",
                schema: "document",
                table: "review");

            migrationBuilder.DropForeignKey(
                name: "FK_training_requirement_position_PositionId",
                schema: "document",
                table: "training_requirement");

            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "document");

            migrationBuilder.DropIndex(
                name: "IX_training_requirement_PositionId",
                schema: "document",
                table: "training_requirement");

            migrationBuilder.DropIndex(
                name: "IX_review_DepartmentId",
                schema: "document",
                table: "review");

            migrationBuilder.DropIndex(
                name: "IX_review_ReviewerUserId",
                schema: "document",
                table: "review");

            migrationBuilder.DropIndex(
                name: "IX_controlled_document_DepartmentId",
                schema: "document",
                table: "controlled_document");

            migrationBuilder.DropIndex(
                name: "IX_controlled_document_OwnerUserId",
                schema: "document",
                table: "controlled_document");

            migrationBuilder.DropColumn(
                name: "PositionId",
                schema: "document",
                table: "training_requirement");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "document",
                table: "review");

            migrationBuilder.DropColumn(
                name: "ReviewerUserId",
                schema: "document",
                table: "review");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "document",
                table: "controlled_document");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "document",
                table: "controlled_document");
        }
    }
}
