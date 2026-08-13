using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Dọn bài đăng sau khi một chỗ của dự án vừa bị lấp (xem <see cref="ProjectSlotRules"/>).
/// Bài đăng đụng vào chỗ đã có người giữ thì không tuyển được ai nữa — mọi hồ sơ nộp vào sẽ bị
/// <c>EnsureSlotFree</c> chặn — nên đóng bài luôn thay vì để nó nằm 'open' đánh lừa provider.
/// </summary>
public static class ProjectSlotClosure
{
    /// <summary>
    /// Đóng các bài đăng còn 'open' của dự án có phạm vi đụng <paramref name="occupiedKind"/>,
    /// và từ chối mọi hồ sơ còn 'pending' của chúng.
    /// KHÔNG commit — caller gộp chung một SaveChanges để nguyên khối thao tác là atomic.
    /// </summary>
    /// <param name="excludePostId">
    /// Bài đăng caller đã tự xử lý trong graph của nó — bỏ qua để không nạp instance thứ hai
    /// cho cùng một dòng (change tracker sẽ hỏng).
    /// </param>
    /// <returns>Id các hồ sơ vừa bị từ chối — caller bắn noti SAU khi commit.</returns>
    public static async Task<List<long>> CloseCoveredPostsAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        long projectShopOwnerId,
        ServiceKind occupiedKind,
        long? excludePostId = null)
    {
        var postRepository = unitOfWork.GetRepository<Post>();
        var openPosts = await postRepository.GetListAsync(
            predicate: p => p.ProjectShopOwnerId == projectShopOwnerId
                            && p.Status == PostStatus.open
                            && (excludePostId == null || p.Id != excludePostId));

        // Overlaps là luật nghiệp vụ viết bằng C#, không dịch được sang SQL — lọc sau khi nạp.
        var coveredPosts = openPosts
            .Where(p => ProjectSlotRules.Overlaps(p.ServiceKind, occupiedKind))
            .ToList();
        if (coveredPosts.Count == 0) return [];

        var now = DateTime.UtcNow;
        foreach (var post in coveredPosts)
        {
            post.Status = PostStatus.closed;
            post.UpdatedAt = now;
        }
        postRepository.UpdateRange(coveredPosts);

        var postIds = coveredPosts.Select(p => p.Id).ToList();
        var applyRepository = unitOfWork.GetRepository<Apply>();
        var pendingApplications = await applyRepository.GetListAsync(
            predicate: a => postIds.Contains(a.PostId) && a.Status == ApplicationStatus.pending);
        if (pendingApplications.Count == 0) return [];

        foreach (var application in pendingApplications)
        {
            application.Status = ApplicationStatus.rejected;
            application.UpdatedAt = now;
        }
        applyRepository.UpdateRange(pendingApplications);

        return pendingApplications.Select(a => a.Id).ToList();
    }
}
