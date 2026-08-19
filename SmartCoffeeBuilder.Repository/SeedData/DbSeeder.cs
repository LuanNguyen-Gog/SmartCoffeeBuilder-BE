using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.SeedData;

/// <summary>
/// Seed tối thiểu: 3 tài khoản provider để test (designer / constructor / both) + các bảng lookup
/// mà API cần mới chạy được (issue_types, doc_types, subscription_plans). KHÔNG seed dữ liệu
/// nghiệp vụ mẫu — dự án, hợp đồng, thiết kế… đều tạo qua API khi test.
///
/// Mọi phần đều idempotent (kiểm tra trước khi thêm) nên gọi mỗi lần app khởi động đều an toàn.
/// Mọi tài khoản dùng chung mật khẩu: "Password123!".
///
/// LƯU Ý: <c>Program.cs</c> bọc lời gọi seed trong try/catch chỉ log — seed chết thì app VẪN start.
/// Đừng tin "app chạy được nghĩa là DB có dữ liệu", phải đọc log startup hoặc query thẳng DB.
/// </summary>
public static class DbSeeder
{
    // Hash BCrypt (workFactor 11) của "Password123!".
    private const string PasswordHash = "$2a$11$KJI0t6BifxyeyqnvukmA0u5/8WSpGOqlJp7jraLLdXUxcvQkPrpQS";

    public static async Task SeedAsync(SmartCafeBuilderContext db, CancellationToken ct = default)
    {
        await SeedLookupsAsync(db, ct);
        await SeedSubscriptionPlansAsync(db, ct);
        await SeedTestAccountsAsync(db, ct);
    }

    /// <summary>
    /// Bảng tra cứu — không phải dữ liệu mẫu mà là dữ liệu tham chiếu bắt buộc: tạo issue cần
    /// issue_type_id có thật, upload doc cần doc_type_id có thật.
    /// </summary>
    private static async Task SeedLookupsAsync(SmartCafeBuilderContext db, CancellationToken ct)
    {
        if (!await db.IssueTypes.AnyAsync(ct))
        {
            db.IssueTypes.AddRange(
                new IssueType { Code = "design_revision",   Name = "Yêu cầu chỉnh sửa thiết kế" },
                new IssueType { Code = "material_delay",    Name = "Chậm trễ vật tư" },
                new IssueType { Code = "site_condition",    Name = "Vấn đề hiện trạng mặt bằng" },
                new IssueType { Code = "quality_defect",    Name = "Lỗi chất lượng thi công" });
        }

        if (!await db.DocTypes.AnyAsync(ct))
        {
            db.DocTypes.AddRange(
                new DocType { Code = "contract",          Name = "Hợp đồng" },
                new DocType { Code = "quotation",         Name = "Báo giá" },
                new DocType { Code = "technical_drawing", Name = "Bản vẽ kỹ thuật" },
                new DocType { Code = "acceptance",        Name = "Biên bản nghiệm thu" });
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Gói phí nền tảng mặc định — idempotent, chỉ chạy khi bảng còn trống.</summary>
    private static async Task SeedSubscriptionPlansAsync(SmartCafeBuilderContext db, CancellationToken ct)
    {
        if (await db.SubscriptionPlans.AnyAsync(ct))
            return;

        db.SubscriptionPlans.AddRange(
            new SubscriptionPlan
            {
                Name = "Gói Chủ Quán - 1 Tháng",
                Description = "Đăng dự án, nhận đề xuất AI và kết nối nhà cung cấp trong 30 ngày.",
                TargetRole = AccountRole.owner,
                Price = 199_000m,
                DurationInDays = 30,
            },
            new SubscriptionPlan
            {
                Name = "Gói Chủ Quán - 1 Năm",
                Description = "Toàn bộ quyền lợi gói tháng, tiết kiệm hơn khi trả theo năm.",
                TargetRole = AccountRole.owner,
                Price = 1_990_000m,
                DurationInDays = 365,
            },
            new SubscriptionPlan
            {
                Name = "Gói Nhà Cung Cấp - 1 Tháng",
                Description = "Nhận job thiết kế/thi công từ marketplace trong 30 ngày.",
                TargetRole = AccountRole.provider,
                Price = 299_000m,
                DurationInDays = 30,
            },
            new SubscriptionPlan
            {
                Name = "Gói Nhà Cung Cấp - 1 Năm",
                Description = "Toàn bộ quyền lợi gói tháng, tiết kiệm hơn khi trả theo năm.",
                TargetRole = AccountRole.provider,
                Price = 2_990_000m,
                DurationInDays = 365,
            });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Bốn tài khoản test: ba provider phân biệt nhau bằng <see cref="Capability"/>
    /// (designer / constructor / both) và một quản trị viên.
    /// Idempotent theo email nên chạy lại không tạo trùng.
    ///
    /// Admin KHÔNG có <see cref="ServiceProviderProfile"/> — nó là vai trò quản trị, không phải
    /// một bên tham gia dự án; gắn hồ sơ năng lực cho admin sẽ làm hỏng mọi query lọc theo provider.
    /// </summary>
    private static async Task SeedTestAccountsAsync(SmartCafeBuilderContext db, CancellationToken ct)
    {
        var emails = new[] { "designer@scb.com", "constructor@scb.com", "both@scb.com", "admin@scb.com" };

        var existing = await db.Accounts
            .Where(a => emails.Contains(a.Email))
            .Select(a => a.Email)
            .ToListAsync(ct);

        // Account tạo qua navigation của ServiceProviderProfile — EF tự fix-up account_id sau khi
        // insert, không cần chờ SaveChanges để lấy Id (uuid chỉ có giá trị SAU khi save).
        Account NewProviderAccount(string email, string phone) => new()
        {
            Email = email,
            Phone = phone,
            PasswordHash = PasswordHash,
            Role = AccountRole.provider,
            Status = AccountStatus.active,
            EmailVerifiedAt = DateTime.UtcNow,
        };

        if (!existing.Contains("designer@scb.com"))
        {
            db.ServiceProviderProfiles.Add(new ServiceProviderProfile
            {
                Account = NewProviderAccount("designer@scb.com", "0900000004"),
                DisplayName = "Studio Mộc Design",
                ProviderType = ProviderType.company,
                Capability = Capability.designer,
                Bio = "Chuyên thiết kế nội thất quán cà phê phong cách tối giản.",
                CompanyTaxCode = "0312345678",
                YearsExperience = 8,
                PortfolioHeadline = "20+ quán cà phê đã hoàn thiện",
                IsVerified = true,
                AvgRating = 0m,
                DesignerProfile = new DesignerProfile
                {
                    Specialties = "Nội thất F&B, quán cà phê, nhà hàng",
                    SoftwareSkills = "SketchUp, AutoCAD, Lumion, Photoshop",
                    DesignStyle = "Minimalist, Industrial, Scandinavian",
                    MinProjectBudget = 50_000_000m,
                },
            });
        }

        if (!existing.Contains("constructor@scb.com"))
        {
            db.ServiceProviderProfiles.Add(new ServiceProviderProfile
            {
                Account = NewProviderAccount("constructor@scb.com", "0900000005"),
                DisplayName = "Xây Dựng An Phát",
                ProviderType = ProviderType.company,
                Capability = Capability.constructor,
                Bio = "Nhà thầu thi công nội thất F&B, cam kết đúng tiến độ.",
                CompanyTaxCode = "0398765432",
                YearsExperience = 10,
                PortfolioHeadline = "Thi công trọn gói chuỗi cà phê",
                IsVerified = true,
                AvgRating = 0m,
                ConstructorProfile = new ConstructorProfile
                {
                    LicenseNo = "GPXD-2019-0456",
                    TeamSize = 25,
                    Equipment = "Giàn giáo, máy cắt CNC, xe nâng, máy phun sơn",
                    MaxProjectValue = 2_000_000_000m,
                    WarrantyPolicy = "Bảo hành 12 tháng phần thô, 6 tháng phần hoàn thiện.",
                },
            });
        }

        // Capability.both = NĂNG LỰC hồ sơ (làm được cả hai), không phải phạm vi công việc.
        // Provider này vẫn có thể chỉ nhận đúng phần design hoặc đúng phần construction của một dự án.
        if (!existing.Contains("both@scb.com"))
        {
            db.ServiceProviderProfiles.Add(new ServiceProviderProfile
            {
                Account = NewProviderAccount("both@scb.com", "0900000006"),
                DisplayName = "Combo Design & Build",
                ProviderType = ProviderType.company,
                Capability = Capability.both,
                Bio = "Thiết kế và thi công trọn gói cho quán cà phê.",
                CompanyTaxCode = "0344455667",
                YearsExperience = 6,
                PortfolioHeadline = "Design & Build một đầu mối",
                IsVerified = true,
                AvgRating = 0m,
                DesignerProfile = new DesignerProfile
                {
                    Specialties = "Thiết kế quán nhỏ, take-away",
                    SoftwareSkills = "SketchUp, Enscape",
                    DesignStyle = "Modern, Cozy",
                    MinProjectBudget = 30_000_000m,
                },
                ConstructorProfile = new ConstructorProfile
                {
                    LicenseNo = "GPXD-2021-0789",
                    TeamSize = 12,
                    Equipment = "Máy cắt, máy khoan, giàn giáo",
                    MaxProjectValue = 800_000_000m,
                    WarrantyPolicy = "Bảo hành 12 tháng toàn bộ hạng mục.",
                },
            });
        }

        // Quản trị viên: chỉ một bản ghi accounts, không kèm hồ sơ nào. Dùng để test các endpoint
        // [Authorize(Roles = "admin")] và các đường "admin đi xuyên" trong service.
        if (!existing.Contains("admin@scb.com"))
        {
            db.Accounts.Add(new Account
            {
                Email = "admin@scb.com",
                Phone = "0900000009",
                PasswordHash = PasswordHash,
                Role = AccountRole.admin,
                Status = AccountStatus.active,
                EmailVerifiedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
