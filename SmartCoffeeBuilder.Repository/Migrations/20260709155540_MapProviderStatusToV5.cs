using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <summary>
    /// v5: provider_status rút gọn còn 5 giá trị quan hệ (requested/accepted/completed/rejected/terminated).
    /// Data-fix: map các giá trị tiến độ cũ về 'accepted' — tiến độ giờ là derived từ
    /// contract confirmed + trạng thái design/construction_item, không lưu ở provider_status.
    /// Không đổi schema (enum lưu dạng text).
    /// </summary>
    public partial class MapProviderStatusToV5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE project_providers
                SET status = 'accepted'
                WHERE status IN ('designing', 'designed', 'constructing', 'constructed', 'in_progress');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không đảo ngược được — không còn thông tin engagement nào từng ở trạng thái tiến độ nào.
        }
    }
}
