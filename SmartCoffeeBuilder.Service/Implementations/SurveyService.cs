using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Survey;
using SmartCoffeeBuilder.Service.DTOs.Responses.Survey;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class SurveyService : ISurveyService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Survey> _repository;

    public SurveyService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Survey>();
    }

    public async Task<PaginationResponse<SurveyResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10, long? projectProviderId = null)
    {
        var query = _repository
            .GetQueryable(s => projectProviderId == null || s.ProjectProviderId == projectProviderId)
            .OrderByDescending(s => s.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<SurveyResponse>(
            paged.Items.Select(SurveyResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<SurveyResponse> GetByIdAsync(long id)
    {
        var survey = await _repository.SingleOrDefaultAsync(predicate: s => s.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy survey với id {id}.");

        return SurveyResponse.From(survey);
    }

    public async Task<SurveyResponse> CreateAsync(CreateSurveyRequest request)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectProvider>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ProjectProviderId)
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {request.ProjectProviderId}.");

        if (engagement.ContractType == ServiceKind.construction)
            throw new InvalidOperationException(
                "Engagement có contract type 'construction' — không có giai đoạn khảo sát/thiết kế.");

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"Engagement đang ở trạng thái '{engagement.Status}' — chỉ tạo survey khi engagement 'accepted'.");

        // v5 (cập nhật): survey ĐỘC LẬP với contract — khảo sát được phép làm TRƯỚC khi ký.
        // Chỉ cần engagement 'accepted' + contract_type có pha design (đã check ở trên).
        // KHÔNG guard contract 'confirmed' ở đây (khác design/construction_item vẫn yêu cầu đã ký).

        if (request.CreatedBy != null)
        {
            _ = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: a => a.Id == request.CreatedBy)
                ?? throw new KeyNotFoundException($"Không tìm thấy account với id {request.CreatedBy}.");
        }

        // Version tự tăng 0.1 theo từng engagement (0.1, 0.2, …).
        var maxVersion = await _repository
            .GetQueryable(s => s.ProjectProviderId == engagement.Id)
            .Select(s => (decimal?)s.Version)
            .MaxAsync() ?? 0m;

        var survey = new Survey
        {
            ProjectProviderId = engagement.Id,
            Version = maxVersion + 0.1m,
            ConditionNote = request.ConditionNote,
            ReportUrl = request.ReportUrl,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(survey);
        await _unitOfWork.CommitAsync();

        return SurveyResponse.From(survey);
    }

    public async Task<SurveyResponse> UpdateAsync(long id, UpdateSurveyRequest request)
    {
        var survey = await _repository.SingleOrDefaultAsync(predicate: s => s.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy survey với id {id}.");

        if (request.ConditionNote != null) survey.ConditionNote = request.ConditionNote;
        if (request.ReportUrl != null) survey.ReportUrl = request.ReportUrl;
        survey.UpdatedAt = DateTime.UtcNow;

        _repository.Update(survey);
        await _unitOfWork.CommitAsync();

        return SurveyResponse.From(survey);
    }
}
