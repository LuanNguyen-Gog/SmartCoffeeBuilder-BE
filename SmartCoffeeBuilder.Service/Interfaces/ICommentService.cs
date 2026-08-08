using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Comment;
using SmartCoffeeBuilder.Service.DTOs.Responses.Comment;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Thread comment public neo vào <see cref="ConstructionItem"/> hoặc <see cref="Design"/> (FK mềm).
/// Read: cả owner và provider liên quan tới ProjectWorking của target đều xem được.
/// Write: chỉ những account thuộc ProjectWorking (owner/provider/admin) mới post được.
/// </summary>
public interface ICommentService
{
    /// <summary>Danh sách comment theo target (FK mềm), sắp xếp mới nhất trước.</summary>
    Task<PaginationResponse<CommentResponse>> GetAllAsync(
        CommentTargetType targetType, long targetId,
        int pageNumber = 1, int pageSize = 20);

    /// <summary>
    /// Tạo comment. <paramref name="request"/>.CreatedBy sẽ được service ghi đè bằng
    /// <paramref name="currentAccountId"/> nếu caller không truyền — controller lấy từ JWT.
    /// Quyền: account phải thuộc ProjectWorking của target hoặc là admin.
    /// </summary>
    Task<CommentResponse> CreateAsync(CreateCommentRequest request, long currentAccountId);

    /// <summary>Xoá comment — chỉ người tạo hoặc admin.</summary>
    Task DeleteAsync(long id, long currentAccountId);
}