using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentsAndDesignVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ───────── comments: thread public neo vào ConstructionItem / Design (FK mềm) ─────────
            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    target_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_id = table.Column<long>(type: "bigint", nullable: false),
                    body = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_comments_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comments_created_by",
                table: "comments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_comments_target_type_target_id",
                table: "comments",
                columns: new[] { "target_type", "target_id" });

            // ───────── design_versions: snapshot Design khi submit / approve ─────────
            migrationBuilder.CreateTable(
                name: "design_versions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    design_id = table.Column<long>(type: "bigint", nullable: false),
                    snapshot_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    snapshotted_by = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    snapshotted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_design_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_design_versions_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_design_versions_accounts_snapshotted_by",
                        column: x => x.snapshotted_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_design_versions_designs_design_id",
                        column: x => x.design_id,
                        principalTable: "designs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_design_versions_design_id",
                table: "design_versions",
                column: "design_id");

            migrationBuilder.CreateIndex(
                name: "ix_design_versions_design_id_snapshot_kind",
                table: "design_versions",
                columns: new[] { "design_id", "snapshot_kind" });

            // ───────── design_version_images: copy nguyên trạng DesignImage lúc snapshot ─────────
            migrationBuilder.CreateTable(
                name: "design_version_images",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    design_version_id = table.Column<long>(type: "bigint", nullable: false),
                    original_image_id = table.Column<long>(type: "bigint", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: false),
                    caption = table.Column<string>(type: "text", nullable: true),
                    uploaded_by = table.Column<long>(type: "bigint", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_design_version_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_design_version_images_accounts_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_design_version_images_design_images_original_image_id",
                        column: x => x.original_image_id,
                        principalTable: "design_images",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_design_version_images_design_versions_design_version_id",
                        column: x => x.design_version_id,
                        principalTable: "design_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_design_version_images_design_version_id",
                table: "design_version_images",
                column: "design_version_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "design_version_images");

            migrationBuilder.DropTable(
                name: "design_versions");

            migrationBuilder.DropTable(
                name: "comments");
        }
    }
}