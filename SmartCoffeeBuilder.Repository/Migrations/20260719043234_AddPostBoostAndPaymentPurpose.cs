using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPostBoostAndPaymentPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "boosted_until",
                table: "project_posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "subscription_id",
                table: "payment_transactions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "boost_days",
                table: "payment_transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "post_id",
                table: "payment_transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purpose",
                table: "payment_transactions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                // Giao dịch có trước migration này đều là mua subscription.
                defaultValue: "subscription");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_post_id",
                table: "payment_transactions",
                column: "post_id");

            migrationBuilder.AddForeignKey(
                name: "fk_payment_transactions_posts_post_id",
                table: "payment_transactions",
                column: "post_id",
                principalTable: "project_posts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_transactions_posts_post_id",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_post_id",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "boosted_until",
                table: "project_posts");

            migrationBuilder.DropColumn(
                name: "boost_days",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "post_id",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "payment_transactions");

            migrationBuilder.AlterColumn<long>(
                name: "subscription_id",
                table: "payment_transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
