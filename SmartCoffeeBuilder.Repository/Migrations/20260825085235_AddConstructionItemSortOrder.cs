using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddConstructionItemSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "construction_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Nạp thứ tự ĐANG HIỂN THỊ vào cột mới, thay vì để mọi hàng cùng bằng 0.
            //
            // Danh sách hạng mục giờ sắp theo sort_order TRƯỚC. Bỏ qua bước này thì mọi hàng cũ
            // hoà nhau ở 0, khoá sắp xếp rơi hết về mốc phụ, và các hạng mục sinh từ mẫu — vốn
            // dùng chung created_at tới từng mili-giây — lại quay về thứ tự tuỳ ý của Postgres.
            // Đó đúng là lỗi mà phần sắp xếp theo estimate_at trước đây đã phải sửa.
            //
            // ORDER BY dưới đây lặp lại đúng thứ tự cũ (estimate_at NULLS LAST → created_at → id),
            // nên ngay sau khi chạy migration màn kế hoạch trông y hệt lúc trước; chỉ từ lần kéo
            // thả đầu tiên thứ tự mới đổi.
            //
            // PARTITION theo (project_provider_id, parent_id): thứ tự chỉ có nghĩa TRONG một nhóm
            // anh em. Postgres gộp các parent_id NULL vào cùng một phân vùng, tức toàn bộ milestone
            // gốc của một engagement được đánh số 1..n như mong muốn.
            migrationBuilder.Sql(@"
                WITH ordered AS (
                    SELECT id,
                           ROW_NUMBER() OVER (
                               PARTITION BY project_provider_id, parent_id
                               ORDER BY estimate_at ASC NULLS LAST, created_at ASC, id ASC
                           ) AS rn
                    FROM construction_items
                )
                UPDATE construction_items AS ci
                SET sort_order = ordered.rn
                FROM ordered
                WHERE ci.id = ordered.id;
            ");

            migrationBuilder.CreateIndex(
                name: "ix_construction_items_project_provider_id_parent_id_sort_order",
                table: "construction_items",
                columns: new[] { "project_provider_id", "parent_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_construction_items_project_provider_id_parent_id_sort_order",
                table: "construction_items");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "construction_items");
        }
    }
}
