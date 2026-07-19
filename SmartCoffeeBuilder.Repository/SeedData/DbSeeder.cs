using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.SeedData;

/// <summary>
/// Seed dữ liệu mẫu cho database. Idempotent: chỉ chạy khi bảng accounts còn trống,
/// nên có thể gọi mỗi lần app khởi động mà không tạo dữ liệu trùng.
/// Mọi tài khoản dùng chung mật khẩu: "Password123!".
/// </summary>
public static class DbSeeder
{
    // Hash BCrypt (workFactor 11) của "Password123!".
    private const string PasswordHash = "$2a$11$KJI0t6BifxyeyqnvukmA0u5/8WSpGOqlJp7jraLLdXUxcvQkPrpQS";

    public static async Task SeedAsync(SmartCafeBuilderContext db, CancellationToken ct = default)
    {
        // Gói phí nền tảng seed riêng (trước early-return) để DB cũ đã có accounts vẫn nhận được plans.
        await SeedSubscriptionPlansAsync(db, ct);

        // Đã có dữ liệu → bỏ qua hoàn toàn, không seed lại.
        if (await db.Accounts.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;
        DateTime At(int daysAgo) => DateTime.SpecifyKind(now.AddDays(-daysAgo), DateTimeKind.Utc);

        // ───────── Lookup tables ─────────
        var issueTypes = new List<IssueType>
        {
            new() { Code = "design_revision", Name = "Yêu cầu chỉnh sửa thiết kế" },
            new() { Code = "material_delay",  Name = "Chậm trễ vật tư" },
            new() { Code = "site_condition",  Name = "Vấn đề hiện trạng mặt bằng" },
            new() { Code = "quality_defect",  Name = "Lỗi chất lượng thi công" },
        };

        var docTypes = new List<DocType>
        {
            new() { Code = "contract",        Name = "Hợp đồng" },
            new() { Code = "quotation",       Name = "Báo giá" },
            new() { Code = "technical_drawing", Name = "Bản vẽ kỹ thuật" },
            new() { Code = "acceptance",      Name = "Biên bản nghiệm thu" },
        };

        // ───────── Accounts ─────────
        Account NewAccount(string email, string phone, AccountRole role) => new()
        {
            Email = email,
            Phone = phone,
            PasswordHash = PasswordHash,
            Role = role,
            Status = AccountStatus.active,
            EmailVerifiedAt = At(60),
        };

        var adminAcc       = NewAccount("admin@scb.com",       "0900000001", AccountRole.admin);
        var owner1Acc      = NewAccount("owner1@scb.com",      "0900000002", AccountRole.owner);
        var owner2Acc      = NewAccount("owner2@scb.com",      "0900000003", AccountRole.owner);
        var designerAcc    = NewAccount("designer@scb.com",    "0900000004", AccountRole.provider);
        var constructorAcc = NewAccount("constructor@scb.com", "0900000005", AccountRole.provider);
        var bothAcc        = NewAccount("both@scb.com",        "0900000006", AccountRole.provider);

        // ───────── Shop owners ─────────
        var owner1 = new ShopOwner
        {
            Account = owner1Acc,
            FullName = "Nguyễn Văn An",
            ShopName = "An's Coffee House",
            Phone = "0900000002",
            Address = "12 Nguyễn Huệ, Quận 1, TP.HCM",
        };
        var owner2 = new ShopOwner
        {
            Account = owner2Acc,
            FullName = "Trần Thị Bình",
            ShopName = "Bình Minh Cafe",
            Phone = "0900000003",
            Address = "45 Lê Lợi, Quận 3, TP.HCM",
        };

        // ───────── Service providers ─────────
        var designerProvider = new ServiceProviderProfile
        {
            Account = designerAcc,
            DisplayName = "Studio Mộc Design",
            ProviderType = ProviderType.company,
            Capability = Capability.designer,
            Bio = "Chuyên thiết kế nội thất quán cà phê phong cách tối giản.",
            CompanyTaxCode = "0312345678",
            YearsExperience = 8,
            PortfolioHeadline = "20+ quán cà phê đã hoàn thiện",
            IsVerified = true,
            AvgRating = 4.75m,
            DesignerProfile = new DesignerProfile
            {
                Specialties = "Nội thất F&B, quán cà phê, nhà hàng",
                SoftwareSkills = "SketchUp, AutoCAD, Lumion, Photoshop",
                DesignStyle = "Minimalist, Industrial, Scandinavian",
                MinProjectBudget = 50_000_000m,
            },
        };

        var constructorProvider = new ServiceProviderProfile
        {
            Account = constructorAcc,
            DisplayName = "Xây Dựng Tín Phát",
            ProviderType = ProviderType.company,
            Capability = Capability.constructor,
            Bio = "Nhà thầu thi công nội thất trọn gói khu vực TP.HCM.",
            CompanyTaxCode = "0398765432",
            YearsExperience = 12,
            PortfolioHeadline = "Thi công nhanh, đúng tiến độ",
            IsVerified = true,
            AvgRating = 4.50m,
            ConstructorProfile = new ConstructorProfile
            {
                LicenseNo = "GPXD-2019-00123",
                TeamSize = 25,
                Equipment = "Máy cắt CNC, giàn giáo, máy phun sơn công nghiệp",
                MaxProjectValue = 2_000_000_000m,
                WarrantyPolicy = "Bảo hành 24 tháng cho hạng mục thi công.",
            },
        };

        var bothProvider = new ServiceProviderProfile
        {
            Account = bothAcc,
            DisplayName = "Combo Design & Build",
            ProviderType = ProviderType.individual,
            Capability = Capability.both,
            Bio = "Thiết kế và thi công trọn gói.",
            YearsExperience = 6,
            IsVerified = false,
            AvgRating = 4.20m,
            DesignerProfile = new DesignerProfile
            {
                Specialties = "Thiết kế quán nhỏ, take-away",
                SoftwareSkills = "SketchUp, Enscape",
                DesignStyle = "Modern, Cozy",
                MinProjectBudget = 30_000_000m,
            },
            ConstructorProfile = new ConstructorProfile
            {
                LicenseNo = "GPXD-2021-00999",
                TeamSize = 8,
                Equipment = "Dụng cụ cầm tay, máy khoan, máy hàn",
                MaxProjectValue = 500_000_000m,
                WarrantyPolicy = "Bảo hành 12 tháng.",
            },
        };

        // ───────── Project 1 (lifecycle đầy đủ) ─────────
        var project1 = new ProjectShopOwner
        {
            Owner = owner1,
            Name = "An's Coffee House - Chi nhánh Quận 1",
            Address = "12 Nguyễn Huệ, Quận 1, TP.HCM",
            AreaM2 = 85.50m,
            Budget = 450_000_000m,
            Status = ProjectStatus.in_progress,
            DesignBrief = new DesignBrief
            {
                TargetCustomer = "Nhân viên văn phòng, khách du lịch 25-40 tuổi",
                Style = "Industrial pha Scandinavian",
                Mood = "Ấm cúng, năng động vào buổi sáng",
                SeatCount = 60,
                Timeline = "8 tuần",
                BrandNote = "Tông màu chủ đạo nâu gỗ + xanh rêu, logo hình hạt cà phê.",
                BusinessModel = "Dine-in kết hợp take-away",
                BusinessGoals = "Tăng nhận diện thương hiệu, doanh thu 300tr/tháng",
                OperationNote = "Cao điểm 7-9h sáng và 18-20h tối",
                AiRecommendations =
                {
                    new AiRecommendation
                    {
                        ConceptSummary = "Bố trí quầy bar trung tâm, khu vực seating linh hoạt, " +
                                         "tận dụng ánh sáng tự nhiên từ mặt tiền kính.",
                        Payload = """{"zones":["bar","seating","takeaway"],"palette":["#6F4E37","#4B5320","#F5F0E6"],"lighting":"warm"}""",
                        EstimatedDesignCost = 45_000_000m,
                        EstimatedConstructionCost = 380_000_000m,
                    },
                },
            },
            BudgetItems =
            {
                new BudgetItem { Category = "Thiết kế", PlannedAmount = 45_000_000m, ActualAmount = 45_000_000m },
                new BudgetItem { Category = "Thi công nội thất", PlannedAmount = 300_000_000m, ActualAmount = 180_000_000m },
                new BudgetItem { Category = "Thiết bị & máy móc", PlannedAmount = 80_000_000m },
                new BudgetItem { Category = "Dự phòng", PlannedAmount = 25_000_000m },
            },
        };

        // Marketplace: bài đăng tìm designer + 2 đơn ứng tuyển.
        var post1 = new Post
        {
            ProjectShopOwner = project1,
            ServiceKind = ServiceKind.design,
            Title = "Tìm đơn vị thiết kế quán cà phê 85m2 Quận 1",
            Description = "Cần thiết kế concept + bản vẽ kỹ thuật cho quán cà phê phong cách industrial.",
            Status = PostStatus.closed,
            SubmissionDeadline = At(40),
        };

        var appDesigner = new Apply
        {
            Post = post1,
            ServiceProviderProfile = designerProvider,
            Proposal = "Chúng tôi đề xuất concept industrial-scandinavian với quầy bar trung tâm. " +
                       "Bao gồm 3 phương án 3D và bản vẽ kỹ thuật chi tiết.",
            EstimatedDurationDays = 21,
            Status = ApplicationStatus.accepted,
            SubmittedAt = At(45),
        };

        var appBoth = new Apply
        {
            Post = post1,
            ServiceProviderProfile = bothProvider,
            Proposal = "Báo giá thiết kế trọn gói, có thể kèm thi công.",
            EstimatedDurationDays = 30,
            Status = ApplicationStatus.rejected,
            SubmittedAt = At(44),
        };

        // ───────── ProjectWorking 1: thiết kế (qua marketplace) ─────────
        var ppDesign = new ProjectWorking
        {
            ProjectShopOwner = project1,
            ServiceProviderProfile = designerProvider,
            Apply = appDesigner,
            ContractType = ServiceKind.design,
            Status = ProviderStatus.completed,
            RequestMessage = "Chấp nhận đơn ứng tuyển của Studio Mộc Design.",
            StartedAt = At(40),
            Surveys =
            {
                new Survey
                {
                    Version = 1.0m,
                    ConditionNote = "Mặt bằng 85m2, trần cao 3.8m, mặt tiền kính 6m. Tường cũ cần xử lý chống ẩm.",
                    ReportUrl = "https://files.scb.com/surveys/p1-survey-v1.pdf",
                    CreatedByAccount = designerAcc,
                },
            },
            Designs =
            {
                new Design
                {
                    Title = "Concept Industrial - Phương án cuối",
                    Version = 2.0m,
                    Type = DesignType.render_3d,
                    Status = DesignStatus.approved,
                    Reason = "Đã chỉnh sửa theo góp ý của chủ quán về khu vực quầy bar.",
                    CreatedByAccount = designerAcc,
                    DesignImages =
                    {
                        new DesignImage
                        {
                            ImageUrl = "https://files.scb.com/designs/p1-3d-bar.jpg",
                            Caption = "Khu vực quầy bar trung tâm",
                            UploadedByAccount = designerAcc,
                        },
                        new DesignImage
                        {
                            ImageUrl = "https://files.scb.com/designs/p1-3d-seating.jpg",
                            Caption = "Khu vực seating bên cửa sổ",
                            UploadedByAccount = designerAcc,
                        },
                    },
                },
            },
            Contracts =
            {
                new Contract
                {
                    Title = "Hợp đồng thiết kế quán cà phê An's Coffee House",
                    PartyInfo = "Bên A: An's Coffee House — Bên B: Studio Mộc Design",
                    Terms = "Bàn giao 3 phương án 3D + bản vẽ kỹ thuật trong 21 ngày.",
                    AgreedValue = 45_000_000m,
                    DocumentUrl = "https://files.scb.com/contracts/p1-design.pdf",
                    Status = ContractStatus.confirmed,
                    ConfirmedAt = At(38),
                    ConfirmedByAccount = owner1Acc,
                },
            },
        };

        // ───────── ProjectWorking 2: thi công (thuê trực tiếp) ─────────
        var ppBuild = new ProjectWorking
        {
            ProjectShopOwner = project1,
            ServiceProviderProfile = constructorProvider,
            Apply = null, // thuê trực tiếp
            ContractType = ServiceKind.construction,
            Status = ProviderStatus.accepted, // đang thi công = accepted + contract confirmed (derived)
            RequestMessage = "Mời thi công theo bản vẽ đã được duyệt.",
            StartedAt = At(20),
            Contracts =
            {
                new Contract
                {
                    Title = "Hợp đồng thi công nội thất An's Coffee House",
                    PartyInfo = "Bên A: An's Coffee House — Bên B: Xây Dựng Tín Phát",
                    Terms = "Thi công trọn gói nội thất theo bản vẽ, hoàn thành trong 6 tuần.",
                    AgreedValue = 380_000_000m,
                    DocumentUrl = "https://files.scb.com/contracts/p1-build.pdf",
                    Status = ContractStatus.confirmed,
                    ConfirmedAt = At(19),
                    ConfirmedByAccount = owner1Acc,
                },
            },
        };

        // Hạng mục thi công (parent + children)
        var itemRough = new ConstructionItem
        {
            ProjectWorking = ppBuild,
            Name = "Phần thô",
            Description = "Xử lý tường, sàn, trần, điện nước âm.",
            Category = "Kết cấu",
            EstimateAt = DateOnly.FromDateTime(At(5)),
            ActualAt = DateOnly.FromDateTime(At(3)),
            Status = ItemStatus.completed,
            CreatedByAccount = constructorAcc,
        };
        var itemElectric = new ConstructionItem
        {
            ProjectWorking = ppBuild,
            Parent = itemRough,
            Name = "Hệ thống điện",
            Description = "Đi dây điện âm tường, lắp ổ cắm khu vực bar.",
            Category = "M&E",
            EstimateAt = DateOnly.FromDateTime(At(8)),
            ActualAt = DateOnly.FromDateTime(At(6)),
            Status = ItemStatus.completed,
            CreatedByAccount = constructorAcc,
        };
        var itemFurniture = new ConstructionItem
        {
            ProjectWorking = ppBuild,
            Name = "Đóng nội thất gỗ",
            Description = "Quầy bar, kệ trang trí, bàn ghế gỗ.",
            Category = "Nội thất",
            EstimateAt = DateOnly.FromDateTime(At(-7)), // dự kiến trong tương lai
            Status = ItemStatus.in_progress,
            CreatedByAccount = constructorAcc,
        };

        // Task nhỏ trong milestone (ảnh hiện trường)
        var taskWaterproof = new ConstructionTask
        {
            ConstructionItem = itemRough,
            Name = "Chống thấm tường mặt tiền",
            Description = "Xử lý 2 lớp chống thấm trước khi tô.",
            ImageUrl = "https://files.scb.com/tasks/p1-waterproof.jpg",
            EstimateAt = DateOnly.FromDateTime(At(5)),
            ActualAt = DateOnly.FromDateTime(At(4)),
            Status = ItemStatus.completed,
            CreatedByAccount = constructorAcc,
        };
        var taskWiring = new ConstructionTask
        {
            ConstructionItem = itemElectric,
            Name = "Đi dây điện âm tường khu bar",
            Description = "Kéo dây, đặt ống luồn, đấu ổ cắm.",
            ImageUrl = "https://files.scb.com/tasks/p1-wiring.jpg",
            EstimateAt = DateOnly.FromDateTime(At(7)),
            Status = ItemStatus.in_progress,
            CreatedByAccount = constructorAcc,
        };

        var issue1 = new Issue
        {
            ProjectWorking = ppBuild,
            ConstructionItem = itemRough,
            IssueType = issueTypes[2], // site_condition
            Cause = "Tường mặt tiền bị thấm nước mưa.",
            Reason = "Lớp chống thấm cũ đã xuống cấp.",
            Solution = "Cạo bỏ lớp cũ, xử lý chống thấm 2 lớp trước khi sơn.",
            IssueImage = "https://files.scb.com/issues/p1-wall-before.jpg",
            ConfirmImage = "https://files.scb.com/issues/p1-wall-after.jpg",
            EstimateAt = DateOnly.FromDateTime(At(4)),
            ActualAt = DateOnly.FromDateTime(At(2)),
            Status = IssueStatus.resolved,
            CreatedByAccount = constructorAcc,
        };

        var docQuote = new Doc
        {
            ProjectWorking = ppBuild,
            DocType = docTypes[1], // quotation
            FileUrl = "https://files.scb.com/docs/p1-quotation.pdf",
            FileName = "bao-gia-thi-cong.pdf",
            Caption = "Báo giá thi công chi tiết",
            UploadedByAccount = constructorAcc,
        };
        var docDrawing = new Doc
        {
            ProjectWorking = ppDesign,
            DocType = docTypes[2], // technical_drawing
            FileUrl = "https://files.scb.com/docs/p1-drawings.pdf",
            FileName = "ban-ve-ky-thuat.pdf",
            Caption = "Bản vẽ kỹ thuật đã duyệt",
            UploadedByAccount = designerAcc,
        };

        // Hội thoại + tin nhắn (trên ppDesign)
        var convo = new Conversation
        {
            ProjectWorking = ppDesign,
            Topic = "Trao đổi concept thiết kế",
            Messages =
            {
                new Message { Sender = owner1Acc,   Body = "Chào shop, mình muốn khu quầy bar nổi bật hơn nhé." },
                new Message { Sender = designerAcc, Body = "Dạ vâng, bên em sẽ điều chỉnh đưa quầy bar ra trung tâm và thêm đèn thả." },
                new Message { Sender = owner1Acc,   Body = "Tuyệt vời, vậy chốt phương án này nha." },
            },
        };

        // Đánh giá (trên ppDesign — đã completed)
        var review = new Review
        {
            ProjectWorking = ppDesign,
            OverallRating = 4.80m,
            Comment = "Thiết kế đẹp, đúng concept, phản hồi nhanh. Rất hài lòng!",
            ReviewScores =
            {
                new ReviewScore { Dimension = "Chất lượng", Score = 5 },
                new ReviewScore { Dimension = "Tiến độ", Score = 5 },
                new ReviewScore { Dimension = "Giao tiếp", Score = 4 },
            },
        };

        // ───────── Project 2 (mới brief, đơn giản) ─────────
        var project2 = new ProjectShopOwner
        {
            Owner = owner2,
            Name = "Bình Minh Cafe - Quán take-away",
            Address = "45 Lê Lợi, Quận 3, TP.HCM",
            AreaM2 = 30.00m,
            Budget = 120_000_000m,
            Status = ProjectStatus.briefed,
            DesignBrief = new DesignBrief
            {
                TargetCustomer = "Học sinh, sinh viên, dân văn phòng mua mang đi",
                Style = "Modern, Cozy",
                Mood = "Trẻ trung, tươi sáng",
                SeatCount = 12,
                Timeline = "4 tuần",
                BrandNote = "Tông pastel, nhấn màu cam.",
                BusinessModel = "Chủ yếu take-away",
            },
            BudgetItems =
            {
                new BudgetItem { Category = "Thiết kế", PlannedAmount = 20_000_000m },
                new BudgetItem { Category = "Thi công", PlannedAmount = 90_000_000m },
            },
            Posts =
            {
                new Post
                {
                    ServiceKind = ServiceKind.both,
                    Title = "Tìm đơn vị thiết kế & thi công quán take-away 30m2",
                    Description = "Cần đơn vị làm trọn gói thiết kế và thi công.",
                    Status = PostStatus.open,
                    SubmissionDeadline = At(-14),
                },
            },
        };

        // ───────── Notifications ─────────
        var notifications = new List<Notification>
        {
            new() { Account = owner1Acc,   Type = "contract_confirmed", Content = "Hợp đồng thiết kế đã được xác nhận.", IsRead = true },
            new() { Account = owner1Acc,   Type = "design_approved",    Content = "Bạn đã duyệt phương án thiết kế cuối cùng.", IsRead = false },
            new() { Account = designerAcc, Type = "application_accepted", Content = "Đơn ứng tuyển của bạn đã được chấp nhận.", IsRead = true },
            new() { Account = constructorAcc, Type = "project_assigned", Content = "Bạn được mời thi công dự án An's Coffee House.", IsRead = false },
            new() { Account = owner2Acc,   Type = "post_published",     Content = "Bài đăng tìm nhà thầu của bạn đã được đăng.", IsRead = false },
        };

        // ───────── Persist ─────────
        // Thêm các "gốc" — EF tự khám phá toàn bộ object graph qua navigation properties.
        db.IssueTypes.AddRange(issueTypes);
        db.DocTypes.AddRange(docTypes);
        db.Accounts.AddRange(adminAcc, owner1Acc, owner2Acc, designerAcc, constructorAcc, bothAcc);
        db.ServiceProviderProfiles.AddRange(designerProvider, constructorProvider, bothProvider);
        db.ProjectShopOwners.AddRange(project1, project2);
        db.ProjectWorkings.AddRange(ppDesign, ppBuild);
        db.ConstructionItems.AddRange(itemRough, itemElectric, itemFurniture);
        db.ConstructionTasks.AddRange(taskWaterproof, taskWiring);
        db.Issues.Add(issue1);
        db.Docs.AddRange(docQuote, docDrawing);
        db.Conversations.Add(convo);
        db.Reviews.Add(review);
        db.Notifications.AddRange(notifications);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Seed các gói phí nền tảng mặc định — idempotent, chỉ chạy khi bảng còn trống.</summary>
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
}
