using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue phải là "web", KHÔNG để "" như EF sinh mặc định: cột này map sang enum
            // PaymentPlatform, chuỗi rỗng không parse được nên mọi giao dịch cũ sẽ ném lỗi khi đọc lại.
            // Toàn bộ giao dịch có trước migration này đều được tạo từ web nên "web" là giá trị đúng.
            migrationBuilder.AddColumn<string>(
                name: "platform",
                table: "payment_transactions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "web");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "platform",
                table: "payment_transactions");
        }
    }
}
