using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCoffeeBuilder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddReview3QuotationPaymentChecklistTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "change_summary",
                table: "designs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "change_summary",
                table: "design_versions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "quotation_id",
                table: "contracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_paid",
                table: "construction_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "checklist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    design_id = table.Column<Guid>(type: "uuid", nullable: true),
                    construction_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    evidence_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    checked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checklist_items", x => x.id);
                    table.CheckConstraint("ck_checklist_items_target", "(design_id IS NOT NULL AND construction_item_id IS NULL) OR (design_id IS NULL AND construction_item_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_checklist_items_accounts_checked_by",
                        column: x => x.checked_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_checklist_items_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_checklist_items_construction_items_construction_item_id",
                        column: x => x.construction_item_id,
                        principalTable: "construction_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_checklist_items_designs_design_id",
                        column: x => x.design_id,
                        principalTable: "designs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "construction_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    service_kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_construction_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_construction_templates_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    application_id = table.Column<Guid>(type: "uuid", nullable: true),
                    project_provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    estimated_duration_days = table.Column<int>(type: "integer", nullable: true),
                    free_revision_count = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    revision_reason = table.Column<string>(type: "text", nullable: true),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotations", x => x.id);
                    table.CheckConstraint("ck_quotations_anchor", "(application_id IS NOT NULL AND project_provider_id IS NULL) OR (application_id IS NULL AND project_provider_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_quotations_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_quotations_accounts_responded_by",
                        column: x => x.responded_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_quotations_project_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "project_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_quotations_project_providers_project_provider_id",
                        column: x => x.project_provider_id,
                        principalTable: "project_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "construction_template_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    construction_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    estimate_days = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_construction_template_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_construction_template_items_construction_templates_construc",
                        column: x => x.construction_template_id,
                        principalTable: "construction_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotation_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotation_attachments_accounts_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_quotation_attachments_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotation_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotation_items_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_payment_terms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    condition = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotation_payment_terms", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotation_payment_terms_quotations_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "construction_template_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    construction_template_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    estimate_days = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_construction_template_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_construction_template_tasks_construction_template_items_con",
                        column: x => x.construction_template_item_id,
                        principalTable: "construction_template_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    construction_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quotation_payment_term_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: false),
                    due_at = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    proof_submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_batches_accounts_confirmed_by",
                        column: x => x.confirmed_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payment_batches_construction_items_construction_item_id",
                        column: x => x.construction_item_id,
                        principalTable: "construction_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payment_batches_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payment_batches_quotation_payment_terms_quotation_payment_t",
                        column: x => x.quotation_payment_term_id,
                        principalTable: "quotation_payment_terms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "payment_proofs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    payment_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    transferred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_proofs", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_proofs_accounts_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_payment_proofs_payment_batches_payment_batch_id",
                        column: x => x.payment_batch_id,
                        principalTable: "payment_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_quotation_id",
                table: "contracts",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_items_checked_by",
                table: "checklist_items",
                column: "checked_by");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_items_construction_item_id",
                table: "checklist_items",
                column: "construction_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_items_created_by",
                table: "checklist_items",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_items_design_id",
                table: "checklist_items",
                column: "design_id");

            migrationBuilder.CreateIndex(
                name: "ix_construction_template_items_construction_template_id",
                table: "construction_template_items",
                column: "construction_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_construction_template_tasks_construction_template_item_id",
                table: "construction_template_tasks",
                column: "construction_template_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_construction_templates_created_by",
                table: "construction_templates",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_payment_batches_confirmed_by",
                table: "payment_batches",
                column: "confirmed_by");

            migrationBuilder.CreateIndex(
                name: "ix_payment_batches_construction_item_id",
                table: "payment_batches",
                column: "construction_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_batches_contract_id",
                table: "payment_batches",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_batches_quotation_payment_term_id",
                table: "payment_batches",
                column: "quotation_payment_term_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_proofs_payment_batch_id",
                table: "payment_proofs",
                column: "payment_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_proofs_uploaded_by",
                table: "payment_proofs",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_attachments_quotation_id",
                table: "quotation_attachments",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_attachments_uploaded_by",
                table: "quotation_attachments",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_items_quotation_id",
                table: "quotation_items",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotation_payment_terms_quotation_id",
                table: "quotation_payment_terms",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_application_id",
                table: "quotations",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_created_by",
                table: "quotations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_project_provider_id",
                table: "quotations",
                column: "project_provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_quotations_responded_by",
                table: "quotations",
                column: "responded_by");

            migrationBuilder.CreateIndex(
                name: "ux_quotations_application_id_version",
                table: "quotations",
                columns: new[] { "application_id", "version" },
                unique: true,
                filter: "application_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_quotations_project_provider_id_version",
                table: "quotations",
                columns: new[] { "project_provider_id", "version" },
                unique: true,
                filter: "project_provider_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_contracts_quotations_quotation_id",
                table: "contracts",
                column: "quotation_id",
                principalTable: "quotations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contracts_quotations_quotation_id",
                table: "contracts");

            migrationBuilder.DropTable(
                name: "checklist_items");

            migrationBuilder.DropTable(
                name: "construction_template_tasks");

            migrationBuilder.DropTable(
                name: "payment_proofs");

            migrationBuilder.DropTable(
                name: "quotation_attachments");

            migrationBuilder.DropTable(
                name: "quotation_items");

            migrationBuilder.DropTable(
                name: "construction_template_items");

            migrationBuilder.DropTable(
                name: "payment_batches");

            migrationBuilder.DropTable(
                name: "construction_templates");

            migrationBuilder.DropTable(
                name: "quotation_payment_terms");

            migrationBuilder.DropTable(
                name: "quotations");

            migrationBuilder.DropIndex(
                name: "ix_contracts_quotation_id",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "change_summary",
                table: "designs");

            migrationBuilder.DropColumn(
                name: "change_summary",
                table: "design_versions");

            migrationBuilder.DropColumn(
                name: "quotation_id",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "is_paid",
                table: "construction_items");
        }
    }
}
