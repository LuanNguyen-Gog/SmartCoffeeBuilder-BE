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

        // Seed gốc: chỉ chạy khi bảng accounts còn trống.
        if (!await db.Accounts.AnyAsync(ct))
            await SeedCoreAsync(db, ct);

        // Dữ liệu test bổ sung (designer/constructor/both 2-3 + project mẫu) — idempotent riêng
        // theo email đại diện, nên DB cũ đã seed vẫn nhận được.
        await SeedExtraTestDataAsync(db, ct);
    }

    private static async Task SeedCoreAsync(SmartCafeBuilderContext db, CancellationToken ct)
    {
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

        // ───────── Comment + DesignVersion: PHẢI seed sau SaveChanges ở trên ─────────
        // comments.target_id là FK MỀM (không có navigation để EF fix-up), nên chỉ gán được sau khi
        // entity cha đã có Id thật. Trước lần save này mọi .Id vẫn là 0.
        await SeedCommentsAndDesignVersionAsync(db, ppDesign, itemFurniture, owner1Acc, designerAcc, constructorAcc, At, ct);
    }

    /// <summary>
    /// Seed thread comment mẫu + 1 bản DesignVersion 'approved' cho design đầu tiên, để FE có dữ liệu
    /// hiển thị ngay khi gọi /api/comments và /api/designs/{id}/versions.
    /// Tách riêng vì phụ thuộc Id đã sinh — gọi SAU khi <see cref="SeedCoreAsync"/> save lần đầu.
    /// </summary>
    private static async Task SeedCommentsAndDesignVersionAsync(
        SmartCafeBuilderContext db,
        ProjectWorking ppDesign, ConstructionItem itemFurniture,
        Account owner1Acc, Account designerAcc, Account constructorAcc,
        Func<int, DateTime> At, CancellationToken ct)
    {
        // Thread trên milestone "Đóng nội thất gỗ" (itemFurniture): owner hỏi + constructor trả lời.
        var commentsMilestone = new List<Comment>
        {
            new()
            {
                TargetType = CommentTargetType.construction_item,
                TargetId = itemFurniture.Id,
                Body = "Anh ơi quầy bar có thể đổi vân gỗ sang tông sáng hơn không?",
                CreatedByAccount = owner1Acc,
                CreatedAt = At(3),
                UpdatedAt = At(3),
            },
            new()
            {
                TargetType = CommentTargetType.construction_item,
                TargetId = itemFurniture.Id,
                Body = "Dạ được, bên em sẽ chọn ván MDF phủ melamine tông oak nhạt, báo lại trong 1 ngày.",
                CreatedByAccount = constructorAcc,
                CreatedAt = At(2),
                UpdatedAt = At(2),
            },
            new()
            {
                TargetType = CommentTargetType.construction_item,
                TargetId = itemFurniture.Id,
                Body = "OK em, chốt vậy nhé.",
                CreatedByAccount = owner1Acc,
                CreatedAt = At(1),
                UpdatedAt = At(1),
            },
        };

        // Thread trên design (target = design). Lấy design đầu tiên trong ppDesign.
        var firstDesign = ppDesign.Designs.First();
        var commentsDesign = new List<Comment>
        {
            new()
            {
                TargetType = CommentTargetType.design,
                TargetId = firstDesign.Id,
                Body = "Mình thấy khu vực quầy bar nên làm nổi bật hơn, có thể đẩy ra trung tâm không?",
                CreatedByAccount = owner1Acc,
                CreatedAt = At(38),
                UpdatedAt = At(38),
            },
            new()
            {
                TargetType = CommentTargetType.design,
                TargetId = firstDesign.Id,
                Body = "Dạ vâng, bên em sẽ chỉnh phương án mới — quầy bar trung tâm + đèn thả.",
                CreatedByAccount = designerAcc,
                CreatedAt = At(37),
                UpdatedAt = At(37),
            },
        };

        // ───────── DesignVersion snapshot (seed bản 'approved' cho design đầu tiên) ─────────
        // Gán qua NAVIGATION (Design / OriginalImage) chứ không gán Id thô: EF tự fix-up FK,
        // nên khối này không vỡ nếu sau này ai đó đổi thứ tự save.
        var designVersionApproved = new DesignVersion
        {
            Design = firstDesign,
            SnapshotKind = DesignVersionSnapshotKind.approved,
            Version = firstDesign.Version,
            Title = firstDesign.Title,
            Type = firstDesign.Type,
            Status = firstDesign.Status,
            Reason = firstDesign.Reason,
            CreatedByAccount = designerAcc,
            SnapshottedByAccount = designerAcc,
            CreatedAt = firstDesign.CreatedAt,
            SnapshottedAt = At(20),
            // Copy ảnh từ design gốc — ObjectName copy nguyên trạng.
            Images = firstDesign.DesignImages.Select(img => new DesignVersionImage
            {
                OriginalImage = img,
                ImageUrl = img.ImageUrl,
                Caption = img.Caption,
                UploadedByAccount = img.UploadedByAccount,
                UploadedAt = img.CreatedAt,
            }).ToList(),
        };

        db.Comments.AddRange(commentsMilestone);
        db.Comments.AddRange(commentsDesign);
        db.DesignVersions.Add(designVersionApproved);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Seed thêm provider (designer/constructor/both 2-3) + owner 3 với các project mẫu đang mở post,
    /// phục vụ test marketplace/apply. Idempotent theo email đại diện "designer2@scb.com".
    /// </summary>
    private static async Task SeedExtraTestDataAsync(SmartCafeBuilderContext db, CancellationToken ct)
    {
        if (await db.Accounts.AnyAsync(a => a.Email == "designer2@scb.com", ct))
            return;

        var now = DateTime.UtcNow;
        DateTime At(int daysAgo) => DateTime.SpecifyKind(now.AddDays(-daysAgo), DateTimeKind.Utc);

        Account NewAccount(string email, string phone) => new()
        {
            Email = email,
            Phone = phone,
            PasswordHash = PasswordHash,
            Role = AccountRole.provider,
            Status = AccountStatus.active,
            EmailVerifiedAt = At(30),
        };

        // ───────── Designer 2 & 3 ─────────
        var designer2 = new ServiceProviderProfile
        {
            Account = NewAccount("designer2@scb.com", "0900000011"),
            DisplayName = "Kiến Trúc Sáng Tạo KAS",
            ProviderType = ProviderType.company,
            Capability = Capability.designer,
            Bio = "Studio thiết kế trẻ, mạnh về concept quán cà phê sân vườn và tropical.",
            CompanyTaxCode = "0311122233",
            YearsExperience = 5,
            PortfolioHeadline = "Chuyên cà phê sân vườn & tropical",
            IsVerified = true,
            AvgRating = 4.60m,
            DesignerProfile = new DesignerProfile
            {
                Specialties = "Cà phê sân vườn, tropical, ngoại thất",
                SoftwareSkills = "SketchUp, 3ds Max, V-Ray",
                DesignStyle = "Tropical, Rustic, Vintage",
                MinProjectBudget = 40_000_000m,
            },
        };
        var designer3 = new ServiceProviderProfile
        {
            Account = NewAccount("designer3@scb.com", "0900000012"),
            DisplayName = "Lê Minh Hoạ - Freelance Designer",
            ProviderType = ProviderType.individual,
            Capability = Capability.designer,
            Bio = "Designer tự do 4 năm kinh nghiệm, nhận dự án quán nhỏ và kiosk.",
            YearsExperience = 4,
            PortfolioHeadline = "Thiết kế nhanh gọn cho quán nhỏ, kiosk",
            IsVerified = false,
            AvgRating = 4.10m,
            DesignerProfile = new DesignerProfile
            {
                Specialties = "Kiosk, xe cà phê, quán dưới 40m2",
                SoftwareSkills = "SketchUp, Photoshop, Canva",
                DesignStyle = "Minimalist, Retro",
                MinProjectBudget = 15_000_000m,
            },
        };

        // ───────── Constructor 2 & 3 ─────────
        var constructor2 = new ServiceProviderProfile
        {
            Account = NewAccount("constructor2@scb.com", "0900000013"),
            DisplayName = "Nội Thất Hưng Thịnh",
            ProviderType = ProviderType.company,
            Capability = Capability.constructor,
            Bio = "Xưởng sản xuất kiêm thi công nội thất gỗ công nghiệp, có xưởng riêng tại Bình Dương.",
            CompanyTaxCode = "0322233445",
            YearsExperience = 9,
            PortfolioHeadline = "Xưởng gỗ riêng — giá tận gốc",
            IsVerified = true,
            AvgRating = 4.40m,
            ConstructorProfile = new ConstructorProfile
            {
                LicenseNo = "GPXD-2020-00456",
                TeamSize = 18,
                Equipment = "Dây chuyền gỗ công nghiệp, máy dán cạnh, xe tải vận chuyển",
                MaxProjectValue = 1_500_000_000m,
                WarrantyPolicy = "Bảo hành 18 tháng nội thất gỗ.",
            },
        };
        var constructor3 = new ServiceProviderProfile
        {
            Account = NewAccount("constructor3@scb.com", "0900000014"),
            DisplayName = "Đội Thi Công Anh Tuấn",
            ProviderType = ProviderType.individual,
            Capability = Capability.constructor,
            Bio = "Đội thợ đa năng nhận sửa chữa, cải tạo mặt bằng quán cà phê giá hợp lý.",
            YearsExperience = 7,
            PortfolioHeadline = "Cải tạo nhanh, nhận việc nhỏ lẻ",
            IsVerified = false,
            AvgRating = 3.90m,
            ConstructorProfile = new ConstructorProfile
            {
                LicenseNo = "GPXD-2022-01777",
                TeamSize = 6,
                Equipment = "Dụng cụ cầm tay, máy khoan bê tông, giàn giáo mini",
                MaxProjectValue = 300_000_000m,
                WarrantyPolicy = "Bảo hành 6 tháng.",
            },
        };

        // ───────── Both 2 & 3 ─────────
        var both2 = new ServiceProviderProfile
        {
            Account = NewAccount("both2@scb.com", "0900000015"),
            DisplayName = "F&B Design Build Group",
            ProviderType = ProviderType.company,
            Capability = Capability.both,
            Bio = "Tổng thầu design & build chuyên chuỗi F&B, đã làm cho 3 chuỗi cà phê lớn.",
            CompanyTaxCode = "0333344556",
            YearsExperience = 11,
            PortfolioHeadline = "Design & Build trọn gói cho chuỗi F&B",
            IsVerified = true,
            AvgRating = 4.85m,
            DesignerProfile = new DesignerProfile
            {
                Specialties = "Chuỗi cà phê, nhận diện thương hiệu không gian",
                SoftwareSkills = "AutoCAD, Revit, 3ds Max, Enscape",
                DesignStyle = "Modern, Industrial, Brand-driven",
                MinProjectBudget = 100_000_000m,
            },
            ConstructorProfile = new ConstructorProfile
            {
                LicenseNo = "GPXD-2018-00088",
                TeamSize = 40,
                Equipment = "Đội M&E riêng, xưởng gỗ, xưởng sắt, xe cẩu nhỏ",
                MaxProjectValue = 5_000_000_000m,
                WarrantyPolicy = "Bảo hành 24 tháng toàn bộ hạng mục.",
            },
        };
        var both3 = new ServiceProviderProfile
        {
            Account = NewAccount("both3@scb.com", "0900000016"),
            DisplayName = "Cafe Maker Studio",
            ProviderType = ProviderType.individual,
            Capability = Capability.both,
            Bio = "Hai anh em kiến trúc sư + kỹ sư, chuyên setup quán cà phê nhỏ trọn gói từ A-Z.",
            YearsExperience = 3,
            PortfolioHeadline = "Setup quán nhỏ trọn gói từ A-Z",
            IsVerified = false,
            AvgRating = 4.30m,
            DesignerProfile = new DesignerProfile
            {
                Specialties = "Quán nhỏ 20-50m2, take-away, container",
                SoftwareSkills = "SketchUp, Enscape, Illustrator",
                DesignStyle = "Cozy, Vintage, Hàn Quốc",
                MinProjectBudget = 20_000_000m,
            },
            ConstructorProfile = new ConstructorProfile
            {
                LicenseNo = "GPXD-2023-02456",
                TeamSize = 5,
                Equipment = "Dụng cụ cầm tay, máy cắt gỗ mini",
                MaxProjectValue = 250_000_000m,
                WarrantyPolicy = "Bảo hành 12 tháng.",
            },
        };

        // ───────── Owner 3 + các project mẫu đang mở post ─────────
        var owner3Acc = new Account
        {
            Email = "owner3@scb.com",
            Phone = "0900000017",
            PasswordHash = PasswordHash,
            Role = AccountRole.owner,
            Status = AccountStatus.active,
            EmailVerifiedAt = At(30),
        };
        var owner3 = new ShopOwner
        {
            Account = owner3Acc,
            FullName = "Phạm Quốc Cường",
            ShopName = "Cường Sài Gòn Coffee",
            Phone = "0900000017",
            Address = "88 Phan Xích Long, Phú Nhuận, TP.HCM",
        };

        // Project 3: cần thiết kế — post design đang mở.
        var project3 = new ProjectShopOwner
        {
            Owner = owner3,
            Name = "Cường Sài Gòn - Quán sân vườn Phú Nhuận",
            Address = "88 Phan Xích Long, Phú Nhuận, TP.HCM",
            AreaM2 = 150.00m,
            Budget = 800_000_000m,
            Status = ProjectStatus.briefed,
            DesignBrief = new DesignBrief
            {
                TargetCustomer = "Gia đình, nhóm bạn cuối tuần, khách chụp ảnh",
                Style = "Tropical sân vườn",
                Mood = "Thư giãn, nhiều cây xanh",
                SeatCount = 120,
                Timeline = "12 tuần",
                BrandNote = "Tông xanh lá + gỗ tự nhiên, có khu vực chụp ảnh check-in.",
                BusinessModel = "Dine-in, tổ chức workshop cuối tuần",
                BusinessGoals = "Trở thành điểm check-in nổi bật khu Phan Xích Long",
                OperationNote = "Đông nhất tối thứ 6 - chủ nhật",
            },
            BudgetItems =
            {
                new BudgetItem { Category = "Thiết kế", PlannedAmount = 80_000_000m },
                new BudgetItem { Category = "Thi công & cảnh quan", PlannedAmount = 600_000_000m },
                new BudgetItem { Category = "Dự phòng", PlannedAmount = 120_000_000m },
            },
            Posts =
            {
                new Post
                {
                    ServiceKind = ServiceKind.design,
                    Title = "Tìm designer cho quán cà phê sân vườn 150m2",
                    Description = "Cần concept tropical sân vườn nhiều cây xanh, có khu check-in. " +
                                  "Ưu tiên đơn vị từng làm quán sân vườn.",
                    Status = PostStatus.open,
                    SubmissionDeadline = At(-21),
                },
            },
        };

        // Project 4: đã có bản vẽ — post construction đang mở.
        var project4 = new ProjectShopOwner
        {
            Owner = owner3,
            Name = "Cường Sài Gòn - Chi nhánh 2 Gò Vấp",
            Address = "215 Quang Trung, Gò Vấp, TP.HCM",
            AreaM2 = 60.00m,
            Budget = 350_000_000m,
            Status = ProjectStatus.briefed,
            DesignBrief = new DesignBrief
            {
                TargetCustomer = "Sinh viên, dân văn phòng khu Gò Vấp",
                Style = "Industrial",
                Mood = "Năng động, trẻ trung",
                SeatCount = 45,
                Timeline = "6 tuần",
                BrandNote = "Đồng bộ nhận diện với chi nhánh 1.",
                BusinessModel = "Dine-in kết hợp take-away",
            },
            BudgetItems =
            {
                new BudgetItem { Category = "Thi công", PlannedAmount = 300_000_000m },
                new BudgetItem { Category = "Dự phòng", PlannedAmount = 50_000_000m },
            },
            Posts =
            {
                new Post
                {
                    ServiceKind = ServiceKind.construction,
                    Title = "Tìm nhà thầu thi công quán 60m2 theo bản vẽ có sẵn",
                    Description = "Đã có đầy đủ bản vẽ kỹ thuật, cần nhà thầu thi công trọn gói trong 6 tuần.",
                    Status = PostStatus.open,
                    SubmissionDeadline = At(-14),
                },
            },
        };

        // Project 5: muốn trọn gói — post both đang mở.
        var project5 = new ProjectShopOwner
        {
            Owner = owner3,
            Name = "Cường Sài Gòn - Kiosk container Thủ Đức",
            Address = "Khu công nghệ cao, TP. Thủ Đức, TP.HCM",
            AreaM2 = 25.00m,
            Budget = 180_000_000m,
            Status = ProjectStatus.briefed,
            DesignBrief = new DesignBrief
            {
                TargetCustomer = "Kỹ sư, nhân viên khu công nghệ cao mua mang đi",
                Style = "Container hiện đại",
                Mood = "Nhanh gọn, bắt mắt",
                SeatCount = 10,
                Timeline = "5 tuần",
                BrandNote = "Container sơn màu cam nổi bật, logo lớn hai mặt.",
                BusinessModel = "Take-away là chính",
            },
            BudgetItems =
            {
                new BudgetItem { Category = "Thiết kế + thi công trọn gói", PlannedAmount = 160_000_000m },
                new BudgetItem { Category = "Dự phòng", PlannedAmount = 20_000_000m },
            },
            Posts =
            {
                new Post
                {
                    ServiceKind = ServiceKind.both,
                    Title = "Tìm đơn vị design & build kiosk container 25m2",
                    Description = "Cần đơn vị làm trọn gói từ thiết kế đến thi công kiosk container, bàn giao chìa khoá trao tay.",
                    Status = PostStatus.open,
                    SubmissionDeadline = At(-30),
                },
            },
        };

        db.ServiceProviderProfiles.AddRange(designer2, designer3, constructor2, constructor3, both2, both3);
        db.ProjectShopOwners.AddRange(project3, project4, project5);
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
