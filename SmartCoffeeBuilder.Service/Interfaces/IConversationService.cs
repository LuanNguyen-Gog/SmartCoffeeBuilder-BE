using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Chat;
using SmartCoffeeBuilder.Service.DTOs.Responses.Chat;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Quản lý thread (Conversation) trong một engagement (<c>ProjectWorking</c>).
/// Phân quyền: cả owner của ProjectShopOwner và provider của ServiceProviderProfile
/// đều là "member" của engagement — được phép list/get/create/update thread. Xoá thread
/// chỉ cho creator để tránh xoá nhầm thread người khác đã tạo.
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// List thread trong engagement — sort theo hoạt động (UpdatedAt DESC).
    /// Mỗi item kèm <c>LastMessage</c> (truy vấn batch) để FE hiển thị preview.
    /// </summary>
    Task<PaginationResponse<ConversationSummary>> GetByEngagementAsync(
        Guid accountId, Guid projectWorkingId, int pageNumber = 1, int pageSize = 20);

    /// <summary>Chi tiết một thread + danh sách message phân trang (SentAt ASC).</summary>
    Task<ConversationDetailResponse> GetByIdAsync(
        Guid accountId, Guid conversationId, int pageNumber = 1, int pageSize = 50);

    /// <summary>Tạo thread. Topic rỗng/khoảng trắng → service sinh "Thread #N" trong engagement.</summary>
    Task<ConversationSummary> CreateAsync(Guid accountId, CreateConversationRequest request);

    /// <summary>Đổi tên thread — cả owner và provider đều được sửa (đặt tên chung cho cả 2).</summary>
    Task<ConversationSummary> UpdateAsync(Guid accountId, Guid conversationId, UpdateConversationRequest request);

    /// <summary>Xoá thread — chỉ creator. Cascade xoá luôn message và attachment.</summary>
    Task DeleteAsync(Guid accountId, Guid conversationId);
}
