using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AlignWithLogicalERD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reviews_projects_project_id",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "fk_reviews_service_providers_provider_id",
                table: "reviews");

            migrationBuilder.DropIndex(
                name: "ix_reviews_project_id",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "estimated_budget",
                table: "project_posts");

            migrationBuilder.DropColumn(
                name: "bid_amount",
                table: "project_applications");

            migrationBuilder.DropColumn(
                name: "type",
                table: "docs");

            migrationBuilder.RenameColumn(
                name: "provider_id",
                table: "reviews",
                newName: "project_provider_id");

            migrationBuilder.RenameIndex(
                name: "ix_reviews_provider_id",
                table: "reviews",
                newName: "ix_reviews_project_provider_id");

            migrationBuilder.AddColumn<long>(
                name: "doc_type_id",
                table: "docs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "doc_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doc_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_docs_doc_type_id",
                table: "docs",
                column: "doc_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_doc_types_code",
                table: "doc_types",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_docs_doc_types_doc_type_id",
                table: "docs",
                column: "doc_type_id",
                principalTable: "doc_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_reviews_project_providers_project_provider_id",
                table: "reviews",
                column: "project_provider_id",
                principalTable: "project_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_docs_doc_types_doc_type_id",
                table: "docs");

            migrationBuilder.DropForeignKey(
                name: "fk_reviews_project_providers_project_provider_id",
                table: "reviews");

            migrationBuilder.DropTable(
                name: "doc_types");

            migrationBuilder.DropIndex(
                name: "ix_docs_doc_type_id",
                table: "docs");

            migrationBuilder.DropColumn(
                name: "doc_type_id",
                table: "docs");

            migrationBuilder.RenameColumn(
                name: "project_provider_id",
                table: "reviews",
                newName: "provider_id");

            migrationBuilder.RenameIndex(
                name: "ix_reviews_project_provider_id",
                table: "reviews",
                newName: "ix_reviews_provider_id");

            migrationBuilder.AddColumn<long>(
                name: "project_id",
                table: "reviews",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_budget",
                table: "project_posts",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "bid_amount",
                table: "project_applications",
                type: "numeric(15,2)",
                precision: 15,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "docs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_project_id",
                table: "reviews",
                column: "project_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reviews_projects_project_id",
                table: "reviews",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_reviews_service_providers_provider_id",
                table: "reviews",
                column: "provider_id",
                principalTable: "service_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
