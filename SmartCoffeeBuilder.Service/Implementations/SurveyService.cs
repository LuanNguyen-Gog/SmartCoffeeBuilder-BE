using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Survey;
using SmartCoffeeBuilder.Service.DTOs.Responses.Survey;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Khảo sát mặt bằng, hai chỗ neo (review 3):
/// <list type="bullet">
/// <item><b>Theo hồ sơ ứng tuyển</b> — provider khảo sát TRƯỚC khi được chọn. Chủ quán so khảo sát
/// + báo giá của nhiều provider rồi mới quyết định, nên ở luồng này KHÔNG đòi engagement
/// 'accepted'. Đây là thay đổi so với v5.</item>
/// <item><b>Theo engagement</b> — khảo sát trong lúc đã hợp tác (luồng cũ, giữ nguyên luật:
/// engagement 'accepted' + contract_type có pha thiết kế).</item>
/// </list>
/// Giá ước tính đi kèm nằm ở <c>Quotation</c> (cũng neo được vào Apply), không nhân đôi ở đây.
/// </summary>
public class SurveyService : ISurveyService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Survey> _repository;
    private readonly IFileStorageService _fileStorage;

    public SurveyService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Survey>();
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Danh sách khảo sát trong TẦM NHÌN của người gọi: chủ dự án thấy mọi bản khảo sát trên dự án
    /// của mình, provider thấy bản của chính mình, admin thấy tất cả.
    ///
    /// <paramref name="postId"/> là bộ lọc phục vụ đúng nghiệp vụ review 3 — chủ quán xem khảo sát
    /// của mọi provider đã ứng tuyển một bài đăng cạnh nhau rồi mới chọn. Không có nó thì owner
    /// phải lấy danh sách hồ sơ rồi gọi lần lượt từng <c>applyId</c>. Cố ý đặt tên và hành vi
    /// giống <c>QuotationService.GetAllAsync</c>: khảo sát và báo giá là hai nửa của cùng một
    /// quyết định, hai bên lệch tham số thì màn so sánh phải ghép bằng hai kiểu query khác nhau.
    /// </summary>
    public async Task<PaginationResponse<SurveyResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, Guid? applyId = null, Guid? postId = null)
    {
        var isAdmin = await IsAdminAsync(accountId);

        // Lọc quyền NGAY TRONG query chứ không lấy về rồi ẩn — phân trang mới đếm đúng theo góc
        // nhìn người gọi. Hai nhánh neo kiểm riêng vì mỗi survey chỉ có đúng một nhánh khác null.
        var query = _repository
            .GetQueryable(
                s => (projectWorkingId == null || s.ProjectWorkingId == projectWorkingId)
                     && (applyId == null || s.ApplyId == applyId)
                     && (postId == null || (s.Apply != null && s.Apply.PostId == postId))
                     && (isAdmin
                         || (s.Apply != null
                             && (s.Apply.Post.ProjectShopOwner.Owner.AccountId == accountId
                                 || s.Apply.ServiceProviderProfile.AccountId == accountId))
                         || (s.ProjectWorking != null
                             && (s.ProjectWorking.ProjectShopOwner.Owner.AccountId == accountId
                                 || s.ProjectWorking.ServiceProviderProfile.AccountId == accountId))))
            .OrderByDescending(s => s.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<SurveyResponse>(
            paged.Items.Select(SurveyResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    /// <summary>
    /// Chi tiết một bản khảo sát. Cùng luật tầm nhìn với <see cref="GetAllAsync"/>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Không tồn tại (HTTP 404).</exception>
    /// <exception cref="UnauthorizedAccessException">Khảo sát của dự án khác (HTTP 401).</exception>
    public async Task<SurveyResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        // Không include gì: SurveyResponse.From chỉ đọc cột phẳng, còn kiểm quyền đi bằng query
        // đếm riêng — nạp cả graph project → owner ở đây là join thừa cho mọi lần gọi.
        var survey = await _repository.SingleOrDefaultAsync(predicate: s => s.Id == id)
            ?? throw new KeyNotFoundException($"No survey found with id {id}.");

        await EnsureSurveyVisibleAsync(accountId, survey);
        return SurveyResponse.From(survey);
    }

    /// <summary>
    /// Ai được ĐỌC một bản khảo sát: chủ dự án mang bản đó, provider đứng tên bản đó, admin.
    ///
    /// Hẹp hơn quyền đọc <c>site_profiles</c>/brief (những thứ mở cho mọi provider khi bài đăng còn
    /// 'open') là CỐ Ý: khảo sát là công sức riêng và là quân bài cạnh tranh của từng provider —
    /// để provider B đọc được bản của provider A thì họ chép số đo rồi báo giá đè lên mà không
    /// phải đi đo.
    /// </summary>
    private async Task EnsureSurveyVisibleAsync(Guid accountId, Survey survey)
    {
        // Một query đếm, tự đi theo đúng nhánh neo mà survey đang dùng — tránh nạp cả graph chỉ để
        // so hai cái account id.
        var visible = await _repository.CountAsync(
            s => s.Id == survey.Id
                 && ((s.Apply != null
                      && (s.Apply.Post.ProjectShopOwner.Owner.AccountId == accountId
                          || s.Apply.ServiceProviderProfile.AccountId == accountId))
                     || (s.ProjectWorking != null
                         && (s.ProjectWorking.ProjectShopOwner.Owner.AccountId == accountId
                             || s.ProjectWorking.ServiceProviderProfile.AccountId == accountId)))) > 0;

        if (visible || await IsAdminAsync(accountId)) return;

        throw new UnauthorizedAccessException(
            "This survey belongs to a project that the signed-in account is not part of.");
    }

    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    public async Task<SurveyResponse> CreateAsync(Guid accountId, CreateSurveyRequest request)
    {
        if ((request.ApplyId == null) == (request.ProjectWorkingId == null))
            throw new ArgumentException(
                "Send EXACTLY ONE of: applyId (survey at application time) or " +
                "projectWorkingId (survey once the engagement is under way).");

        var survey = new Survey
        {
            ProjectWorkingId = request.ProjectWorkingId,
            ApplyId = request.ApplyId,
            ScheduledAt = request.ScheduledAt,
            SurveyedAt = request.SurveyedAt,
            ConditionNote = request.ConditionNote ?? string.Empty,
            // File báo cáo phải upload qua api/files trước; giá trị gửi lên rút về ObjectName.
            ReportUrl = await _fileStorage.NormalizeForStorageAsync(request.ReportUrl, "reportUrl"),
            // Người tạo lấy từ TOKEN, không nhận từ body: client tự khai thì cột created_by
            // mất giá trị đối chứng (xem quy tắc Authorization trong CLAUDE.md).
            CreatedBy = accountId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (request.ApplyId is Guid applyId)
            await EnsureCanSurveyApplyAsync(accountId, applyId);
        else
            await EnsureCanSurveyEngagementAsync(accountId, request.ProjectWorkingId!.Value);

        await _repository.InsertAsync(survey);
        await _unitOfWork.CommitAsync();

        return SurveyResponse.From(survey);
    }

    /// <summary>
    /// Luồng ứng tuyển: chỉ chính provider đứng tên hồ sơ mới khảo sát được, và chỉ khi hồ sơ còn
    /// 'pending' — hồ sơ đã bị từ chối / đã được chọn thì khảo sát thêm không còn ý nghĩa so sánh.
    /// KHÔNG kiểm tra engagement: cả điểm của review 3 là khảo sát có TRƯỚC khi được chọn.
    /// </summary>
    private async Task EnsureCanSurveyApplyAsync(Guid accountId, Guid applyId)
    {
        var apply = await _unitOfWork.GetRepository<Apply>()
            .SingleOrDefaultAsync(
                predicate: a => a.Id == applyId,
                include: q => q.Include(a => a.ServiceProviderProfile).Include(a => a.Post))
            ?? throw new KeyNotFoundException($"No application found with id {applyId}.");

        if (apply.ServiceProviderProfile.AccountId != accountId)
            throw new UnauthorizedAccessException(
                "Only the provider named on this application may create a survey.");

        if (apply.Status != ApplicationStatus.pending)
            throw new InvalidOperationException(
                $"The application is in status '{apply.Status}' — a survey can only be created while the application is 'pending'.");

        if (apply.Post.Status != PostStatus.open)
            throw new InvalidOperationException(
                $"The post is in status '{apply.Post.Status}' — it is not accepting further surveys.");
    }

    /// <summary>Luồng đã hợp tác — giữ nguyên luật v5.</summary>
    private async Task EnsureCanSurveyEngagementAsync(Guid accountId, Guid projectWorkingId)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == projectWorkingId)
            ?? throw new KeyNotFoundException($"No project provider found with id {projectWorkingId}.");

        // Quyền TRƯỚC mọi check trạng thái — role gate 'provider' không phân biệt được provider NÀO,
        // thiếu chỗ này thì provider bất kỳ tạo được khảo sát trên engagement của người khác.
        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, engagement.Id),
            "create a survey", EngagementActor.Provider);

        if (engagement.ContractType == ServiceKind.construction)
            throw new InvalidOperationException(
                "This engagement has contract type 'construction' — it has no survey or design phase.");

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"The engagement is in status '{engagement.Status}' — a survey can only be created while the engagement is 'accepted'.");

        // v5 (cập nhật): survey ĐỘC LẬP với contract — khảo sát được phép làm TRƯỚC khi ký.
        // KHÔNG guard contract 'confirmed' ở đây (khác design/construction_item vẫn yêu cầu đã ký).
    }

    public async Task<SurveyResponse> UpdateAsync(Guid accountId, Guid id, UpdateSurveyRequest request)
    {
        var survey = await _repository.SingleOrDefaultAsync(
            predicate: s => s.Id == id,
            include: q => q.Include(s => s.Apply!).ThenInclude(a => a.ServiceProviderProfile))
            ?? throw new KeyNotFoundException($"No survey found with id {id}.");

        if (survey.ApplyId != null)
        {
            if (survey.Apply!.ServiceProviderProfile.AccountId != accountId)
                throw new UnauthorizedAccessException(
                    "Only the provider named on this application may edit the survey.");
        }
        else
        {
            EngagementAuthorization.EnsureActor(
                await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, survey.ProjectWorkingId!.Value),
                "edit a survey", EngagementActor.Provider);
        }

        if (request.ConditionNote != null) survey.ConditionNote = request.ConditionNote;
        if (request.ScheduledAt.HasValue) survey.ScheduledAt = request.ScheduledAt;
        if (request.SurveyedAt.HasValue) survey.SurveyedAt = request.SurveyedAt;

        // File cũ bị thay thì dọn luôn object trên bucket (sau khi DB commit) để khỏi rác.
        string? replacedReport = null;
        if (request.ReportUrl != null)
        {
            var newReport = await _fileStorage.NormalizeForStorageAsync(request.ReportUrl, "reportUrl");
            if (newReport != survey.ReportUrl) replacedReport = survey.ReportUrl;
            survey.ReportUrl = newReport;
        }

        survey.UpdatedAt = DateTime.UtcNow;

        _repository.Update(survey);
        await _unitOfWork.CommitAsync();

        await _fileStorage.TryDeleteAsync(replacedReport);

        return SurveyResponse.From(survey);
    }
}
