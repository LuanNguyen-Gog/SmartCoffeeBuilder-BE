using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class LinkChangeOrderToPaymentBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "change_order_id",
                table: "payment_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_batches_change_order_id",
                table: "payment_batches",
                column: "change_order_id");

            migrationBuilder.AddForeignKey(
                name: "fk_payment_batches_change_orders_change_order_id",
                table: "payment_batches",
                column: "change_order_id",
                principalTable: "change_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_batches_change_orders_change_order_id",
                table: "payment_batches");

            migrationBuilder.DropIndex(
                name: "ix_payment_batches_change_order_id",
                table: "payment_batches");

            migrationBuilder.DropColumn(
                name: "change_order_id",
                table: "payment_batches");
        }
    }
}
