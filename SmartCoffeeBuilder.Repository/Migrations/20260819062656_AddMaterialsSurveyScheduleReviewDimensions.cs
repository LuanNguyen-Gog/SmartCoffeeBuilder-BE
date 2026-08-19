using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialsSurveyScheduleReviewDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "project_provider_id",
                table: "surveys",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "apply_id",
                table: "surveys",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "scheduled_at",
                table: "surveys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "surveyed_at",
                table: "surveys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "dimension",
                table: "review_scores",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_materials_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_materials_project_providers_project_provider_id",
                        column: x => x.project_provider_id,
                        principalTable: "project_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "construction_materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    construction_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    construction_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estimated_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    actual_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_construction_materials", x => x.id);
                    table.CheckConstraint("ck_construction_materials_target", "(construction_item_id IS NOT NULL AND construction_task_id IS NULL) OR (construction_item_id IS NULL AND construction_task_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_construction_materials_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_construction_materials_construction_items_construction_item",
                        column: x => x.construction_item_id,
                        principalTable: "construction_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_construction_materials_construction_tasks_construction_task",
                        column: x => x.construction_task_id,
                        principalTable: "construction_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_construction_materials_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_surveys_apply_id",
                table: "surveys",
                column: "apply_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_surveys_target",
                table: "surveys",
                sql: "(project_provider_id IS NOT NULL AND apply_id IS NULL) OR (project_provider_id IS NULL AND apply_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_review_scores_review_id_dimension",
                table: "review_scores",
                columns: new[] { "review_id", "dimension" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_construction_materials_construction_item_id",
                table: "construction_materials",
                column: "construction_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_construction_materials_construction_task_id",
                table: "construction_materials",
                column: "construction_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_construction_materials_created_by",
                table: "construction_materials",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_construction_materials_material_id",
                table: "construction_materials",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_materials_created_by",
                table: "materials",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_materials_project_provider_id",
                table: "materials",
                column: "project_provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_materials_project_provider_id_name",
                table: "materials",
                columns: new[] { "project_provider_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_surveys_applies_apply_id",
                table: "surveys",
                column: "apply_id",
                principalTable: "project_applications",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_surveys_applies_apply_id",
                table: "surveys");

            migrationBuilder.DropTable(
                name: "construction_materials");

            migrationBuilder.DropTable(
                name: "materials");

            migrationBuilder.DropIndex(
                name: "ix_surveys_apply_id",
                table: "surveys");

            migrationBuilder.DropCheckConstraint(
                name: "ck_surveys_target",
                table: "surveys");

            migrationBuilder.DropIndex(
                name: "ix_review_scores_review_id_dimension",
                table: "review_scores");

            migrationBuilder.DropColumn(
                name: "apply_id",
                table: "surveys");

            migrationBuilder.DropColumn(
                name: "scheduled_at",
                table: "surveys");

            migrationBuilder.DropColumn(
                name: "surveyed_at",
                table: "surveys");

            migrationBuilder.AlterColumn<Guid>(
                name: "project_provider_id",
                table: "surveys",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "dimension",
                table: "review_scores",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}
