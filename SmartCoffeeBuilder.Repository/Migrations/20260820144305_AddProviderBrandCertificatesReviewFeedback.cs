using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderBrandCertificatesReviewFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "brand_story",
                table: "service_providers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_address",
                table: "service_providers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_image_url",
                table: "service_providers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "employee_count",
                table: "service_providers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "founded_year",
                table: "service_providers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "intro_video_url",
                table: "service_providers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "service_providers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "review_count",
                table: "service_providers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "website",
                table: "service_providers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_reply",
                table: "reviews",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "replied_at",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replied_by",
                table: "reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "provider_certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    issuer = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    certificate_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    issued_at = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    file_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider_certificates", x => x.id);
                    table.ForeignKey(
                        name: "fk_provider_certificates_service_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "service_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_service_areas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider_service_areas", x => x.id);
                    table.ForeignKey(
                        name: "fk_provider_service_areas_service_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "service_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_social_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider_social_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_provider_social_links_service_providers_provider_id",
                        column: x => x.provider_id,
                        principalTable: "service_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    caption = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_images_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_replied_by",
                table: "reviews",
                column: "replied_by");

            migrationBuilder.CreateIndex(
                name: "ix_provider_certificates_provider_id",
                table: "provider_certificates",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_provider_service_areas_provider_id_province_district",
                table: "provider_service_areas",
                columns: new[] { "provider_id", "province", "district" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_provider_service_areas_province",
                table: "provider_service_areas",
                column: "province");

            migrationBuilder.CreateIndex(
                name: "ix_provider_social_links_provider_id_platform",
                table: "provider_social_links",
                columns: new[] { "provider_id", "platform" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_images_review_id",
                table: "review_images",
                column: "review_id");

            migrationBuilder.AddForeignKey(
                name: "fk_reviews_accounts_replied_by",
                table: "reviews",
                column: "replied_by",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reviews_accounts_replied_by",
                table: "reviews");

            migrationBuilder.DropTable(
                name: "provider_certificates");

            migrationBuilder.DropTable(
                name: "provider_service_areas");

            migrationBuilder.DropTable(
                name: "provider_social_links");

            migrationBuilder.DropTable(
                name: "review_images");

            migrationBuilder.DropIndex(
                name: "ix_reviews_replied_by",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "brand_story",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "company_address",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "cover_image_url",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "employee_count",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "founded_year",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "intro_video_url",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "review_count",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "website",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "provider_reply",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "replied_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "replied_by",
                table: "reviews");
        }
    }
}
