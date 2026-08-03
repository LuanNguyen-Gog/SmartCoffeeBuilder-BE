using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddEngagementCompletionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "completion_request_note",
                table: "project_providers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "completion_requested_at",
                table: "project_providers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completion_request_note",
                table: "project_providers");

            migrationBuilder.DropColumn(
                name: "completion_requested_at",
                table: "project_providers");
        }
    }
}
