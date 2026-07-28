using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversations_project_providers_project_provider_id",
                table: "conversations");

            migrationBuilder.RenameColumn(
                name: "project_provider_id",
                table: "conversations",
                newName: "project_working_id");

            migrationBuilder.RenameIndex(
                name: "ix_conversations_project_provider_id",
                table: "conversations",
                newName: "ix_conversations_project_working_id");

            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "messages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<long>(
                name: "created_by",
                table: "conversations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "conversations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateTable(
                name: "message_attachments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message_id = table.Column<long>(type: "bigint", nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_attachments_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_created_by",
                table: "conversations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_message_attachments_message_id",
                table: "message_attachments",
                column: "message_id");

            migrationBuilder.AddForeignKey(
                name: "fk_conversations_accounts_created_by",
                table: "conversations",
                column: "created_by",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_conversations_project_workings_project_working_id",
                table: "conversations",
                column: "project_working_id",
                principalTable: "project_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversations_accounts_created_by",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "fk_conversations_project_workings_project_working_id",
                table: "conversations");

            migrationBuilder.DropTable(
                name: "message_attachments");

            migrationBuilder.DropIndex(
                name: "ix_conversations_created_by",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "conversations");

            migrationBuilder.RenameColumn(
                name: "project_working_id",
                table: "conversations",
                newName: "project_provider_id");

            migrationBuilder.RenameIndex(
                name: "ix_conversations_project_working_id",
                table: "conversations",
                newName: "ix_conversations_project_provider_id");

            migrationBuilder.AlterColumn<string>(
                name: "body",
                table: "messages",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_conversations_project_providers_project_provider_id",
                table: "conversations",
                column: "project_provider_id",
                principalTable: "project_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
