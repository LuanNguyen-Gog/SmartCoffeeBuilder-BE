using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.SeedData;

/// <summary>
/// Seed tối thiểu: 3 tài khoản provider để test (designer / constructor / both) + các bảng lookup
/// mà API cần mới chạy được (issue_types, doc_types, subscription_plans) + mẫu quy trình thi công
/// công khai (không có đường nào khác tạo được mẫu <c>IsPublic</c>, xem
/// <see cref="SeedConstructionTemplatesAsync"/>). KHÔNG seed dữ liệu giao dịch — dự án, hợp đồng,
/// thiết kế… đều tạo qua API khi test.
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
        await SeedConstructionTemplatesAsync(db, ct);
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

    /// <summary>
    /// Mẫu quy trình CÔNG KHAI của hệ thống (review 3: "add thêm template cho quá trình thi công").
    ///
    /// Phải seed ở đây chứ không tạo qua API được: <c>ConstructionTemplateService.CreateAsync</c>
    /// ép <c>IsPublic = false</c> cho mọi mẫu provider tạo — công khai là quyết định quản trị. Không
    /// có bước seed này thì màn "chọn mẫu" của provider mở ra trống trơn cho tới khi có người tự gõ
    /// một bộ hạng mục từ đầu, đúng thứ mà template sinh ra để khỏi phải làm.
    ///
    /// <c>CreatedBy = null</c> đánh dấu mẫu của hệ thống, không thuộc provider nào — nên không ai
    /// xoá được nó qua <c>DeleteAsync</c> (chỉ admin).
    ///
    /// Thời lượng là ước tính cho một mặt bằng 80–120 m² và chỉ là điểm khởi đầu: áp mẫu là COPY,
    /// provider sửa lại mốc trên dự án của mình mà không đụng gì tới mẫu gốc.
    /// </summary>
    private static async Task SeedConstructionTemplatesAsync(SmartCafeBuilderContext db, CancellationToken ct)
    {
        // Gate on PUBLIC templates, not on the table being empty. Providers author
        // their own private mẫu through the API, so an "any row at all" check would
        // let the first provider-created template block the system set from ever
        // being seeded — on a shared database that is one `POST` away.
        if (await db.ConstructionTemplates.AnyAsync(t => t.IsPublic, ct))
            return;

        var now = DateTime.UtcNow;

        db.ConstructionTemplates.AddRange(
            new ConstructionTemplate
            {
                Name = "Thi công quán cà phê - Quy trình chuẩn",
                Description =
                    "Chín giai đoạn từ nhận mặt bằng tới nghiệm thu bàn giao, áp cho mặt bằng " +
                    "80–120 m². Tổng thời lượng dự kiến 71 ngày.",
                ServiceKind = ServiceKind.construction,
                IsPublic = true,
                CreatedBy = null,
                CreatedAt = now,
                UpdatedAt = now,
                Items = BuildTemplateItems(
                    ("Chuẩn bị mặt bằng & tháo dỡ", "Bàn giao mặt bằng, dọn hiện trạng cũ và định vị theo bản vẽ.", "Chuẩn bị", 5, new[]
                    {
                        ("Nhận mặt bằng, chụp ảnh hiện trạng", "Lập biên bản bàn giao kèm ảnh làm căn cứ đối chiếu khi nghiệm thu.", 1),
                        ("Tháo dỡ vách, trần và thiết bị cũ", (string?)null, 2),
                        ("Vận chuyển phế thải, vệ sinh mặt bằng", null, 1),
                        ("Định vị tim trục, bật mực theo bản vẽ", "Sai ở bước này kéo lệch toàn bộ các hạng mục sau.", 1),
                    }),
                    ("Phần thô & xây tô", "Xây tường ngăn, tô trát, chống thấm khu ướt.", "Kết cấu", 10, new[]
                    {
                        ("Xây tường ngăn khu pha chế, kho, WC", (string?)null, 4),
                        ("Tô trát, cán nền tạo dốc", null, 3),
                        ("Chống thấm WC và khu pha chế", "Ngâm thử nước 24h trước khi cho ốp lát đè lên.", 3),
                    }),
                    ("Hệ thống điện - nước (MEP)", "Đi ngầm toàn bộ đường điện, cấp thoát nước và mạng trước khi đóng trần.", "MEP", 12, new[]
                    {
                        ("Đi ống điện âm tường, âm trần", (string?)null, 3),
                        ("Đi ống cấp thoát nước quầy bar và WC", null, 3),
                        ("Lắp tủ điện, aptomat, kéo dây trục chính", "Tính tải riêng cho máy espresso và máy lạnh.", 2),
                        ("Đi dây mạng, camera, loa", null, 2),
                        ("Thử áp lực nước, đo cách điện", "Nghiệm thu phần ngầm — sau bước này là đóng trần, sửa rất đắt.", 2),
                    }),
                    ("Trần - vách - sàn", "Đóng trần, ốp lát sàn và mặt dựng.", "Hoàn thiện thô", 10, new[]
                    {
                        ("Đóng khung xương trần thạch cao", (string?)null, 3),
                        ("Hoàn thiện tấm trần, xử lý mối nối", null, 2),
                        ("Ốp lát sàn khu khách và khu pha chế", null, 4),
                        ("Ốp gạch mặt dựng quầy bar, WC", null, 1),
                    }),
                    ("Sơn nước & hoàn thiện bề mặt", "Bả, sơn lót và sơn phủ toàn bộ tường trần.", "Hoàn thiện", 7, new[]
                    {
                        ("Bả matit, xả nhám", (string?)null, 3),
                        ("Sơn lót chống kiềm", null, 1),
                        ("Sơn phủ hai lớp hoàn thiện", null, 3),
                    }),
                    ("Quầy bar & nội thất cố định", "Gia công tại xưởng rồi lắp đặt tại công trình.", "Nội thất", 12, new[]
                    {
                        ("Gia công quầy bar tại xưởng", "Chạy song song với các hạng mục hoàn thiện tại công trình.", 6),
                        ("Lắp đặt quầy bar, mặt đá", null, 3),
                        ("Lắp kệ trưng bày, tủ bếp, kho", null, 2),
                        ("Lắp chậu rửa, vòi, hệ thoát quầy", null, 1),
                    }),
                    ("Thiết bị & chiếu sáng", "Lắp đèn theo layout ánh sáng và toàn bộ thiết bị vận hành.", "MEP", 6, new[]
                    {
                        ("Lắp đèn chiếu sáng theo layout", (string?)null, 2),
                        ("Lắp máy lạnh, quạt hút, thông gió bếp", null, 2),
                        ("Lắp thiết bị pha chế, chạy thử", "Máy espresso, máy xay, tủ mát — thử tải thật trước khi nghiệm thu.", 2),
                    }),
                    ("Biển hiệu & nhận diện thương hiệu", "Mặt tiền và các hạng mục nhận diện trong quán.", "Nhận diện", 5, new[]
                    {
                        ("Gia công biển hiệu mặt tiền", (string?)null, 3),
                        ("Lắp biển hiệu, đèn hắt mặt tiền", null, 1),
                        ("Dán decal, tranh tường, bảng menu", null, 1),
                    }),
                    ("Vệ sinh & nghiệm thu bàn giao", "Chạy thử toàn hệ thống và nghiệm thu theo checklist.", "Nghiệm thu", 4, new[]
                    {
                        ("Vệ sinh công nghiệp toàn bộ mặt bằng", (string?)null, 2),
                        ("Chạy thử tổng thể điện, nước, thiết bị", null, 1),
                        ("Nghiệm thu theo checklist, lập biên bản bàn giao", "Đính kèm ảnh minh chứng cho từng mục chưa đạt.", 1),
                    })),
            },
            new ConstructionTemplate
            {
                Name = "Thiết kế quán cà phê - Quy trình chuẩn",
                Description =
                    "Bốn giai đoạn từ khảo sát tới bàn giao hồ sơ kỹ thuật. Tổng thời lượng dự " +
                    "kiến 28 ngày, đã tính hai vòng chỉnh sửa concept.",
                ServiceKind = ServiceKind.design,
                IsPublic = true,
                CreatedBy = null,
                CreatedAt = now,
                UpdatedAt = now,
                Items = BuildTemplateItems(
                    ("Khảo sát & chốt yêu cầu", "Đo đạc hiện trạng và thống nhất brief với chủ quán.", "Khảo sát", 3, new[]
                    {
                        ("Khảo sát hiện trạng, đo đạc mặt bằng", "Số đo vào hồ sơ mặt bằng để chủ quán duyệt đồng bộ sang dự án.", 1),
                        ("Chốt brief: phong cách, công năng, ngân sách", (string?)null, 2),
                    }),
                    ("Concept & bố trí công năng", "Phương án mặt bằng và hình ảnh concept để chủ quán duyệt.", "Concept", 10, new[]
                    {
                        ("Lập mặt bằng bố trí công năng", (string?)null, 3),
                        ("Dựng 3D concept các khu vực chính", "Quầy bar, khu khách, mặt tiền.", 5),
                        ("Trình bày và chốt concept với chủ quán", null, 2),
                    }),
                    ("Hồ sơ thiết kế kỹ thuật", "Bộ bản vẽ đủ để nhà thầu bóc khối lượng và thi công.", "Kỹ thuật", 12, new[]
                    {
                        ("Bản vẽ mặt bằng, mặt cắt, mặt đứng", (string?)null, 5),
                        ("Bản vẽ chi tiết quầy bar và nội thất", null, 4),
                        ("Bản vẽ phối hợp điện - nước", "Khớp với layout thiết bị pha chế đã chốt.", 3),
                    }),
                    ("Bàn giao hồ sơ", "Thống kê vật tư và nghiệm thu thiết kế.", "Bàn giao", 3, new[]
                    {
                        ("Lập bảng thống kê vật tư, bảng finish", (string?)null, 2),
                        ("Bàn giao hồ sơ, nghiệm thu thiết kế", null, 1),
                    })),
            });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Dựng danh sách hạng mục mẫu kèm việc con, tự đánh <c>SortOrder</c> theo thứ tự khai báo —
    /// thứ tự các giai đoạn thi công là thông tin nghiệp vụ, đánh số tay thì thêm/bớt một giai đoạn
    /// là phải sửa lại cả dãy.
    /// </summary>
    private static List<ConstructionTemplateItem> BuildTemplateItems(
        params (string Name, string? Description, string Category, int EstimateDays,
                (string Name, string? Description, int EstimateDays)[] Tasks)[] items)
    {
        // Cả hai cấp đều lấy thứ tự từ overload có index của Select. Dùng biến đếm bên ngoài thì
        // SortOrder phụ thuộc vào việc lambda được duyệt bao nhiêu lần — đúng chỉ vì có ToList()
        // ngay sau, và hỏng lặng lẽ nếu về sau ai đó trả về IEnumerable rồi duyệt hai lần.
        return items.Select((i, itemOrder) => new ConstructionTemplateItem
        {
            Name = i.Name,
            Description = i.Description,
            Category = i.Category,
            EstimateDays = i.EstimateDays,
            SortOrder = itemOrder,
            Tasks = i.Tasks.Select((t, taskOrder) => new ConstructionTemplateTask
            {
                Name = t.Name,
                Description = t.Description,
                EstimateDays = t.EstimateDays,
                SortOrder = taskOrder,
            }).ToList(),
        }).ToList();
    }
}
