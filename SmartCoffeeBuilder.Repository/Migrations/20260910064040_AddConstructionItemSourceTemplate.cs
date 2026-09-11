using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddConstructionItemSourceTemplate : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Cột chỉ ghi VẾT NGUỒN cho hạng mục sinh từ mẫu kể từ lúc migration chạy. KHÔNG backfill
        /// được cho dữ liệu cũ: áp mẫu vốn là copy thuần, hạng mục đã sinh không giữ lại dấu vết
        /// nào để dò ngược — so tên với mẫu là đoán, mà đoán sai thì báo cho chủ quán một quy
        /// trình nhà thầu chưa từng dùng. Dự án cũ vì thế hiện "chưa áp mẫu nào", đúng với những
        /// gì hệ thống thực sự biết; áp lại mẫu là có ngay.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_template_id",
                table: "construction_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_construction_items_source_template_id",
                table: "construction_items",
                column: "source_template_id");

            migrationBuilder.AddForeignKey(
                name: "fk_construction_items_construction_templates_source_template_id",
                table: "construction_items",
                column: "source_template_id",
                principalTable: "construction_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_construction_items_construction_templates_source_template_id",
                table: "construction_items");

            migrationBuilder.DropIndex(
                name: "ix_construction_items_source_template_id",
                table: "construction_items");

            migrationBuilder.DropColumn(
                name: "source_template_id",
                table: "construction_items");
        }
    }
}
