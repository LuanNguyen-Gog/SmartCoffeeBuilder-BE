using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.DTOs.Requests.SiteProfile;
using SmartCoffeeBuilder.Service.DTOs.Responses.SiteProfile;
using SmartCoffeeBuilder.Service.Interfaces;
using Entities = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Thông số vật lý mặt bằng: kích thước, hướng, số tầng, cửa và ban công (review 1.1).
///
/// Quyền GHI mở cho CẢ HAI bên — khác <c>DesignBriefService</c> (chỉ owner ghi). Lý do: brief là ý
/// muốn của chủ quán, còn đây là SỐ ĐO THẬT, mà người cầm thước là provider lúc đi khảo sát. Bắt
/// owner nhập hộ số đo provider đọc cho thì vừa sai vừa chậm.
///
/// Quyền ĐỌC theo đúng luật của brief: chủ dự án, provider đang có engagement còn hiệu lực, và mọi
/// provider khi dự án còn bài đăng 'open' (không đọc được mặt bằng thì không báo giá nổi).
/// </summary>
public class SiteProfileService : ISiteProfileService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Entities.SiteProfile> _repository;

    public SiteProfileService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Entities.SiteProfile>();
    }

    public async Task<SiteProfileResponse> GetByProjectAsync(Guid accountId, Guid projectShopOwnerId)
    {
        await EnsureProjectVisibleAsync(accountId, projectShopOwnerId);

        var profile = await LoadGraphAsync(p => p.ProjectShopOwnerId == projectShopOwnerId)
            ?? throw new KeyNotFoundException(
                $"Dự án {projectShopOwnerId} chưa khai hồ sơ thông số mặt bằng.");

        return SiteProfileResponse.From(profile);
    }

    public async Task<SiteProfileResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var profile = await LoadGraphAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ mặt bằng với id {id}.");

        await EnsureProjectVisibleAsync(accountId, profile.ProjectShopOwnerId);
        return SiteProfileResponse.From(profile);
    }

    public async Task<SiteProfileResponse> CreateAsync(Guid accountId, CreateSiteProfileRequest request)
    {
        // Quyền TRƯỚC check trùng — cùng lý do đã ghi ở DesignBriefService.CreateAsync: check trùng
        // chạy trước thì người ngoài phân biệt được dự án nào đã khai (409) / chưa khai (401).
        await EnsureCanWriteAsync(accountId, request.ProjectShopOwnerId, "khai hồ sơ mặt bằng cho dự án này");

        if (await _repository.CountAsync(p => p.ProjectShopOwnerId == request.ProjectShopOwnerId) > 0)
            throw new InvalidOperationException(
                $"Dự án {request.ProjectShopOwnerId} đã có hồ sơ mặt bằng — sửa bản cũ thay vì tạo bản thứ hai.");

        EnsureDimensionsValid(request.LengthM, request.WidthM, request.FrontageWidthM,
            request.CeilingHeightM, request.RoadWidthM);
        if (request.FloorCount is int fc && fc <= 0)
            throw new ArgumentException("FloorCount phải lớn hơn 0 — mặt bằng luôn có ít nhất tầng trệt.");

        var now = DateTime.UtcNow;
        var profile = new Entities.SiteProfile
        {
            ProjectShopOwnerId = request.ProjectShopOwnerId,
            LengthM = request.LengthM,
            WidthM = request.WidthM,
            FrontageWidthM = request.FrontageWidthM,
            CeilingHeightM = request.CeilingHeightM,
            RoadWidthM = request.RoadWidthM,
            Orientation = ParseOrientation(request.Orientation),
            FloorCount = request.FloorCount,
            HasMezzanine = request.HasMezzanine,
            StructureNote = request.StructureNote,
            ExistingConditionNote = request.ExistingConditionNote,
            CreatedBy = accountId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.InsertAsync(profile);

        // Id là uuid do Postgres sinh nên chỉ có giá trị SAU commit — phải commit trước rồi mới
        // gắn tầng / ô cửa (xem ghi chú ở SmartCafeBuilderContext).
        await _unitOfWork.CommitAsync();

        if (request.Floors is { Count: > 0 })
        {
            var seen = new HashSet<int>();
            foreach (var f in request.Floors)
            {
                if (!seen.Add(f.FloorNo))
                    throw new ArgumentException($"Tầng số {f.FloorNo} bị khai hai lần trong cùng một yêu cầu.");
                await _unitOfWork.GetRepository<SiteFloor>().InsertAsync(BuildFloor(profile.Id, f, now));
            }
            await _unitOfWork.CommitAsync();
        }

        if (request.Openings is { Count: > 0 })
        {
            var floors = await _unitOfWork.GetRepository<SiteFloor>()
                .GetListAsync(predicate: f => f.SiteProfileId == profile.Id);

            var order = 0;
            foreach (var o in request.Openings)
            {
                var floorId = ResolveFloorId(o, floors);
                await _unitOfWork.GetRepository<SiteOpening>()
                    .InsertAsync(BuildOpening(profile.Id, floorId, o, o.SortOrder ?? order++, now));
            }
            await _unitOfWork.CommitAsync();
        }

        return await GetByIdAsync(accountId, profile.Id);
    }

    public async Task<SiteProfileResponse> UpdateAsync(Guid accountId, Guid id, UpdateSiteProfileRequest request)
    {
        var profile = await _repository.SingleOrDefaultAsync(predicate: p => p.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ mặt bằng với id {id}.");

        await EnsureCanWriteAsync(accountId, profile.ProjectShopOwnerId, "sửa hồ sơ mặt bằng này");

        EnsureDimensionsValid(request.LengthM, request.WidthM, request.FrontageWidthM,
            request.CeilingHeightM, request.RoadWidthM);

        if (request.LengthM.HasValue) profile.LengthM = request.LengthM;
        if (request.WidthM.HasValue) profile.WidthM = request.WidthM;
        if (request.FrontageWidthM.HasValue) profile.FrontageWidthM = request.FrontageWidthM;
        if (request.CeilingHeightM.HasValue) profile.CeilingHeightM = request.CeilingHeightM;
        if (request.RoadWidthM.HasValue) profile.RoadWidthM = request.RoadWidthM;
        if (request.Orientation != null) profile.Orientation = ParseOrientation(request.Orientation);
        if (request.FloorCount.HasValue)
        {
            if (request.FloorCount.Value <= 0)
                throw new ArgumentException("FloorCount phải lớn hơn 0 — mặt bằng luôn có ít nhất tầng trệt.");
            profile.FloorCount = request.FloorCount;
        }
        if (request.HasMezzanine.HasValue) profile.HasMezzanine = request.HasMezzanine.Value;
        if (request.StructureNote != null) profile.StructureNote = request.StructureNote;
        if (request.ExistingConditionNote != null) profile.ExistingConditionNote = request.ExistingConditionNote;

        profile.UpdatedAt = DateTime.UtcNow;
        _repository.Update(profile);
        await _unitOfWork.CommitAsync();

        return await GetByIdAsync(accountId, profile.Id);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var profile = await _repository.SingleOrDefaultAsync(predicate: p => p.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ mặt bằng với id {id}.");

        await EnsureCanWriteAsync(accountId, profile.ProjectShopOwnerId, "xoá hồ sơ mặt bằng này");

        // Tầng và ô cửa cascade theo FK ở DB — không cần xoá tay.
        _repository.Delete(profile);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Owner duyệt số đo đã khảo sát và đồng bộ sang <c>projects.area_m2</c>.
    ///
    /// Đây là chỗ DUY NHẤT con số dự án được cập nhật từ số đo thật. Lý do phải có một bước duyệt
    /// tường minh thay vì đồng bộ tự động mỗi lần provider sửa tầng: <c>projects.area_m2</c> là con
    /// số payload AI đọc và là con số hiện trên mọi màn hình dự án — để provider đổi thẳng thì chủ
    /// quán mất quyền kiểm soát thông số dự án của chính mình, mà bỏ đồng bộ thì số đo thật nằm mãi
    /// trong <c>site_floors</c> còn AI thì chạy trên con số khai lúc đăng ký.
    ///
    /// KHÔNG có cột "đã duyệt" trong DB: trạng thái đó suy được bằng cách so
    /// <c>projects.area_m2</c> với tổng diện tích các tầng (xem <c>IsAreaSyncedToProject</c>). Thêm
    /// cột chỉ để lưu một thứ tính lại được là tạo cơ hội cho hai nguồn lệch nhau — và tránh được
    /// một migration đụng vào <c>ModelSnapshot</c> mà nhánh khác đang sửa.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Hồ sơ không tồn tại (HTTP 404).</exception>
    /// <exception cref="UnauthorizedAccessException">Người gọi không phải chủ dự án (HTTP 401).</exception>
    /// <exception cref="InvalidOperationException">Chưa tầng nào khai diện tích (HTTP 409).</exception>
    public async Task<SiteProfileResponse> ApproveMeasurementsAsync(Guid accountId, Guid id)
    {
        var profile = await LoadGraphAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ mặt bằng với id {id}.");

        // CHỈ chủ dự án — hẹp hơn EnsureCanWriteAsync một bậc. Provider ghi được số đo (họ cầm
        // thước) nhưng không tự duyệt số của chính mình vào thông số dự án.
        await EnsureProjectOwnerAsync(accountId, profile.ProjectShopOwnerId);

        var areas = (profile.Floors ?? new List<SiteFloor>())
            .Where(f => f.AreaM2.HasValue)
            .Select(f => f.AreaM2!.Value)
            .ToList();

        if (areas.Count == 0)
            throw new InvalidOperationException(
                "Chưa tầng nào khai diện tích — không có số đo để duyệt. " +
                "Điền diện tích cho ít nhất một tầng trước khi đồng bộ sang dự án.");

        var project = await _unitOfWork.GetRepository<ProjectShopOwner>()
            .SingleOrDefaultAsync(predicate: p => p.Id == profile.ProjectShopOwnerId && p.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Không tìm thấy dự án {profile.ProjectShopOwnerId}.");

        // Dự án đã đóng thì thông số chốt luôn — khớp guard của ProjectShopOwnerService.UpdateAsync,
        // không thì đây thành đường vòng sửa được dự án đã completed/cancelled.
        if (project.Status is ProjectStatus.completed or ProjectStatus.cancelled)
            throw new InvalidOperationException(
                $"Dự án đang ở trạng thái '{project.Status}' — không cập nhật thông số nữa.");

        project.AreaM2 = decimal.Round(areas.Sum(), 2);
        project.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.GetRepository<ProjectShopOwner>().Update(project);
        await _unitOfWork.CommitAsync();

        // Nạp lại để ProjectAreaM2 / IsAreaSyncedToProject phản ánh giá trị vừa ghi.
        return await GetByIdAsync(accountId, id);
    }

    // ───────────────────────── Tầng ─────────────────────────

    public async Task<SiteFloorResponse> AddFloorAsync(Guid accountId, Guid siteProfileId, SiteFloorRequest request)
    {
        var profile = await LoadForWriteAsync(accountId, siteProfileId, "thêm tầng cho mặt bằng này");

        var duplicated = await _unitOfWork.GetRepository<SiteFloor>()
            .CountAsync(f => f.SiteProfileId == profile.Id && f.FloorNo == request.FloorNo) > 0;
        if (duplicated)
            throw new InvalidOperationException(
                $"Mặt bằng này đã khai tầng số {request.FloorNo} — sửa dòng cũ thay vì thêm trùng.");

        var floor = BuildFloor(profile.Id, request, DateTime.UtcNow);
        await _unitOfWork.GetRepository<SiteFloor>().InsertAsync(floor);
        await _unitOfWork.CommitAsync();

        return SiteFloorResponse.From(floor);
    }

    public async Task<SiteFloorResponse> UpdateFloorAsync(Guid accountId, Guid floorId, SiteFloorRequest request)
    {
        var repo = _unitOfWork.GetRepository<SiteFloor>();
        var floor = await repo.SingleOrDefaultAsync(predicate: f => f.Id == floorId)
            ?? throw new KeyNotFoundException($"Không tìm thấy tầng với id {floorId}.");

        await LoadForWriteAsync(accountId, floor.SiteProfileId, "sửa tầng của mặt bằng này");
        EnsureFloorValid(request);

        if (floor.FloorNo != request.FloorNo)
        {
            var duplicated = await repo.CountAsync(
                f => f.SiteProfileId == floor.SiteProfileId && f.FloorNo == request.FloorNo && f.Id != floor.Id) > 0;
            if (duplicated)
                throw new InvalidOperationException($"Mặt bằng này đã có tầng số {request.FloorNo}.");
            floor.FloorNo = request.FloorNo;
        }

        floor.Name = request.Name;
        floor.AreaM2 = request.AreaM2;
        floor.CeilingHeightM = request.CeilingHeightM;
        floor.Purpose = request.Purpose;
        floor.Note = request.Note;
        floor.UpdatedAt = DateTime.UtcNow;

        repo.Update(floor);
        await _unitOfWork.CommitAsync();

        return SiteFloorResponse.From(floor);
    }

    public async Task RemoveFloorAsync(Guid accountId, Guid floorId)
    {
        var repo = _unitOfWork.GetRepository<SiteFloor>();
        var floor = await repo.SingleOrDefaultAsync(predicate: f => f.Id == floorId)
            ?? throw new KeyNotFoundException($"Không tìm thấy tầng với id {floorId}.");

        await LoadForWriteAsync(accountId, floor.SiteProfileId, "xoá tầng của mặt bằng này");

        // Ô cửa gắn vào tầng này KHÔNG bị xoá — FK là SET NULL, chúng vẫn thuộc mặt bằng.
        repo.Delete(floor);
        await _unitOfWork.CommitAsync();
    }

    // ───────────────────────── Cửa / ban công ─────────────────────────

    public async Task<SiteOpeningResponse> AddOpeningAsync(
        Guid accountId, Guid siteProfileId, SiteOpeningRequest request)
    {
        var profile = await LoadForWriteAsync(accountId, siteProfileId, "thêm cửa/ban công cho mặt bằng này");

        var floors = await _unitOfWork.GetRepository<SiteFloor>()
            .GetListAsync(predicate: f => f.SiteProfileId == profile.Id);
        var floorId = ResolveFloorId(request, floors);

        var existing = await _unitOfWork.GetRepository<SiteOpening>()
            .GetListAsync(selector: o => o.SortOrder, predicate: o => o.SiteProfileId == profile.Id);

        var opening = BuildOpening(profile.Id, floorId, request,
            request.SortOrder ?? (existing.Count == 0 ? 0 : existing.Max() + 1), DateTime.UtcNow);

        await _unitOfWork.GetRepository<SiteOpening>().InsertAsync(opening);
        await _unitOfWork.CommitAsync();

        return SiteOpeningResponse.From(opening);
    }

    public async Task<SiteOpeningResponse> UpdateOpeningAsync(
        Guid accountId, Guid openingId, SiteOpeningRequest request)
    {
        var repo = _unitOfWork.GetRepository<SiteOpening>();
        var opening = await repo.SingleOrDefaultAsync(predicate: o => o.Id == openingId)
            ?? throw new KeyNotFoundException($"Không tìm thấy cửa/ban công với id {openingId}.");

        await LoadForWriteAsync(accountId, opening.SiteProfileId, "sửa cửa/ban công của mặt bằng này");
        EnsureOpeningValid(request);

        var floors = await _unitOfWork.GetRepository<SiteFloor>()
            .GetListAsync(predicate: f => f.SiteProfileId == opening.SiteProfileId);

        opening.Type = ParseOpeningType(request.Type);
        opening.SiteFloorId = ResolveFloorId(request, floors);
        opening.Orientation = ParseOrientation(request.Orientation);
        opening.WidthM = request.WidthM;
        opening.HeightM = request.HeightM;
        opening.Quantity = request.Quantity;
        opening.Note = request.Note;
        if (request.SortOrder.HasValue) opening.SortOrder = request.SortOrder.Value;
        opening.UpdatedAt = DateTime.UtcNow;

        repo.Update(opening);
        await _unitOfWork.CommitAsync();

        return SiteOpeningResponse.From(opening);
    }

    public async Task RemoveOpeningAsync(Guid accountId, Guid openingId)
    {
        var repo = _unitOfWork.GetRepository<SiteOpening>();
        var opening = await repo.SingleOrDefaultAsync(predicate: o => o.Id == openingId)
            ?? throw new KeyNotFoundException($"Không tìm thấy cửa/ban công với id {openingId}.");

        await LoadForWriteAsync(accountId, opening.SiteProfileId, "xoá cửa/ban công của mặt bằng này");

        repo.Delete(opening);
        await _unitOfWork.CommitAsync();
    }

    // ───────────────────────── Helper ─────────────────────────

    // ProjectShopOwner nạp kèm để SiteProfileResponse điền được ProjectAreaM2 / IsAreaSyncedToProject
    // — một join theo FK, rẻ hơn nhiều so với bắt FE gọi thêm GET /api/projects/{id} chỉ để lấy
    // một con số rồi tự so sánh (và tự so thì mỗi client lại làm quy tắc làm tròn một kiểu).
    private Task<Entities.SiteProfile?> LoadGraphAsync(
        System.Linq.Expressions.Expression<Func<Entities.SiteProfile, bool>> predicate) =>
        _repository.SingleOrDefaultAsync(
            predicate: predicate,
            include: q => q.Include(p => p.Floors)
                           .Include(p => p.Openings)
                           .Include(p => p.ProjectShopOwner));

    private async Task<Entities.SiteProfile> LoadForWriteAsync(Guid accountId, Guid siteProfileId, string action)
    {
        var profile = await _repository.SingleOrDefaultAsync(predicate: p => p.Id == siteProfileId)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ mặt bằng với id {siteProfileId}.");

        await EnsureCanWriteAsync(accountId, profile.ProjectShopOwnerId, action);
        return profile;
    }

    /// <summary>
    /// Ai được ĐỌC hồ sơ mặt bằng — khớp <c>DesignBriefService.EnsureProjectVisibleAsync</c>:
    /// chủ dự án, provider có engagement còn hiệu lực, mọi provider khi dự án còn bài đăng 'open',
    /// và admin.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Không liên quan tới dự án (HTTP 401).</exception>
    private async Task EnsureProjectVisibleAsync(Guid accountId, Guid projectShopOwnerId)
    {
        var visible = await _unitOfWork.GetRepository<ProjectShopOwner>().CountAsync(
            p => p.Id == projectShopOwnerId
                 && p.DeletedAt == null
                 && (p.Owner.AccountId == accountId
                     || p.ProjectWorkings.Any(e => e.ServiceProviderProfile.AccountId == accountId
                                                   && e.Status != ProviderStatus.rejected
                                                   && e.Status != ProviderStatus.terminated)
                     || p.Posts.Any(post => post.Status == PostStatus.open))) > 0;

        if (visible || await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException(
            "Hồ sơ mặt bằng này thuộc một dự án mà tài khoản đang đăng nhập không tham gia.");
    }

    /// <summary>
    /// Ai được GHI: chủ dự án, hoặc provider đã được nhận việc trên dự án (engagement 'accepted').
    /// Hẹp hơn quyền đọc một bậc — provider mới ứng tuyển chỉ được XEM số đo, không sửa được,
    /// nếu không thì bất kỳ ai nộp hồ sơ cũng đổi được thông số mặt bằng của người khác.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Không có quyền ghi (HTTP 401).</exception>
    private async Task EnsureCanWriteAsync(Guid accountId, Guid projectShopOwnerId, string action)
    {
        var allowed = await _unitOfWork.GetRepository<ProjectShopOwner>().CountAsync(
            p => p.Id == projectShopOwnerId
                 && p.DeletedAt == null
                 && (p.Owner.AccountId == accountId
                     || p.ProjectWorkings.Any(e => e.ServiceProviderProfile.AccountId == accountId
                                                   && e.Status == ProviderStatus.accepted))) > 0;

        if (allowed || await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException(
            $"Chỉ chủ dự án hoặc nhà cung cấp đang thực hiện dự án mới được {action}.");
    }

    /// <summary>
    /// CHỈ chủ dự án (hoặc admin). Hẹp hơn <see cref="EnsureCanWriteAsync"/> một bậc: provider đang
    /// thực hiện dự án ghi được số đo nhưng KHÔNG tự duyệt số của mình vào thông số dự án.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Không phải chủ dự án (HTTP 401).</exception>
    private async Task EnsureProjectOwnerAsync(Guid accountId, Guid projectShopOwnerId)
    {
        var isOwner = await _unitOfWork.GetRepository<ProjectShopOwner>().CountAsync(
            p => p.Id == projectShopOwnerId
                 && p.DeletedAt == null
                 && p.Owner.AccountId == accountId) > 0;

        if (isOwner || await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException(
            "Chỉ chủ dự án mới được duyệt số đo khảo sát vào thông số dự án.");
    }

    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    private static SiteFloor BuildFloor(Guid siteProfileId, SiteFloorRequest r, DateTime now)
    {
        EnsureFloorValid(r);
        return new SiteFloor
        {
            SiteProfileId = siteProfileId,
            FloorNo = r.FloorNo,
            Name = r.Name,
            AreaM2 = r.AreaM2,
            CeilingHeightM = r.CeilingHeightM,
            Purpose = r.Purpose,
            Note = r.Note,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static SiteOpening BuildOpening(
        Guid siteProfileId, Guid? floorId, SiteOpeningRequest r, int sortOrder, DateTime now)
    {
        EnsureOpeningValid(r);
        return new SiteOpening
        {
            SiteProfileId = siteProfileId,
            SiteFloorId = floorId,
            Type = ParseOpeningType(r.Type),
            Orientation = ParseOrientation(r.Orientation),
            WidthM = r.WidthM,
            HeightM = r.HeightM,
            Quantity = r.Quantity,
            Note = r.Note,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Ô cửa chỉ trỏ được về tầng THUỘC CHÍNH mặt bằng này. <c>SiteFloorId</c> ưu tiên hơn
    /// <c>FloorNo</c> (FloorNo tồn tại để tạo một lượt cả tầng lẫn cửa, khi id chưa có).
    /// </summary>
    private static Guid? ResolveFloorId(SiteOpeningRequest r, ICollection<SiteFloor> floors)
    {
        if (r.SiteFloorId is Guid id)
            return floors.Any(f => f.Id == id)
                ? id
                : throw new ArgumentException($"Tầng {id} không thuộc mặt bằng này.");

        if (r.FloorNo is int no)
            return floors.FirstOrDefault(f => f.FloorNo == no)?.Id
                ?? throw new ArgumentException($"Mặt bằng này chưa khai tầng số {no}.");

        return null;
    }

    private static void EnsureFloorValid(SiteFloorRequest r)
    {
        if (r.AreaM2 is decimal a && a <= 0)
            throw new ArgumentException($"Diện tích tầng {r.FloorNo} phải lớn hơn 0.");
        if (r.CeilingHeightM is decimal h && h <= 0)
            throw new ArgumentException($"Chiều cao tầng {r.FloorNo} phải lớn hơn 0.");
    }

    private static void EnsureOpeningValid(SiteOpeningRequest r)
    {
        if (r.Quantity <= 0)
            throw new ArgumentException("Quantity của một ô cửa phải lớn hơn 0.");
        if (r.WidthM is decimal w && w <= 0)
            throw new ArgumentException("Chiều rộng ô cửa phải lớn hơn 0.");
        if (r.HeightM is decimal h && h <= 0)
            throw new ArgumentException("Chiều cao ô cửa phải lớn hơn 0.");
    }

    /// <summary>Mọi số đo đều là chiều dài vật lý — âm hoặc 0 là dữ liệu sai, không phải "chưa biết".</summary>
    private static void EnsureDimensionsValid(params decimal?[] values)
    {
        foreach (var v in values)
            if (v is decimal d && d <= 0)
                throw new ArgumentException("Các số đo mặt bằng phải lớn hơn 0 — bỏ trống nếu chưa đo.");
    }

    private static Orientation? ParseOrientation(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (Enum.TryParse<Orientation>(raw.Trim(), ignoreCase: true, out var parsed)) return parsed;

        throw new ArgumentException(
            $"Hướng '{raw}' không hợp lệ. Nhận: {string.Join(", ", Enum.GetNames<Orientation>())}.");
    }

    private static SiteOpeningType ParseOpeningType(string raw)
    {
        if (Enum.TryParse<SiteOpeningType>((raw ?? string.Empty).Trim(), ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException(
            $"Loại ô mở '{raw}' không hợp lệ. Nhận: {string.Join(", ", Enum.GetNames<SiteOpeningType>())}.");
    }
}
