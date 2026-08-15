using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <summary>
    /// Bỏ cột <c>surveys.version</c>. Số hiệu phiên bản của survey được sinh tự động (max + 0.1)
    /// nhưng KHÔNG có nghiệp vụ nào đọc tới: survey chỉ là bản ghi khảo sát, xếp theo
    /// <c>created_at</c> là đủ. (Khác <c>designs.version</c> / <c>design_versions</c> — hai chỗ đó
    /// vẫn giữ vì luồng submit/approve dựa vào phiên bản.)
    ///
    /// CẢNH BÁO: đây là thao tác MẤT DỮ LIỆU — giá trị version cũ không khôi phục được sau khi
    /// migration chạy (Down chỉ tạo lại cột rỗng với default 0).
    /// </summary>
    public partial class RemoveSurveyVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "surveys");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "version",
                table: "surveys",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
