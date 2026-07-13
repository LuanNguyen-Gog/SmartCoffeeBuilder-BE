using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddConstructionTaskAndDropIsDone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // v5 (Mục 8): trước khi drop is_done, giữ lại nghĩa hoàn thành —
            // dòng nào is_done = true mà status chưa 'completed' thì set 'completed'.
            migrationBuilder.Sql(
                "UPDATE construction_items SET status = 'completed' WHERE is_done = true AND status <> 'completed';");

            migrationBuilder.DropColumn(
                name: "is_done",
                table: "construction_items");

            migrationBuilder.CreateTable(
                name: "construction_tasks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    construction_item_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    estimate_at = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_at = table.Column<DateOnly>(type: "date", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_construction_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_construction_tasks_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_construction_tasks_construction_items_construction_item_id",
                        column: x => x.construction_item_id,
                        principalTable: "construction_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_construction_tasks_construction_item_id",
                table: "construction_tasks",
                column: "construction_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_construction_tasks_created_by",
                table: "construction_tasks",
                column: "created_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "construction_tasks");

            migrationBuilder.AddColumn<bool>(
                name: "is_done",
                table: "construction_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
