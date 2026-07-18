using Microsoft.EntityFrameworkCore;
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
    public DbSet<ServiceProvider> ServiceProviders => Set<ServiceProvider>();
    public DbSet<DesignerProfile> DesignerProfiles => Set<DesignerProfile>();
    public DbSet<ConstructorProfile> ConstructorProfiles => Set<ConstructorProfile>();

    // Nhóm 2 — Dự án & AI
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<DesignBrief> DesignBriefs => Set<DesignBrief>();
    public DbSet<AiRecommendation> AiRecommendations => Set<AiRecommendation>();
    public DbSet<BudgetItem> BudgetItems => Set<BudgetItem>();

    // Nhóm 3 — Marketplace
    public DbSet<ProjectPost> ProjectPosts => Set<ProjectPost>();
    public DbSet<ProjectApplication> ProjectApplications => Set<ProjectApplication>();

    // Nhóm 4 — Trục trung tâm
    public DbSet<ProjectProvider> ProjectProviders => Set<ProjectProvider>();

    // Nhóm 5 — Thiết kế
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Design> Designs => Set<Design>();
    public DbSet<DesignImage> DesignImages => Set<DesignImage>();

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
    public DbSet<Notification> Notifications => Set<Notification>();

    // Nhóm 9 — Đánh giá
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewScore> ReviewScores => Set<ReviewScore>();

    // Nhóm 10 — Thanh toán (phí nền tảng qua payOS)
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

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
        configurationBuilder.Properties<DesignStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<DesignType>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ItemStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<IssueStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ContractStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<SubscriptionStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<PaymentTransactionStatus>().HaveConversion<string>().HaveMaxLength(30);
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

        modelBuilder.Entity<ServiceProvider>(e =>
        {
            e.HasIndex(x => x.AccountId).IsUnique();
            e.Property(x => x.AvgRating).HasPrecision(3, 2);
            e.HasOne(x => x.Account).WithOne(a => a.ServiceProvider)
                .HasForeignKey<ServiceProvider>(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DesignerProfile>(e =>
        {
            e.HasIndex(x => x.ProviderId).IsUnique();
            e.Property(x => x.MinProjectBudget).HasPrecision(15, 2);
            e.HasOne(x => x.Provider).WithOne(p => p.DesignerProfile)
                .HasForeignKey<DesignerProfile>(x => x.ProviderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConstructorProfile>(e =>
        {
            e.HasIndex(x => x.ProviderId).IsUnique();
            e.Property(x => x.MaxProjectValue).HasPrecision(15, 2);
            e.HasOne(x => x.Provider).WithOne(p => p.ConstructorProfile)
                .HasForeignKey<ConstructorProfile>(x => x.ProviderId).OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── Nhóm 2 — Dự án & AI ─────────
        modelBuilder.Entity<Project>(e =>
        {
            e.HasIndex(x => x.OwnerId);
            e.Property(x => x.AreaM2).HasPrecision(10, 2);
            e.Property(x => x.Budget).HasPrecision(15, 2);
            e.HasOne(x => x.Owner).WithMany(o => o.Projects)
                .HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DesignBrief>(e =>
        {
            e.HasIndex(x => x.ProjectId).IsUnique();
            e.HasOne(x => x.Project).WithOne(p => p.DesignBrief)
                .HasForeignKey<DesignBrief>(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasIndex(x => x.ProjectId);
            e.Property(x => x.PlannedAmount).HasPrecision(15, 2);
            e.Property(x => x.ActualAmount).HasPrecision(15, 2);
            e.HasOne(x => x.Project).WithMany(p => p.BudgetItems)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // ───────── Nhóm 3 — Marketplace ─────────
        modelBuilder.Entity<ProjectPost>(e =>
        {
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Project).WithMany(p => p.ProjectPosts)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectApplication>(e =>
        {
            e.HasIndex(x => x.PostId);
            e.HasIndex(x => x.ProviderId);
            e.HasOne(x => x.Post).WithMany(p => p.ProjectApplications)
                .HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Provider).WithMany(p => p.ProjectApplications)
                .HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        });

        // ───────── Nhóm 4 — Trục ─────────
        modelBuilder.Entity<ProjectProvider>(e =>
        {
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.ProviderId);
            e.HasIndex(x => x.ApplicationId);
            e.HasOne(x => x.Project).WithMany(p => p.ProjectProviders)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Provider).WithMany(p => p.ProjectProviders)
                .HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Application).WithMany(a => a.ProjectProviders)
                .HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.SetNull);
        });

        // ───────── Nhóm 5 — Thiết kế ─────────
        modelBuilder.Entity<Survey>(e =>
        {
            e.HasIndex(x => x.ProjectProviderId);
            e.Property(x => x.Version).HasPrecision(4, 1);
            e.HasOne(x => x.ProjectProvider).WithMany(p => p.Surveys)
                .HasForeignKey(x => x.ProjectProviderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByAccount).WithMany()
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Design>(e =>
        {
            e.HasIndex(x => x.ProjectProviderId);
            e.Property(x => x.Version).HasPrecision(4, 1);
            e.HasOne(x => x.ProjectProvider).WithMany(p => p.Designs)
                .HasForeignKey(x => x.ProjectProviderId).OnDelete(DeleteBehavior.Cascade);
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

        // ───────── Nhóm 6 — Thi công ─────────
        modelBuilder.Entity<ConstructionItem>(e =>
        {
            e.HasIndex(x => x.ProjectProviderId);
            e.HasIndex(x => x.ParentId);
            e.HasOne(x => x.ProjectProvider).WithMany(p => p.ConstructionItems)
                .HasForeignKey(x => x.ProjectProviderId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasIndex(x => x.ProjectProviderId);
            e.HasIndex(x => x.ConstructionItemId);
            e.HasIndex(x => x.IssueTypeId);
            e.HasOne(x => x.ProjectProvider).WithMany(p => p.Issues)
                .HasForeignKey(x => x.ProjectProviderId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasIndex(x => x.ProjectProviderId);
            e.Property(x => x.AgreedValue).HasPrecision(15, 2);
            e.Property(x => x.OtpCode).HasMaxLength(10);
            e.HasOne(x => x.ProjectProvider).WithMany(p => p.Contracts)
                .HasForeignKey(x => x.ProjectProviderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ConfirmedByAccount).WithMany()
                .HasForeignKey(x => x.ConfirmedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Doc>(e =>
        {
            e.HasIndex(x => x.ProjectProviderId);
            e.HasIndex(x => x.DocTypeId);
            e.HasOne(x => x.ProjectProvider).WithMany(p => p.Docs)
                .HasForeignKey(x => x.ProjectProviderId).OnDelete(DeleteBehavior.Cascade);
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
        modelBuilder.Entity<Conversation>(e =>
        {
            e.HasIndex(x => x.ProjectProviderId);
            e.Property(x => x.Topic).HasMaxLength(200);
            e.HasOne(x => x.ProjectProvider).WithMany(p => p.Conversations)
                .HasForeignKey(x => x.ProjectProviderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.HasIndex(x => x.ConversationId);
            e.HasIndex(x => x.SenderId);
            e.HasOne(x => x.Conversation).WithMany(c => c.Messages)
                .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Sender).WithMany()
                .HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
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
            e.HasIndex(x => x.ProjectProviderId);
            e.Property(x => x.OverallRating).HasPrecision(3, 2);
            e.HasOne(x => x.ProjectProvider).WithMany(p => p.Reviews)
                .HasForeignKey(x => x.ProjectProviderId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasOne(x => x.Subscription).WithMany(s => s.PaymentTransactions)
                .HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Cascade);
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
    }
}
