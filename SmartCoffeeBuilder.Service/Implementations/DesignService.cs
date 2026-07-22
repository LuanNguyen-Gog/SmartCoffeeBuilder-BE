using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Design;
using SmartCoffeeBuilder.Service.DTOs.Responses.Design;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class DesignService : IDesignService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Design> _repository;
    private readonly IFileStorageService _fileStorage;

    public DesignService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Design>();
        _fileStorage = fileStorage;
    }

    public async Task<PaginationResponse<DesignResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectWorkingId = null, string? status = null, string? type = null)
    {
        DesignStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<DesignStatus>(status, ignoreCase: true, out var parsedStatus))
                throw new ArgumentException($"Status '{status}' không hợp lệ.");
            st = parsedStatus;
        }

        DesignType? tp = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<DesignType>(type, ignoreCase: true, out var parsedType))
                throw new ArgumentException($"Type '{type}' không hợp lệ.");
            tp = parsedType;
        }

        var query = _repository
            .GetQueryable(
                d => (projectWorkingId == null || d.ProjectWorkingId == projectWorkingId)
                     && (st == null || d.Status == st)
                     && (tp == null || d.Type == tp),
                include: q => q.Include(d => d.DesignImages))
            .OrderByDescending(d => d.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<DesignResponse>(
            paged.Items.Select(DesignResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<DesignResponse> GetByIdAsync(long id)
    {
        var design = await GetDesignAsync(id);
        return DesignResponse.From(design);
    }

    public async Task<DesignResponse> CreateAsync(CreateDesignRequest request)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ProjectWorkingId)
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {request.ProjectWorkingId}.");

        if (engagement.ContractType == ServiceKind.construction)
            throw new InvalidOperationException(
                "Engagement có contract type 'construction' — không có giai đoạn thiết kế.");

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"Engagement đang ở trạng thái '{engagement.Status}' — chỉ tạo design khi engagement 'accepted'.");

        // v5: "đã ký mới được làm" — guard qua contract confirmed, không check provider_status.
        var hasConfirmedContract = await _unitOfWork.GetRepository<Contract>()
            .CountAsync(c => c.ProjectWorkingId == engagement.Id && c.Status == ContractStatus.confirmed) > 0;
        if (!hasConfirmedContract)
            throw new InvalidOperationException(
                "Engagement chưa có contract 'confirmed' — ký hợp đồng trước khi tạo design.");

        if (!Enum.TryParse<DesignType>(request.Type, ignoreCase: true, out var type))
            throw new ArgumentException(
                $"Type '{request.Type}' không hợp lệ. Cho phép: concept, layout_2d, render_3d, technical_drawing.");

        if (request.CreatedBy != null)
        {
            _ = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: a => a.Id == request.CreatedBy)
                ?? throw new KeyNotFoundException($"Không tìm thấy account với id {request.CreatedBy}.");
        }

        var design = new Design
        {
            ProjectWorkingId = engagement.Id,
            Title = request.Title,
            Version = 0.1m, // bản nháp đầu tiên; mỗi vòng revision +0.1
            Type = type,
            Status = DesignStatus.in_progress,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(design);
        await _unitOfWork.CommitAsync();

        return DesignResponse.From(design);
    }

    public async Task<DesignResponse> UpdateAsync(long id, UpdateDesignRequest request)
    {
        var design = await GetDesignAsync(id);

        if (design.Status is not (DesignStatus.in_progress or DesignStatus.revision))
            throw new InvalidOperationException(
                $"Design đang ở trạng thái '{design.Status}' — chỉ chỉnh sửa khi 'in_progress' hoặc 'revision'.");

        if (request.Title != null) design.Title = request.Title;
        if (request.Type != null)
        {
            if (!Enum.TryParse<DesignType>(request.Type, ignoreCase: true, out var type))
                throw new ArgumentException(
                    $"Type '{request.Type}' không hợp lệ. Cho phép: concept, layout_2d, render_3d, technical_drawing.");
            design.Type = type;
        }
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        return DesignResponse.From(design);
    }

    /// <summary>Provider nộp bản design cho owner duyệt: in_progress → submitted.</summary>
    public async Task<DesignResponse> SubmitAsync(long id)
    {
        var design = await GetDesignAsync(id);

        if (design.Status != DesignStatus.in_progress)
            throw new InvalidOperationException(
                $"Chỉ submit được design đang 'in_progress' (hiện tại: '{design.Status}').");

        if (design.DesignImages.Count == 0)
            throw new InvalidOperationException("Design chưa có ảnh nào — thêm ảnh trước khi submit.");

        design.Status = DesignStatus.submitted;
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        return DesignResponse.From(design);
    }

    /// <summary>
    /// Owner duyệt bản design: submitted → approved.
    /// Pha design "xong" là derived từ design approved — không đổi provider_status.
    /// </summary>
    public async Task<DesignResponse> ApproveAsync(long id)
    {
        var design = await GetDesignAsync(id);

        if (design.Status != DesignStatus.submitted)
            throw new InvalidOperationException(
                $"Chỉ approve được design đang 'submitted' (hiện tại: '{design.Status}').");

        design.Status = DesignStatus.approved;
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        return DesignResponse.From(design);
    }

    /// <summary>Owner yêu cầu chỉnh sửa: submitted → revision (kèm lý do).</summary>
    public async Task<DesignResponse> RequestRevisionAsync(long id, RequestDesignRevisionRequest request)
    {
        var design = await GetDesignAsync(id);

        if (design.Status != DesignStatus.submitted)
            throw new InvalidOperationException(
                $"Chỉ yêu cầu revision được design đang 'submitted' (hiện tại: '{design.Status}').");

        design.Status = DesignStatus.revision;
        design.Reason = request.Reason;
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        return DesignResponse.From(design);
    }

    /// <summary>Provider bắt đầu sửa theo yêu cầu: revision → in_progress, version +0.1.</summary>
    public async Task<DesignResponse> StartRevisionAsync(long id)
    {
        var design = await GetDesignAsync(id);

        if (design.Status != DesignStatus.revision)
            throw new InvalidOperationException(
                $"Chỉ bắt đầu sửa được design đang 'revision' (hiện tại: '{design.Status}').");

        design.Status = DesignStatus.in_progress;
        design.Version += 0.1m;
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        return DesignResponse.From(design);
    }

    public async Task<DesignImageResponse> UploadFileAsync(
        long designId, Stream content, string fileName, string? contentType, long sizeBytes,
        string? caption = null, long? uploadedBy = null)
    {
        var design = await GetDesignAsync(designId);

        if (design.Status == DesignStatus.approved)
            throw new InvalidOperationException("Design đã được approve — không thêm file được nữa.");

        Account? uploader = null;
        if (uploadedBy != null)
        {
            uploader = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: a => a.Id == uploadedBy)
                ?? throw new KeyNotFoundException($"Không tìm thấy account với id {uploadedBy}.");
        }

        // Nhận cả ảnh render lẫn file bản vẽ (pdf/office). Lưu theo "{role}/{accountId}" của người
        // upload — cùng quy ước với api/files (controller luôn truyền account từ token nếu form
        // không có uploadedBy); "designs" chỉ là chốt chặn cho caller không xác định được người upload.
        var folderPath = uploader != null ? $"{uploader.Role}/{uploader.Id}" : "designs";
        var uploaded = await _fileStorage.UploadAsync(content, fileName, contentType, sizeBytes, folderPath);

        var image = new DesignImage
        {
            DesignId = design.Id,
            ImageUrl = uploaded.ObjectName,
            Caption = caption,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GetRepository<DesignImage>().InsertAsync(image);
        design.UpdatedAt = DateTime.UtcNow;
        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        // ViewUrl do DesignImageResponse.From resolve từ ObjectName — giống hệt uploaded.Url.
        return DesignImageResponse.From(image);
    }

    public async Task RemoveFileAsync(long designId, long imageId)
    {
        var design = await GetDesignAsync(designId);

        if (design.Status == DesignStatus.approved)
            throw new InvalidOperationException("Design đã được approve — không xóa file được nữa.");

        var image = design.DesignImages.FirstOrDefault(i => i.Id == imageId)
            ?? throw new KeyNotFoundException($"Không tìm thấy file với id {imageId} trong design {designId}.");

        _unitOfWork.GetRepository<DesignImage>().Delete(image);
        design.UpdatedAt = DateTime.UtcNow;
        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        // Dọn object trên bucket sau khi DB đã commit; object không còn cũng bỏ qua.
        try { await _fileStorage.DeleteAsync(image.ImageUrl); }
        catch (KeyNotFoundException) { }
    }

    private async Task<Design> GetDesignAsync(long id)
    {
        return await _repository.SingleOrDefaultAsync(
            predicate: d => d.Id == id,
            include: q => q.Include(d => d.DesignImages))
            ?? throw new KeyNotFoundException($"Không tìm thấy design với id {id}.");
    }
}
