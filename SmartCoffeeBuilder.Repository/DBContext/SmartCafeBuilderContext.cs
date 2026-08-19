using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.DBContext;

public class SmartCafeBuilderContext : DbContext
{
    public SmartCafeBuilderContext(DbContextOptions<SmartCafeBuilderContext> options)
        : base(options)
    {
    }

    // Nhóm 1 — Định danh & Actor
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ShopOwner> ShopOwners => Set<ShopOwner>();
    public DbSet<ServiceProviderProfile> ServiceProviderProfiles => Set<ServiceProviderProfile>();
    public DbSet<DesignerProfile> DesignerProfiles => Set<DesignerProfile>();
    public DbSet<ConstructorProfile> ConstructorProfiles => Set<ConstructorProfile>();

    // Nhóm 2 — Dự án & AI
    public DbSet<ProjectShopOwner> ProjectShopOwners => Set<ProjectShopOwner>();
    public DbSet<DesignBrief> DesignBriefs => Set<DesignBrief>();
    public DbSet<AiRecommendation> AiRecommendations => Set<AiRecommendation>();
    public DbSet<BudgetItem> BudgetItems => Set<BudgetItem>();

    // Nhóm 3 — Marketplace
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Apply> Applies => Set<Apply>();

    // Nhóm 3.1 — Báo giá (giai đoạn TIỀN hợp đồng): neo vào Apply hoặc ProjectWorking
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<QuotationPaymentTerm> QuotationPaymentTerms => Set<QuotationPaymentTerm>();
    public DbSet<QuotationAttachment> QuotationAttachments => Set<QuotationAttachment>();

    // Nhóm 4 — Trục trung tâm
    public DbSet<ProjectWorking> ProjectWorkings => Set<ProjectWorking>();

    // Nhóm 5 — Thiết kế
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Design> Designs => Set<Design>();
    public DbSet<DesignImage> DesignImages => Set<DesignImage>();
    public DbSet<DesignVersion> DesignVersions => Set<DesignVersion>();
    public DbSet<DesignVersionImage> DesignVersionImages => Set<DesignVersionImage>();

    // Nhóm 5.1 — Thread comment (FK mềm vào ConstructionItem + Design; mở rộng thêm entity khác sau).
    public DbSet<Comment> Comments => Set<Comment>();

    // Nhóm 6 — Thi công
    public DbSet<ConstructionItem> ConstructionItems => Set<ConstructionItem>();
    public DbSet<ConstructionTask> ConstructionTasks => Set<ConstructionTask>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueType> IssueTypes => Set<IssueType>();

    // Nhóm 7 — Hợp đồng & File
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Doc> Docs => Set<Doc>();
    public DbSet<DocType> DocTypes => Set<DocType>();

    // Nhóm 8 — Giao tiếp
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Nhóm 9 — Đánh giá
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewScore> ReviewScores => Set<ReviewScore>();

    // Nhóm 10 — Thanh toán (phí nền tảng qua payOS)
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    // Nhóm 10.1 — Thanh toán owner → provider theo đợt (hệ thống KHÔNG giữ tiền)
    public DbSet<PaymentBatch> PaymentBatches => Set<PaymentBatch>();
    public DbSet<PaymentProof> PaymentProofs => Set<PaymentProof>();

    // Nhóm 12 — Checklist nghiệm thu (design hoặc construction_item)
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    // Nhóm 13 — Mẫu quy trình thi công tái dùng
    public DbSet<ConstructionTemplate> ConstructionTemplates => Set<ConstructionTemplate>();
    public DbSet<ConstructionTemplateItem> ConstructionTemplateItems => Set<ConstructionTemplateItem>();
    public DbSet<ConstructionTemplateTask> ConstructionTemplateTasks => Set<ConstructionTemplateTask>();

    // Auth
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Otp> Otps => Set<Otp>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Mọi enum lưu dạng text (không dùng native Postgres enum).
        configurationBuilder.Properties<AccountRole>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<AccountStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ProviderType>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<Capability>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ProjectStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ServiceKind>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<PostStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ApplicationStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ProviderStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<EngagementParty>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<DesignStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<DesignType>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ItemStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<IssueStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ContractStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<SubscriptionStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<PaymentTransactionStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<PaymentPurpose>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<PaymentPlatform>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<CommentTargetType>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<DesignVersionSnapshotKind>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<QuotationStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<PaymentBatchStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ChecklistStatus>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ───────── Nhóm 1 — Actor ─────────
        modelBuilder.Entity<Account>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.PasswordHash).HasMaxLength(255);
        });

        modelBuilder.Entity<ShopOwner>(e =>
        {
            e.HasIndex(x => x.AccountId).IsUnique();
            e.HasOne(x => x.Account).WithOne(a => a.ShopOwner)
                .HasForeignKey<ShopOwner>(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        // Các entity đổi tên (ServiceProviderProfile/ProjectShopOwner/Post/Apply/ProjectWorking)
        // vẫn map về tên bảng & cột DB cũ để không phải migration — KHÔNG bỏ ToTable/HasColumnName.
        modelBuilder.Entity<ServiceProviderProfile>(e =>
        {
            e.ToTable("service_providers");
            e.HasIndex(x => x.AccountId).IsUnique();
            e.Property(x => x.AvgRating).HasPrecision(3, 2);
            e.HasOne(x => x.Account).WithOne(a => a.ServiceProviderProfile)
                .HasForeignKey<ServiceProviderProfile>(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DesignerProfile>(e =>
        {
            e.Property(x => x.ServiceProviderProfileId).HasColumnName("provider_id");
            e.HasIndex(x => x.ServiceProviderProfileId).IsUnique().HasDatabaseName("ix_designer_profiles_provider_id");
            e.Property(x => x.MinProjectBudget).HasPrecision(15, 2);
            e.HasOne(x => x.ServiceProviderProfile).WithOne(p => p.DesignerProfile)
                .HasForeignKey<DesignerProfile>(x => x.ServiceProviderProfileId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_designer_profiles_service_providers_provider_id");
        });

        modelBuilder.Entity<ConstructorProfile>(e =>
        {
            e.Property(x => x.ServiceProviderProfileId).HasColumnName("provider_id");
            e.HasIndex(x => x.ServiceProviderProfileId).IsUnique().HasDatabaseName("ix_constructor_profiles_provider_id");
            e.Property(x => x.MaxProjectValue).HasPrecision(15, 2);
            e.HasOne(x => x.ServiceProviderProfile).WithOne(p => p.ConstructorProfile)
                .HasForeignKey<ConstructorProfile>(x => x.ServiceProviderProfileId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_constructor_profiles_service_providers_provider_id");
        });

        // ───────── Nhóm 2 — Dự án & AI ─────────
        modelBuilder.Entity<ProjectShopOwner>(e =>
        {
            e.ToTable("projects");
            e.HasIndex(x => x.OwnerId);
            e.Property(x => x.AreaM2).HasPrecision(10, 2);
            e.Property(x => x.Budget).HasPrecision(15, 2);
            e.HasOne(x => x.Owner).WithMany(o => o.ProjectShopOwners)
                .HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DesignBrief>(e =>
        {
            e.Property(x => x.ProjectShopOwnerId).HasColumnName("project_id");
            e.HasIndex(x => x.ProjectShopOwnerId).IsUnique().HasDatabaseName("ix_design_briefs_project_id");
            e.HasOne(x => x.ProjectShopOwner).WithOne(p => p.DesignBrief)
                .HasForeignKey<DesignBrief>(x => x.ProjectShopOwnerId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_design_briefs_projects_project_id");
        });

        modelBuilder.Entity<AiRecommendation>(e =>
        {
            e.HasIndex(x => x.BriefId);
            e.Property(x => x.Payload).HasColumnType("jsonb");
            e.Property(x => x.EstimatedDesignCost).HasPrecision(15, 2);
            e.Property(x => x.EstimatedConstructionCost).HasPrecision(15, 2);
            // JSONB columns for plan fields
            e.Property(x => x.LayoutZones).HasColumnType("jsonb");
            e.Property(x => x.LayoutAdjacencyRules).HasColumnType("jsonb");
            e.Property(x => x.CustomerFlow).HasColumnType("jsonb");
            e.Property(x => x.Recommendations).HasColumnType("jsonb");
            e.Property(x => x.RiskNotes).HasColumnType("jsonb");
            e.Property(x => x.ImageReferenceUrls).HasColumnType("jsonb");
            e.Property(x => x.PlanJson).HasColumnType("text");
            // Numeric precision for costs
            e.Property(x => x.FitoutMinVnd).HasPrecision(18, 2);
            e.Property(x => x.FitoutMaxVnd).HasPrecision(18, 2);
            e.Property(x => x.EquipmentMinVnd).HasPrecision(18, 2);
            e.Property(x => x.EquipmentMaxVnd).HasPrecision(18, 2);
            e.Property(x => x.ContingencyPercent).HasPrecision(5, 2);
            e.HasOne(x => x.Brief).WithMany(b => b.AiRecommendations)
                .HasForeignKey(x => x.BriefId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BudgetItem>(e =>
        {
            e.Property(x => x.ProjectShopOwnerId).HasColumnName("project_id");
            e.HasIndex(x => x.ProjectShopOwnerId).HasDatabaseName("ix_budget_items_project_id");
            e.Property(x => x.PlannedAmount).HasPrecision(15, 2);
            e.Property(x => x.ActualAmount).HasPrecision(15, 2);
            e.HasOne(x => x.ProjectShopOwner).WithMany(p => p.BudgetItems)
                .HasForeignKey(x => x.ProjectShopOwnerId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_budget_items_projects_project_id");
        });

        // ───────── Nhóm 3 — Marketplace ─────────
        modelBuilder.Entity<Post>(e =>
        {
            e.ToTable("project_posts");
            e.Property(x => x.ProjectShopOwnerId).HasColumnName("project_id");
            e.HasIndex(x => x.ProjectShopOwnerId).HasDatabaseName("ix_project_posts_project_id");
            e.HasOne(x => x.ProjectShopOwner).WithMany(p => p.Posts)
                .HasForeignKey(x => x.ProjectShopOwnerId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_project_posts_projects_project_id");
        });

        modelBuilder.Entity<Apply>(e =>
        {
            e.ToTable("project_applications");
            e.Property(x => x.ServiceProviderProfileId).HasColumnName("provider_id");
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.ServiceProviderProfileId).HasDatabaseName("ix_project_applications_provider_id");
            e.HasOne(x => x.Post).WithMany(p => p.Applies)
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ServiceProviderProfile).WithMany(p => p.Applies)
                .HasForeignKey(x => x.ServiceProviderProfileId).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_project_applications_service_providers_provider_id");
        });

        // ───────── Nhóm 4 — Trục ─────────
        modelBuilder.Entity<ProjectWorking>(e =>
        {
            e.ToTable("project_providers");
            e.Property(x => x.ProjectShopOwnerId).HasColumnName("project_id");
            e.Property(x => x.ServiceProviderProfileId).HasColumnName("provider_id");
            e.Property(x => x.ApplyId).HasColumnName("application_id");
            // Cột mới v5 — engagement chưa từng đổi tên nên để snake_case tự sinh
            // (completion_requested_at / completion_request_note).
            e.Property(x => x.CompletionRequestNote).HasMaxLength(1000);
            e.Property(x => x.TerminationRequestNote).HasMaxLength(1000);
            e.HasIndex(x => x.ProjectShopOwnerId).HasDatabaseName("ix_project_providers_project_id");
            e.HasIndex(x => x.ServiceProviderProfileId).HasDatabaseName("ix_project_providers_provider_id");
            e.HasIndex(x => x.ApplyId).HasDatabaseName("ix_project_providers_application_id");
            e.HasOne(x => x.ProjectShopOwner).WithMany(p => p.ProjectWorkings)
                .HasForeignKey(x => x.ProjectShopOwnerId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_project_providers_projects_project_id");
            e.HasOne(x => x.ServiceProviderProfile).WithMany(p => p.ProjectWorkings)
                .HasForeignKey(x => x.ServiceProviderProfileId).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_project_providers_service_providers_provider_id");
            e.HasOne(x => x.Apply).WithMany(a => a.ProjectWorkings)
                .HasForeignKey(x => x.ApplyId).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_project_providers_project_applications_application_id");
        });

        // ───────── Nhóm 5 — Thiết kế ─────────
        modelBuilder.Entity<Survey>(e =>
        {
            e.Property(x => x.ProjectWorkingId).HasColumnName("project_provider_id");
            e.HasIndex(x => x.ProjectWorkingId).HasDatabaseName("ix_surveys_project_provider_id");
            e.HasOne(x => x.ProjectWorking).WithMany(p => p.Surveys)
                .HasForeignKey(x => x.ProjectWorkingId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_surveys_project_providers_project_provider_id");
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Design>(e =>
        {
            e.Property(x => x.ProjectWorkingId).HasColumnName("project_provider_id");
            e.HasIndex(x => x.ProjectWorkingId).HasDatabaseName("ix_designs_project_provider_id");
            e.Property(x => x.Version).HasPrecision(4, 1);
            e.HasOne(x => x.ProjectWorking).WithMany(p => p.Designs)
                .HasForeignKey(x => x.ProjectWorkingId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_designs_project_providers_project_provider_id");
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DesignImage>(e =>
        {
            e.HasIndex(x => x.DesignId);
            e.HasOne(x => x.Design).WithMany(d => d.DesignImages)
                .HasForeignKey(x => x.DesignId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UploadedByAccount).WithMany()
                .HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.SetNull);
        });

        // FK mềm: target_type + target_id không có FK cứng tới entity cha — service phải tự cascade xoá.
        // Index (target_type, target_id) phục vụ list comment theo thread.
        modelBuilder.Entity<Comment>(e =>
        {
            e.HasIndex(x => new { x.TargetType, x.TargetId })
                .HasDatabaseName("ix_comments_target_type_target_id");
            e.HasIndex(x => x.CreatedBy)
                .HasDatabaseName("ix_comments_created_by");
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_comments_accounts_created_by");
        });

        // DesignVersion: mỗi submit / approve đều sinh 1 bản mới (full history) — không unique,
        // chỉ index thường để query nhanh.
        modelBuilder.Entity<DesignVersion>(e =>
        {
            e.Property(x => x.Version).HasPrecision(4, 1);
            e.HasIndex(x => new { x.DesignId, x.SnapshotKind })
                .HasDatabaseName("ix_design_versions_design_id_snapshot_kind");
            e.HasIndex(x => x.DesignId)
                .HasDatabaseName("ix_design_versions_design_id");
            e.HasOne(x => x.Design).WithMany()
                .HasForeignKey(x => x.DesignId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_design_versions_designs_design_id");
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_design_versions_accounts_created_by");
            e.HasOne(x => x.SnapshottedByAccount).WithMany()
                .HasForeignKey(x => x.SnapshottedBy).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_design_versions_accounts_snapshotted_by");
        });

        modelBuilder.Entity<DesignVersionImage>(e =>
        {
            e.HasIndex(x => x.DesignVersionId)
                .HasDatabaseName("ix_design_version_images_design_version_id");
            e.HasOne(x => x.DesignVersion).WithMany(v => v.Images)
                .HasForeignKey(x => x.DesignVersionId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_design_version_images_design_versions_design_version_id");
            e.HasOne(x => x.OriginalImage).WithMany()
                .HasForeignKey(x => x.OriginalImageId).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_design_version_images_design_images_original_image_id");
            e.HasOne(x => x.UploadedByAccount).WithMany()
                .HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_design_version_images_accounts_uploaded_by");
        });

        // ───────── Nhóm 6 — Thi công ─────────
        modelBuilder.Entity<ConstructionItem>(e =>
        {
            e.Property(x => x.ProjectWorkingId).HasColumnName("project_provider_id");
            e.HasIndex(x => x.ProjectWorkingId).HasDatabaseName("ix_construction_items_project_provider_id");
            e.HasIndex(x => x.ParentId);
            e.HasOne(x => x.ProjectWorking).WithMany(p => p.ConstructionItems)
                .HasForeignKey(x => x.ProjectWorkingId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_construction_items_project_providers_project_provider_id");
            e.HasOne(x => x.Parent).WithMany(p => p.Children)
                .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConstructionTask>(e =>
        {
            e.HasIndex(x => x.ConstructionItemId);
            e.HasOne(x => x.ConstructionItem).WithMany(c => c.Tasks)
                .HasForeignKey(x => x.ConstructionItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Issue>(e =>
        {
            e.Property(x => x.ProjectWorkingId).HasColumnName("project_provider_id");
            e.HasIndex(x => x.ProjectWorkingId).HasDatabaseName("ix_issues_project_provider_id");
            e.HasIndex(x => x.ConstructionItemId);
            e.HasIndex(x => x.IssueTypeId);
            e.HasOne(x => x.ProjectWorking).WithMany(p => p.Issues)
                .HasForeignKey(x => x.ProjectWorkingId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_issues_project_providers_project_provider_id");
            e.HasOne(x => x.ConstructionItem).WithMany(c => c.Issues)
                .HasForeignKey(x => x.ConstructionItemId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.IssueType).WithMany(t => t.Issues)
                .HasForeignKey(x => x.IssueTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<IssueType>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(150);
        });

        // ───────── Nhóm 7 — Hợp đồng & File ─────────
        modelBuilder.Entity<Contract>(e =>
        {
            e.Property(x => x.ProjectWorkingId).HasColumnName("project_provider_id");
            e.HasIndex(x => x.ProjectWorkingId).HasDatabaseName("ix_contracts_project_provider_id");
            e.Property(x => x.AgreedValue).HasPrecision(15, 2);
            e.Property(x => x.OtpCode).HasMaxLength(10);
            e.HasIndex(x => x.QuotationId);
            // Báo giá bị xoá không được kéo theo hợp đồng đã ký — chỉ gỡ liên kết.
            e.HasOne(x => x.Quotation).WithMany(q => q.Contracts)
                .HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ProjectWorking).WithMany(p => p.Contracts)
                .HasForeignKey(x => x.ProjectWorkingId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_contracts_project_providers_project_provider_id");
            e.HasOne(x => x.ConfirmedByAccount).WithMany()
                .HasForeignKey(x => x.ConfirmedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Doc>(e =>
        {
            e.Property(x => x.ProjectWorkingId).HasColumnName("project_provider_id");
            e.HasIndex(x => x.ProjectWorkingId).HasDatabaseName("ix_docs_project_provider_id");
            e.HasIndex(x => x.DocTypeId);
            e.HasOne(x => x.ProjectWorking).WithMany(p => p.Docs)
                .HasForeignKey(x => x.ProjectWorkingId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_docs_project_providers_project_provider_id");
            e.HasOne(x => x.DocType).WithMany(t => t.Docs)
                .HasForeignKey(x => x.DocTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.UploadedByAccount).WithMany()
                .HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DocType>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(150);
        });

        // ───────── Nhóm 8 — Giao tiếp ─────────
        // Conversation = thread trong một engagement; Message là tin nhắn trong thread;
        // MessageAttachment là file/ảnh đính kèm (nhiều file / 1 message).
        modelBuilder.Entity<Conversation>(e =>
        {
            e.Property(x => x.ProjectWorkingId).HasColumnName("project_working_id");
            e.HasIndex(x => x.ProjectWorkingId).HasDatabaseName("ix_conversations_project_working_id");
            e.Property(x => x.Topic).HasMaxLength(200);
            e.HasOne(x => x.ProjectWorking).WithMany(p => p.Conversations)
                .HasForeignKey(x => x.ProjectWorkingId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_conversations_project_workings_project_working_id");
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.Property(x => x.Body).HasColumnName("body");
            e.HasIndex(x => x.ConversationId);
            e.HasIndex(x => x.SenderId);
            e.HasOne(x => x.Conversation).WithMany(c => c.Messages)
                .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_messages_conversations_conversation_id");
            e.HasOne(x => x.Sender).WithMany()
                .HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_messages_accounts_sender_id");
        });

        modelBuilder.Entity<MessageAttachment>(e =>
        {
            e.Property(x => x.Url).HasColumnName("url").HasMaxLength(500);
            e.Property(x => x.FileName).HasMaxLength(255);
            e.Property(x => x.ContentType).HasMaxLength(100);
            e.HasIndex(x => x.MessageId);
            e.HasOne(x => x.Message).WithMany(m => m.Attachments)
                .HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_message_attachments_messages_message_id");
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasIndex(x => x.AccountId);
            e.Property(x => x.Type).HasMaxLength(50);
            e.Property(x => x.Title).HasMaxLength(200).HasDefaultValue(string.Empty);
            e.Property(x => x.Content).HasMaxLength(500);
            e.Property(x => x.ReferenceType).HasMaxLength(50);
            e.HasOne(x => x.Account).WithMany(a => a.Notifications)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── Nhóm 9 — Đánh giá ─────────
        modelBuilder.Entity<Review>(e =>
        {
            e.Property(x => x.ProjectWorkingId).HasColumnName("project_provider_id");
            e.HasIndex(x => x.ProjectWorkingId).HasDatabaseName("ix_reviews_project_provider_id");
            e.Property(x => x.OverallRating).HasPrecision(3, 2);
            e.HasOne(x => x.ProjectWorking).WithMany(p => p.Reviews)
                .HasForeignKey(x => x.ProjectWorkingId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_reviews_project_providers_project_provider_id");
        });

        modelBuilder.Entity<ReviewScore>(e =>
        {
            e.HasIndex(x => x.ReviewId);
            e.Property(x => x.Dimension).HasMaxLength(50);
            e.HasOne(x => x.Review).WithMany(r => r.ReviewScores)
                .HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── Nhóm 10 — Thanh toán ─────────
        modelBuilder.Entity<SubscriptionPlan>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Price).HasPrecision(15, 2);
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.PlanId);
            e.Property(x => x.PaidAmount).HasPrecision(15, 2);
            e.HasOne(x => x.Account).WithMany(a => a.Subscriptions)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Plan).WithMany(p => p.Subscriptions)
                .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentTransaction>(e =>
        {
            e.HasIndex(x => x.OrderCode).IsUnique();
            e.HasIndex(x => x.SubscriptionId);
            e.HasIndex(x => x.AccountId);
            e.Property(x => x.PaymentLinkId).HasMaxLength(100);
            e.Property(x => x.CheckoutUrl).HasMaxLength(500);
            e.Property(x => x.QrCode).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(15, 2);
            e.HasIndex(x => x.PostId);
            e.HasOne(x => x.Subscription).WithMany(s => s.PaymentTransactions)
                .HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Cascade);
            // Bài đăng bị xoá thì giữ lịch sử giao dịch, chỉ set null.
            e.HasOne(x => x.Post).WithMany()
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Account).WithMany(a => a.PaymentTransactions)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── Auth ─────────
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Token).HasMaxLength(500);
            e.HasOne(x => x.Account).WithMany(a => a.RefreshTokens)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(x => x.IsActive);
        });

        modelBuilder.Entity<Otp>(e =>
        {
            e.HasIndex(x => x.AccountId);
            e.Property(x => x.CurrentCode).HasMaxLength(10);
            e.Property(x => x.PreviousCode).HasMaxLength(10);
            e.HasOne(x => x.Account).WithMany(a => a.Otps)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(x => x.IsActive);
        });

        // ───────── Nhóm 11 — Báo giá & thanh toán owner–provider (chốt ở review 3, 17/08/2026) ─────────
        modelBuilder.Entity<Quotation>(e =>
        {
            // Hai cột neo dùng lại đúng tên cột DB của Apply/ProjectWorking (application_id /
            // project_provider_id) cho khớp với phần còn lại của schema.
            e.Property(x => x.ApplyId).HasColumnName("application_id");
            e.Property(x => x.ProjectWorkingId).HasColumnName("project_provider_id");
            e.Property(x => x.Title).HasMaxLength(255);
            e.Property(x => x.TotalAmount).HasPrecision(15, 2);

            e.HasIndex(x => x.ApplyId).HasDatabaseName("ix_quotations_application_id");
            e.HasIndex(x => x.ProjectWorkingId).HasDatabaseName("ix_quotations_project_provider_id");

            // Số hiệu bản báo giá là duy nhất TRONG một chỗ neo — hai provider khác nhau đều có
            // bản "v1" của riêng mình, nên unique phải kèm cột neo chứ không đứng một mình.
            e.HasIndex(x => new { x.ApplyId, x.Version }).IsUnique()
                .HasDatabaseName("ux_quotations_application_id_version")
                .HasFilter("application_id IS NOT NULL");
            e.HasIndex(x => new { x.ProjectWorkingId, x.Version }).IsUnique()
                .HasDatabaseName("ux_quotations_project_provider_id_version")
                .HasFilter("project_provider_id IS NOT NULL");

            // Báo giá phải neo vào ĐÚNG MỘT chỗ. Không có ràng buộc này thì sinh ra bản ghi mồ côi
            // (không neo đâu cả) hoặc neo hai chỗ mâu thuẫn nhau — cả hai đều không check quyền được.
            e.ToTable(t => t.HasCheckConstraint(
                "ck_quotations_anchor",
                "(application_id IS NOT NULL AND project_provider_id IS NULL) OR " +
                "(application_id IS NULL AND project_provider_id IS NOT NULL)"));

            e.HasOne(x => x.Apply).WithMany(a => a.Quotations)
                .HasForeignKey(x => x.ApplyId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_quotations_project_applications_application_id");
            e.HasOne(x => x.ProjectWorking).WithMany(p => p.Quotations)
                .HasForeignKey(x => x.ProjectWorkingId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_quotations_project_providers_project_provider_id");
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.RespondedByAccount).WithMany()
                .HasForeignKey(x => x.RespondedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuotationItem>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Unit).HasMaxLength(50);
            e.Property(x => x.Quantity).HasPrecision(15, 2);
            e.Property(x => x.UnitPrice).HasPrecision(15, 2);
            e.Property(x => x.Amount).HasPrecision(15, 2);
            e.HasIndex(x => x.QuotationId);
            e.HasOne(x => x.Quotation).WithMany(q => q.Items)
                .HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuotationPaymentTerm>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Percentage).HasPrecision(5, 2);
            e.Property(x => x.Amount).HasPrecision(15, 2);
            e.HasIndex(x => x.QuotationId);
            e.HasOne(x => x.Quotation).WithMany(q => q.PaymentTerms)
                .HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuotationAttachment>(e =>
        {
            e.Property(x => x.FileUrl).HasMaxLength(500);
            e.Property(x => x.FileName).HasMaxLength(255);
            e.HasIndex(x => x.QuotationId);
            e.HasOne(x => x.Quotation).WithMany(q => q.Attachments)
                .HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UploadedByAccount).WithMany()
                .HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentBatch>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Percentage).HasPrecision(5, 2);
            e.Property(x => x.Amount).HasPrecision(15, 2);
            e.HasIndex(x => x.ContractId);
            e.HasIndex(x => x.ConstructionItemId);
            e.HasOne(x => x.Contract).WithMany(c => c.PaymentBatches)
                .HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
            // Hạng mục bị xoá thì đợt tiền vẫn phải còn (đó là chứng từ) — chỉ gỡ liên kết.
            e.HasOne(x => x.ConstructionItem).WithMany(ci => ci.PaymentBatches)
                .HasForeignKey(x => x.ConstructionItemId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.QuotationPaymentTerm).WithMany()
                .HasForeignKey(x => x.QuotationPaymentTermId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ConfirmedByAccount).WithMany()
                .HasForeignKey(x => x.ConfirmedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentProof>(e =>
        {
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.Amount).HasPrecision(15, 2);
            e.HasIndex(x => x.PaymentBatchId);
            e.HasOne(x => x.PaymentBatch).WithMany(b => b.Proofs)
                .HasForeignKey(x => x.PaymentBatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UploadedByAccount).WithMany()
                .HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.SetNull);
        });

        // ───────── Nhóm 12 — Checklist nghiệm thu (review 3) ─────────
        modelBuilder.Entity<ChecklistItem>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.EvidenceUrl).HasMaxLength(500);
            e.HasIndex(x => x.DesignId);
            e.HasIndex(x => x.ConstructionItemId);

            // Mục nghiệm thu phải treo vào ĐÚNG MỘT hạng mục: mồ côi hoặc treo hai chỗ thì không
            // check quyền được (quyền suy ra từ engagement của hạng mục cha).
            e.ToTable(t => t.HasCheckConstraint(
                "ck_checklist_items_target",
                "(design_id IS NOT NULL AND construction_item_id IS NULL) OR " +
                "(design_id IS NULL AND construction_item_id IS NOT NULL)"));

            e.HasOne(x => x.Design).WithMany(d => d.ChecklistItems)
                .HasForeignKey(x => x.DesignId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ConstructionItem).WithMany(ci => ci.ChecklistItems)
                .HasForeignKey(x => x.ConstructionItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CheckedByAccount).WithMany()
                .HasForeignKey(x => x.CheckedBy).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        // ───────── Nhóm 13 — Mẫu quy trình thi công (review 3) ─────────
        modelBuilder.Entity<ConstructionTemplate>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(255);
            e.HasIndex(x => x.CreatedBy);
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConstructionTemplateItem>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Category).HasMaxLength(100);
            e.HasIndex(x => x.ConstructionTemplateId);
            e.HasOne(x => x.ConstructionTemplate).WithMany(t => t.Items)
                .HasForeignKey(x => x.ConstructionTemplateId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConstructionTemplateTask>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(255);
            e.HasIndex(x => x.ConstructionTemplateItemId);
            e.HasOne(x => x.ConstructionTemplateItem).WithMany(i => i.Tasks)
                .HasForeignKey(x => x.ConstructionTemplateItemId).OnDelete(DeleteBehavior.Cascade);
        });

        // Default now() cho mọi cột created_at / updated_at / sent_at.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var prop in entityType.GetProperties())
            {
                if (prop.ClrType == typeof(DateTime) &&
                    prop.Name is "CreatedAt" or "UpdatedAt" or "SentAt")
                {
                    prop.SetDefaultValueSql("now()");
                }
            }
        }

        // Khoá chính uuid do Postgres sinh (gen_random_uuid(), có sẵn từ PG13) — KHÔNG sinh ở tầng app.
        // Hệ quả: .Id chỉ có giá trị SAU CommitAsync; trước đó là Guid.Empty. Code phụ thuộc Id của
        // bản ghi vừa tạo phải commit trước rồi mới dùng (xem DbSeeder).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var key = entityType.FindPrimaryKey();
            if (key is null || key.Properties.Count != 1) continue;

            var pk = key.Properties[0];
            if (pk.ClrType != typeof(Guid)) continue;

            pk.SetDefaultValueSql("gen_random_uuid()");
            pk.ValueGenerated = ValueGenerated.OnAdd;
        }
    }
}
