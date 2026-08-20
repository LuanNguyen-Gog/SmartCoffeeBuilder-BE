using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteProfileChangeOrderPortfolioCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "extra_revision_fee",
                table: "quotations",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "revision_count",
                table: "designs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "actual_labor_cost",
                table: "construction_tasks",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "actual_start_at",
                table: "construction_tasks",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_labor_cost",
                table: "construction_tasks",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "start_at",
                table: "construction_tasks",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "actual_labor_cost",
                table: "construction_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "actual_start_at",
                table: "construction_items",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_labor_cost",
                table: "construction_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "start_at",
                table: "construction_items",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "change_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    design_id = table.Column<Guid>(type: "uuid", nullable: true),
                    construction_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    revision_no = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_by_party = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    responded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_change_orders_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_change_orders_accounts_responded_by",
                        column: x => x.responded_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_change_orders_construction_items_construction_item_id",
                        column: x => x.construction_item_id,
                        principalTable: "construction_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_change_orders_designs_design_id",
                        column: x => x.design_id,
                        principalTable: "designs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_change_orders_project_providers_project_provider_id",
                        column: x => x.project_provider_id,
                        principalTable: "project_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_portfolios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    style = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    area_m2 = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    contract_value = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    completed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    duration_days = table.Column<int>(type: "integer", nullable: true),
                    video_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cover_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider_portfolios", x => x.id);
                    table.ForeignKey(
                        name: "fk_provider_portfolios_service_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "service_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    length_m = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    width_m = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    frontage_width_m = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ceiling_height_m = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    road_width_m = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    orientation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    floor_count = table.Column<int>(type: "integer", nullable: true),
                    has_mezzanine = table.Column<bool>(type: "boolean", nullable: false),
                    structure_note = table.Column<string>(type: "text", nullable: true),
                    existing_condition_note = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_profiles_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_site_profiles_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_portfolio_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    provider_portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    caption = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider_portfolio_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_provider_portfolio_images_provider_portfolios_provider_port",
                        column: x => x.provider_portfolio_id,
                        principalTable: "provider_portfolios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_floors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    floor_no = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    area_m2 = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ceiling_height_m = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    purpose = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_floors", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_floors_site_profiles_site_profile_id",
                        column: x => x.site_profile_id,
                        principalTable: "site_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_openings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    site_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_floor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    orientation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    width_m = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    height_m = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_openings", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_openings_site_floors_site_floor_id",
                        column: x => x.site_floor_id,
                        principalTable: "site_floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_site_openings_site_profiles_site_profile_id",
                        column: x => x.site_profile_id,
                        principalTable: "site_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_change_orders_construction_item_id",
                table: "change_orders",
                column: "construction_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_change_orders_created_by",
                table: "change_orders",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_change_orders_design_id",
                table: "change_orders",
                column: "design_id");

            migrationBuilder.CreateIndex(
                name: "ix_change_orders_project_provider_id",
                table: "change_orders",
                column: "project_provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_change_orders_responded_by",
                table: "change_orders",
                column: "responded_by");

            migrationBuilder.CreateIndex(
                name: "ix_provider_portfolio_images_provider_portfolio_id",
                table: "provider_portfolio_images",
                column: "provider_portfolio_id");

            migrationBuilder.CreateIndex(
                name: "ix_provider_portfolios_provider_id",
                table: "provider_portfolios",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_site_floors_site_profile_id_floor_no",
                table: "site_floors",
                columns: new[] { "site_profile_id", "floor_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_site_openings_site_floor_id",
                table: "site_openings",
                column: "site_floor_id");

            migrationBuilder.CreateIndex(
                name: "ix_site_openings_site_profile_id",
                table: "site_openings",
                column: "site_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_site_profiles_created_by",
                table: "site_profiles",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_site_profiles_project_id",
                table: "site_profiles",
                column: "project_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "change_orders");

            migrationBuilder.DropTable(
                name: "provider_portfolio_images");

            migrationBuilder.DropTable(
                name: "site_openings");

            migrationBuilder.DropTable(
                name: "provider_portfolios");

            migrationBuilder.DropTable(
                name: "site_floors");

            migrationBuilder.DropTable(
                name: "site_profiles");

            migrationBuilder.DropColumn(
                name: "extra_revision_fee",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "revision_count",
                table: "designs");

            migrationBuilder.DropColumn(
                name: "actual_labor_cost",
                table: "construction_tasks");

            migrationBuilder.DropColumn(
                name: "actual_start_at",
                table: "construction_tasks");

            migrationBuilder.DropColumn(
                name: "estimated_labor_cost",
                table: "construction_tasks");

            migrationBuilder.DropColumn(
                name: "start_at",
                table: "construction_tasks");

            migrationBuilder.DropColumn(
                name: "actual_labor_cost",
                table: "construction_items");

            migrationBuilder.DropColumn(
                name: "actual_start_at",
                table: "construction_items");

            migrationBuilder.DropColumn(
                name: "estimated_labor_cost",
                table: "construction_items");

            migrationBuilder.DropColumn(
                name: "start_at",
                table: "construction_items");
        }
    }
}
