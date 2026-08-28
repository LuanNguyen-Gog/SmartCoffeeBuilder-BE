using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "company_latitude",
                table: "service_providers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "company_longitude",
                table: "service_providers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "projects",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "projects",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company_latitude",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "company_longitude",
                table: "service_providers");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "projects");
        }
    }
}
