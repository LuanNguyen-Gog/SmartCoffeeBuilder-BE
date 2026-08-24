using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyLogQuotationCommentContractPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "execution_end_at",
                table: "contracts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "execution_start_at",
                table: "contracts",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "daily_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    construction_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    construction_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    log_date = table.Column<DateOnly>(type: "date", nullable: false),
                    work_done = table.Column<string>(type: "text", nullable: false),
                    issue_note = table.Column<string>(type: "text", nullable: true),
                    weather_note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    worker_count = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_daily_logs_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_daily_logs_construction_items_construction_item_id",
                        column: x => x.construction_item_id,
                        principalTable: "construction_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_daily_logs_construction_tasks_construction_task_id",
                        column: x => x.construction_task_id,
                        principalTable: "construction_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_daily_logs_project_providers_project_provider_id",
                        column: x => x.project_provider_id,
                        principalTable: "project_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_log_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    daily_log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    media_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    caption = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_log_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_daily_log_media_daily_logs_daily_log_id",
                        column: x => x.daily_log_id,
                        principalTable: "daily_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_daily_log_media_daily_log_id",
                table: "daily_log_media",
                column: "daily_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_daily_logs_construction_item_id",
                table: "daily_logs",
                column: "construction_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_daily_logs_construction_task_id",
                table: "daily_logs",
                column: "construction_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_daily_logs_created_by",
                table: "daily_logs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_daily_logs_project_provider_id_log_date",
                table: "daily_logs",
                columns: new[] { "project_provider_id", "log_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_log_media");

            migrationBuilder.DropTable(
                name: "daily_logs");

            migrationBuilder.DropColumn(
                name: "execution_end_at",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "execution_start_at",
                table: "contracts");
        }
    }
}
