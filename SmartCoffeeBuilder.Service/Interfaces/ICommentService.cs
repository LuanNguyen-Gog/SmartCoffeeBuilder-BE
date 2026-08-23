using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Comment;
using SmartCoffeeBuilder.Service.DTOs.Responses.Comment;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Thread comment neo bằng FK mềm vào <see cref="ConstructionItem"/>, <see cref="Design"/> hoặc
/// <see cref="Quotation"/> (review 3: owner trao đổi với provider ngay trên từng bản báo giá).
/// Write: chỉ hai bên của chỗ neo (owner/provider) hoặc admin.
/// Read: thread trong một engagement để mở; riêng thread BÁO GIÁ bị khoá theo hai bên — xem
/// <see cref="GetAllAsync"/>.
/// </summary>
public interface ICommentService
{
    /// <summary>
    /// Danh sách comment theo target (FK mềm), sắp xếp mới nhất trước.
    ///
    /// Với <c>targetType = quotation</c>, chỉ hai bên của chỗ neo (hoặc admin) đọc được: nhiều
    /// provider cùng nộp báo giá vào một bài đăng, để mở thì đối thủ đọc được cả cuộc mặc cả giá.
    /// Hai target còn lại giữ nguyên hành vi cũ (chỉ cần đăng nhập).
    /// </summary>
    Task<PaginationResponse<CommentResponse>> GetAllAsync(
        CommentTargetType targetType, Guid targetId, Guid currentAccountId,
        int pageNumber = 1, int pageSize = 20);

    /// <summary>
    /// Tạo comment. <paramref name="request"/>.CreatedBy sẽ được service ghi đè bằng
    /// <paramref name="currentAccountId"/> nếu caller không truyền — controller lấy từ JWT.
    /// Quyền: account phải là một trong hai bên của chỗ neo, hoặc admin.
    /// </summary>
    Task<CommentResponse> CreateAsync(CreateCommentRequest request, Guid currentAccountId);

    /// <summary>Xoá comment — chỉ người tạo hoặc admin.</summary>
    Task DeleteAsync(Guid id, Guid currentAccountId);
}