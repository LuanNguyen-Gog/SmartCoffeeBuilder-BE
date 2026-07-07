using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddAiDesignJobFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempts",
                table: "ai_recommendations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "ai_recommendations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "job_id",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_error",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parent_job_id",
                table: "ai_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan",
                table: "ai_recommendations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                table: "ai_recommendations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "ai_recommendations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attempts",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "job_id",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "last_error",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "parent_job_id",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "plan",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "ai_recommendations");

            migrationBuilder.DropColumn(
                name: "state",
                table: "ai_recommendations");
        }
    }
}
