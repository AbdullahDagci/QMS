using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddM13ToM16SpecializedRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.EnsureSchema(
                name: "specialized");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition9",
                schema: "work_tracking",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition8",
                schema: "training",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_lookup_definition7",
                schema: "supplier_audit",
                table: "lookup_definition",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "lookup_definition",
                schema: "specialized",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_definition6", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "record",
                schema: "specialized",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TypeCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TypeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubjectCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SubjectName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ScopeCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ScopeName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: false),
                    StructuredDataJson = table.Column<string>(type: "jsonb", nullable: false),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reviewer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Approver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_record", x => x.Id);
                    table.ForeignKey(
                        name: "FK_record_quality_record_QualityRecordId",
                        column: x => x.QualityRecordId,
                        principalSchema: "core",
                        principalTable: "quality_record",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_user_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_user_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_record_user_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalSchema: "identity",
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lookup_definition_ModuleCode_Category_Code",
                schema: "specialized",
                table: "lookup_definition",
                columns: new[] { "ModuleCode", "Category", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_record_ApproverUserId",
                schema: "specialized",
                table: "record",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_record_ModuleCode_Status_DueAtUtc",
                schema: "specialized",
                table: "record",
                columns: new[] { "ModuleCode", "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_record_OwnerUserId",
                schema: "specialized",
                table: "record",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_record_QualityRecordId",
                schema: "specialized",
                table: "record",
                column: "QualityRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_record_ReviewerUserId",
                schema: "specialized",
                table: "record",
                column: "ReviewerUserId");

            migrationBuilder.Sql("""
                INSERT INTO specialized.lookup_definition ("Id","ModuleCode","Category","Code","Name","SortOrder","IsActive","CreatedAtUtc","UpdatedAtUtc") VALUES
                (gen_random_uuid(),'M.13','Type','Carton','Karton ambalaj',10,true,now(),now()),(gen_random_uuid(),'M.13','Type','Label','Etiket',20,true,now(),now()),(gen_random_uuid(),'M.13','Type','Leaflet','Kullanma talimatı',30,true,now(),now()),
                (gen_random_uuid(),'M.13','Subject','PRD-PAR-500','Parasetamol 500 mg Tablet',10,true,now(),now()),(gen_random_uuid(),'M.13','Subject','PRD-IBU-200','İbuprofen 200 mg Tablet',20,true,now(),now()),
                (gen_random_uuid(),'M.13','Scope','TR-TR','Türkiye · Türkçe',10,true,now(),now()),(gen_random_uuid(),'M.13','Scope','EU-EN','Avrupa · İngilizce',20,true,now(),now()),
                (gen_random_uuid(),'M.14','Type','OOS','Spesifikasyon dışı sonuç',10,true,now(),now()),(gen_random_uuid(),'M.14','Type','OOT','Trend dışı sonuç',20,true,now(),now()),(gen_random_uuid(),'M.14','Type','Atypical','Atipik sonuç',30,true,now(),now()),
                (gen_random_uuid(),'M.14','Subject','ASSAY','Etken madde miktar tayini',10,true,now(),now()),(gen_random_uuid(),'M.14','Subject','DISSOLUTION','Çözünme testi',20,true,now(),now()),(gen_random_uuid(),'M.14','Subject','MICRO','Mikrobiyolojik analiz',30,true,now(),now()),
                (gen_random_uuid(),'M.14','Scope','QC-CHEM','Kimyasal kalite kontrol',10,true,now(),now()),(gen_random_uuid(),'M.14','Scope','QC-MICRO','Mikrobiyoloji laboratuvarı',20,true,now(),now()),
                (gen_random_uuid(),'M.15','Type','Initial','İlk bildirim',10,true,now(),now()),(gen_random_uuid(),'M.15','Type','FollowUp','Takip bildirimi',20,true,now(),now()),(gen_random_uuid(),'M.15','Type','Literature','Literatür vakası',30,true,now(),now()),
                (gen_random_uuid(),'M.15','Subject','PRD-PAR-500','Parasetamol 500 mg Tablet',10,true,now(),now()),(gen_random_uuid(),'M.15','Subject','PRD-IBU-200','İbuprofen 200 mg Tablet',20,true,now(),now()),
                (gen_random_uuid(),'M.15','Scope','Domestic','Yurt içi',10,true,now(),now()),(gen_random_uuid(),'M.15','Scope','Foreign','Yurt dışı',20,true,now(),now()),
                (gen_random_uuid(),'M.16','Type','Initial','İlk nitelendirme',10,true,now(),now()),(gen_random_uuid(),'M.16','Type','Periodic','Periyodik değerlendirme',20,true,now(),now()),(gen_random_uuid(),'M.16','Type','Requalification','Yeniden nitelendirme',30,true,now(),now()),
                (gen_random_uuid(),'M.16','Subject','API','Etken madde tedarikçisi',10,true,now(),now()),(gen_random_uuid(),'M.16','Subject','PACK','Ambalaj malzemesi tedarikçisi',20,true,now(),now()),(gen_random_uuid(),'M.16','Subject','SERVICE','Kritik hizmet sağlayıcı',30,true,now(),now()),
                (gen_random_uuid(),'M.16','Scope','Critical','Kritik tedarikçi',10,true,now(),now()),(gen_random_uuid(),'M.16','Scope','Standard','Standart tedarikçi',20,true,now(),now());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lookup_definition",
                schema: "specialized");

            migrationBuilder.DropTable(
                name: "record",
                schema: "specialized");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition9",
                schema: "work_tracking",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition8",
                schema: "training",
                table: "lookup_definition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_lookup_definition7",
                schema: "supplier_audit",
                table: "lookup_definition");

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
        }
    }
}
