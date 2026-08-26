using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM12MasterBatchRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition7",
                schema: "work_tracking",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition6",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "supplier_audit",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition4",
                schema: "risk_management",
                table: "lookup_definition");

            migrationBuilder.EnsureSchema(
                name: "mbr");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code7",
                schema: "work_tracking",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code8");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code6",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code7");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code5",
                schema: "supplier_audit",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code6");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code4",
                schema: "risk_management",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code5");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition8",
                schema: "work_tracking",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition7",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition6",
                schema: "supplier_audit",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "risk_management",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "mbr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_definition4", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "master_batch_record",
                schema: "mbr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    DosageFormCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DosageFormName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Strength = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    BatchSize = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BatchUnitCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    BatchUnitName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    SiteCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SiteName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    LineCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    LineName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    DocumentVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reviewer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Approver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_batch_record", x => x.Id);
                    table.ForeignKey(
                        name: "FK_master_batch_record_master_batch_record_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalSchema: "mbr",
                        principalTable: "master_batch_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_master_batch_record_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_master_batch_record_user_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_master_batch_record_user_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_master_batch_record_user_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "step",
                schema: "mbr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterBatchRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    PhaseCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PhaseName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Instruction = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    MaterialOrEquipmentReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false),
                    Parameter = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    LowerLimit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    UpperLimit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    UnitCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UnitName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step", x => x.Id);
                    table.ForeignKey(
                        name: "FK_step_master_batch_record_MasterBatchRecordId",
                        column: x => x.MasterBatchRecordId,
                        principalSchema: "mbr",
                        principalTable: "master_batch_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_Category_Code4",
                schema: "mbr",
                table: "lookup_definition",
                columns: new[] { "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_master_batch_record_ApproverUserId",
                schema: "mbr",
                table: "master_batch_record",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_master_batch_record_AuthorUserId",
                schema: "mbr",
                table: "master_batch_record",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_master_batch_record_PreviousVersionId",
                schema: "mbr",
                table: "master_batch_record",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_master_batch_record_ProductCode_SiteCode_Status",
                schema: "mbr",
                table: "master_batch_record",
                columns: new[] { "ProductCode", "SiteCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_master_batch_record_QualityRecordId",
                schema: "mbr",
                table: "master_batch_record",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_master_batch_record_ReviewerUserId",
                schema: "mbr",
                table: "master_batch_record",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_step_MasterBatchRecordId_Order",
                schema: "mbr",
                table: "step",
                columns: new[] { "MasterBatchRecordId", "Order" },
                unique: true);
            migrationBuilder.Sql("""
                INSERT INTO mbr.lookup_definition ("Id","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc") VALUES
                (gen_random_uuid(),'Product','PRD-PAR-500','Parasetamol 500 mg Tablet',10,true,NOW(),NOW()),
                (gen_random_uuid(),'Product','PRD-IBU-200','İbuprofen 200 mg Tablet',20,true,NOW(),NOW()),
                (gen_random_uuid(),'DosageForm','Tablet','Tablet',10,true,NOW(),NOW()),
                (gen_random_uuid(),'DosageForm','Capsule','Kapsül',20,true,NOW(),NOW()),
                (gen_random_uuid(),'BatchUnit','KG','kg',10,true,NOW(),NOW()),
                (gen_random_uuid(),'BatchUnit','UNIT','adet',20,true,NOW(),NOW()),
                (gen_random_uuid(),'Site','IST-01','İstanbul Üretim Tesisi',10,true,NOW(),NOW()),
                (gen_random_uuid(),'Line','TAB-01','Tablet Hattı 1',10,true,NOW(),NOW()),
                (gen_random_uuid(),'Line','TAB-02','Tablet Hattı 2',20,true,NOW(),NOW()),
                (gen_random_uuid(),'Phase','Dispensing','Tartım',10,true,NOW(),NOW()),
                (gen_random_uuid(),'Phase','Granulation','Granülasyon',20,true,NOW(),NOW()),
                (gen_random_uuid(),'Phase','Compression','Tablet baskı',30,true,NOW(),NOW()),
                (gen_random_uuid(),'Phase','Coating','Kaplama',40,true,NOW(),NOW()),
                (gen_random_uuid(),'Phase','Packaging','Ambalajlama',50,true,NOW(),NOW()),
                (gen_random_uuid(),'ParameterUnit','C','°C',10,true,NOW(),NOW()),
                (gen_random_uuid(),'ParameterUnit','MIN','dakika',20,true,NOW(),NOW()),
                (gen_random_uuid(),'ParameterUnit','RPM','rpm',30,true,NOW(),NOW()),
                (gen_random_uuid(),'ParameterUnit','KG','kg',40,true,NOW(),NOW());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "mbr");

            migrationBuilder.DropTable(
                name: "step",
                schema: "mbr");

            migrationBuilder.DropTable(
                name: "master_batch_record",
                schema: "mbr");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition8",
                schema: "work_tracking",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition7",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition6",
                schema: "supplier_audit",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "risk_management",
                table: "lookup_definition");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code8",
                schema: "work_tracking",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code7");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code7",
                schema: "training",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code6");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code6",
                schema: "supplier_audit",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code5");

            migrationBuilder.RenameIndex(
                name: "IX_lookup_definition_Category_Code5",
                schema: "risk_management",
                table: "lookup_definition",
                newName: "IX_lookup_definition_Category_Code4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition7",
                schema: "work_tracking",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition6",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition5",
                schema: "supplier_audit",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition4",
                schema: "risk_management",
                table: "lookup_definition",
                column: "Id");
        }
    }
}
