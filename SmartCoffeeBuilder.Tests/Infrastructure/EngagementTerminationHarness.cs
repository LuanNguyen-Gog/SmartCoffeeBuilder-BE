using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Implementations;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.Implementations;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Tests.Infrastructure;

/// <summary>
/// Test bed cho luồng huỷ ngang đồng thuận hai bên của <see cref="ProjectWorkingService"/>:
/// một EF Core InMemory database dùng chung nhiều DbContext (seed bằng context này, hành động
/// bằng context khác để tránh xung đột change-tracker). INotificationService được mock để
/// khẳng định noti/email được bắn đúng bên, đúng thời điểm — không chạm email thật.
/// </summary>
public sealed class EngagementTerminationHarness
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    public Mock<INotificationService> Notifications { get; } = new();

    public SmartCafeBuilderContext NewDb() =>
        new(new DbContextOptionsBuilder<SmartCafeBuilderContext>()
            .UseInMemoryDatabase(_dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    public ProjectWorkingService NewService(SmartCafeBuilderContext db) =>
        new(new UnitOfWork<SmartCafeBuilderContext>(db), Notifications.Object);

    /// <summary>
    /// Seed graph owner → project → engagement 'accepted' + contract 'confirmed'.
    /// <paramref name="viaMarketplace"/> = true thì engagement sinh từ post + apply (post đã 'closed'),
    /// để kiểm chứng cửa vào marketplace mở lại sau khi huỷ ngang.
    /// </summary>
    public TerminationSeed Seed(
        ProviderStatus engagementStatus = ProviderStatus.accepted,
        DateTime? terminationRequestedAt = null,
        EngagementParty? terminationRequestedBy = null,
        bool viaMarketplace = false,
        DateTime? postDeadline = null,
        ProjectStatus projectStatus = ProjectStatus.in_progress,
        long? adminAccount = null)
    {
        using var db = NewDb();
        var now = DateTime.UtcNow;

        var ownerAcc = new Account { Email = "owner@test.local", PasswordHash = "x", Role = AccountRole.owner, Status = AccountStatus.active, CreatedAt = now, UpdatedAt = now };
        var shop = new ShopOwner { Account = ownerAcc, FullName = "Owner", ShopName = "Shop", Phone = "0900000000", Address = "Addr", CreatedAt = now, UpdatedAt = now };
        var project = new ProjectShopOwner { Owner = shop, Name = "Project", Address = "Addr", AreaM2 = 10m, Budget = 1_000_000m, Status = projectStatus, CreatedAt = now, UpdatedAt = now };

        var provAcc = new Account { Email = "provider@test.local", PasswordHash = "x", Role = AccountRole.provider, Status = AccountStatus.active, CreatedAt = now, UpdatedAt = now };
        var provider = new ServiceProviderProfile { Account = provAcc, DisplayName = "Provider", ProviderType = ProviderType.company, Capability = Capability.designer, AvgRating = 0m, CreatedAt = now, UpdatedAt = now };

        var engagement = new ProjectWorking
        {
            ProjectShopOwner = project,
            ServiceProviderProfile = provider,
            ContractType = ServiceKind.design,
            Status = engagementStatus,
            TerminationRequestedAt = terminationRequestedAt,
            TerminationRequestedBy = terminationRequestedBy,
            CreatedAt = now,
            UpdatedAt = now
        };

        Post? post = null;
        if (viaMarketplace)
        {
            post = new Post
            {
                ProjectShopOwner = project,
                Title = "Tuyển designer",
                Description = "Cần designer cho quán cà phê",
                ServiceKind = ServiceKind.design,
                Status = PostStatus.closed,          // accept hồ sơ đã đóng bài
                SubmissionDeadline = postDeadline,
                CreatedAt = now,
                UpdatedAt = now
            };
            var apply = new Apply
            {
                Post = post,
                ServiceProviderProfile = provider,
                Proposal = "Hồ sơ ban đầu",
                Status = ApplicationStatus.accepted,
                SubmittedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            engagement.Apply = apply;
            db.Add(apply);
        }

        db.Add(engagement);
        db.Add(new Contract { ProjectWorking = engagement, Title = "Signed", Status = ContractStatus.confirmed, ConfirmedAt = now, CreatedAt = now, UpdatedAt = now });

        long adminId = 0;
        if (adminAccount is not null)
        {
            var adminAcc = new Account { Email = "admin@test.local", PasswordHash = "x", Role = AccountRole.admin, Status = AccountStatus.active, CreatedAt = now, UpdatedAt = now };
            db.Add(adminAcc);
            db.SaveChanges();
            adminId = adminAcc.Id;
        }

        db.SaveChanges();

        return new TerminationSeed(
            engagement.Id, ownerAcc.Id, provAcc.Id, adminId,
            project.Id, provider.Id, post?.Id ?? 0, engagement.ApplyId ?? 0);
    }

    /// <summary>Đọc lại engagement từ một context mới (không dính change-tracker của service).</summary>
    public ProjectWorking Reload(long engagementId)
    {
        using var db = NewDb();
        return db.ProjectWorkings.AsNoTracking().Single(e => e.Id == engagementId);
    }

    public Post ReloadPost(long postId)
    {
        using var db = NewDb();
        return db.Posts.AsNoTracking().Single(p => p.Id == postId);
    }
}

public readonly record struct TerminationSeed(
    long EngagementId, long OwnerAccountId, long ProviderAccountId, long AdminAccountId,
    long ProjectId, long ProviderProfileId, long PostId, long ApplyId);
