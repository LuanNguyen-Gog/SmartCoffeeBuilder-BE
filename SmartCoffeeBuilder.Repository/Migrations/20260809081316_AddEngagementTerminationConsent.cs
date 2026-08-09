using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <summary>
    /// Huỷ ngang engagement cần ĐỒNG THUẬN HAI BÊN: thêm 4 cột vết đề nghị/chốt huỷ trên
    /// project_providers (termination_requested_at / _by / _note + terminated_at).
    ///
    /// Kèm theo là phần RECONCILE cho design_versions — KHÔNG liên quan tới tính năng này:
    /// migration 20260808193000_AddCommentsAndDesignVersions viết tay nên lệch model
    /// (đặt default now() nhầm sang snapshotted_at thay vì created_at theo convention chung ở
    /// SmartCafeBuilderContext, và thiếu index cho các cột FK). EF sinh ra để kéo DB về đúng model.
    /// </summary>
    public partial class AddEngagementTerminationConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ───────── Huỷ ngang đồng thuận hai bên ─────────
            migrationBuilder.AddColumn<DateTime>(
                name: "terminated_at",
                table: "project_providers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "termination_request_note",
                table: "project_providers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "termination_requested_at",
                table: "project_providers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "termination_requested_by",
                table: "project_providers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            // ───────── Reconcile design_versions về đúng model (không liên quan huỷ ngang) ─────────
            migrationBuilder.AlterColumn<DateTime>(
                name: "snapshotted_at",
                table: "design_versions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "design_versions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "ix_design_versions_created_by",
                table: "design_versions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_design_versions_snapshotted_by",
                table: "design_versions",
                column: "snapshotted_by");

            migrationBuilder.CreateIndex(
                name: "ix_design_version_images_original_image_id",
                table: "design_version_images",
                column: "original_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_design_version_images_uploaded_by",
                table: "design_version_images",
                column: "uploaded_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_design_versions_created_by",
                table: "design_versions");

            migrationBuilder.DropIndex(
                name: "ix_design_versions_snapshotted_by",
                table: "design_versions");

            migrationBuilder.DropIndex(
                name: "ix_design_version_images_original_image_id",
                table: "design_version_images");

            migrationBuilder.DropIndex(
                name: "ix_design_version_images_uploaded_by",
                table: "design_version_images");

            migrationBuilder.DropColumn(
                name: "terminated_at",
                table: "project_providers");

            migrationBuilder.DropColumn(
                name: "termination_request_note",
                table: "project_providers");

            migrationBuilder.DropColumn(
                name: "termination_requested_at",
                table: "project_providers");

            migrationBuilder.DropColumn(
                name: "termination_requested_by",
                table: "project_providers");

            migrationBuilder.AlterColumn<DateTime>(
                name: "snapshotted_at",
                table: "design_versions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "design_versions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");
        }
    }
}
