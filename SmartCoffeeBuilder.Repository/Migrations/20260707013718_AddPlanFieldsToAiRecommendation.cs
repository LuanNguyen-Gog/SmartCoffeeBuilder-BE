using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanFieldsToAiRecommendation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "plan",
                table: "ai_recommendations",
                newName: "risk_notes");

            migrationBuilder.AddColumn<decimal>(
                name: "contingency_percent",
                table: "ai_recommendations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cost_notes",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_flow",
                table: "ai_recommendations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "equipment_max_vnd",
                table: "ai_recommendations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "equipment_min_vnd",
                table: "ai_recommendations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "fitout_max_vnd",
                table: "ai_recommendations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "fitout_min_vnd",
                table: "ai_recommendations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_artifact_url",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_aspect_ratio",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_negative_prompt",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_prompt",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_reference_urls",
                table: "ai_recommendations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_view",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "layout_adjacency_rules",
                table: "ai_recommendations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "layout_height",
                table: "ai_recommendations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "layout_unit",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "layout_width",
                table: "ai_recommendations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "layout_zones",
                table: "ai_recommendations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan_concept_name",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan_json",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan_summary",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recommendations",
                table: "ai_recommendations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "seat_capacity_recommendation",
                table: "ai_recommendations",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contingency_percent",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "cost_notes",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "customer_flow",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "equipment_max_vnd",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "equipment_min_vnd",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "fitout_max_vnd",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "fitout_min_vnd",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "image_artifact_url",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "image_aspect_ratio",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "image_negative_prompt",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "image_prompt",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "image_reference_urls",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "image_view",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "layout_adjacency_rules",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "layout_height",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "layout_unit",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "layout_width",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "layout_zones",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "plan_concept_name",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "plan_json",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "plan_summary",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "recommendations",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "seat_capacity_recommendation",
                table: "ai_recommendations");

            migrationBuilder.RenameColumn(
                name: "risk_notes",
                table: "ai_recommendations",
                newName: "plan");
        }
    }
}
