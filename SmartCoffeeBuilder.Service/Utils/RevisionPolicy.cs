using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Cam kết về số lần sửa thiết kế của một engagement (review 1.1: "quy định số lần sửa và phí sửa").
/// </summary>
/// <param name="QuotationId">Báo giá đang chi phối engagement. null = chưa có báo giá nào được chốt.</param>
/// <param name="FreeRevisionCount">Số vòng sửa miễn phí. null = không cam kết con số nào ⇒ KHÔNG giới hạn.</param>
/// <param name="ExtraRevisionFee">Phí mỗi vòng vượt hạn mức. null = provider chưa công bố giá.</param>
public sealed record RevisionTerms(Guid? QuotationId, int? FreeRevisionCount, decimal? ExtraRevisionFee)
{
    /// <summary>Không có báo giá nào chốt số lần sửa ⇒ không gate được vòng nào.</summary>
    public bool IsUnlimited => FreeRevisionCount is null;

    /// <summary>Vòng sửa thứ <paramref name="revisionNo"/> có vượt hạn mức miễn phí không.</summary>
    public bool Exceeds(int revisionNo) => FreeRevisionCount is int free && revisionNo > free;
}

/// <summary>
/// Tra cam kết số lần sửa của một engagement.
///
/// Nguồn ưu tiên là báo giá gắn trên HỢP ĐỒNG ĐÃ KÝ (<c>contracts.quotation_id</c> với status
/// confirmed) — đó mới là bản hai bên thực sự ràng buộc nhau. Chỉ khi engagement chưa ký hợp đồng
/// (hoặc hợp đồng lập tay không gắn báo giá) mới rơi về bản báo giá 'accepted' neo vào engagement
/// hay vào hồ sơ ứng tuyển đã sinh ra nó.
/// </summary>
public static class RevisionPolicy
{
    public static async Task<RevisionTerms> ResolveAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid projectWorkingId)
    {
        // 1) Báo giá gắn trên hợp đồng đã ký.
        var fromContract = (await unitOfWork.GetRepository<Contract>().GetListAsync(
                selector: c => c.QuotationId,
                predicate: c => c.ProjectWorkingId == projectWorkingId
                                && c.Status == ContractStatus.confirmed
                                && c.QuotationId != null))
            .FirstOrDefault();

        if (fromContract is Guid quotationId)
        {
            var q = (await unitOfWork.GetRepository<Quotation>().GetListAsync(
                    selector: x => new { x.Id, x.FreeRevisionCount, x.ExtraRevisionFee },
                    predicate: x => x.Id == quotationId))
                .FirstOrDefault();

            if (q != null) return new RevisionTerms(q.Id, q.FreeRevisionCount, q.ExtraRevisionFee);
        }

        // 2) Chưa ký hợp đồng: lấy bản 'accepted' neo vào engagement, hoặc vào hồ sơ ứng tuyển
        //    đã sinh ra engagement này (báo giá marketplace neo vào application_id, không phải
        //    project_provider_id — xem CHECK ck_quotations_anchor).
        var applyId = (await unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
                selector: e => e.ApplyId,
                predicate: e => e.Id == projectWorkingId))
            .FirstOrDefault();

        var accepted = (await unitOfWork.GetRepository<Quotation>().GetListAsync(
                selector: x => new { x.Id, x.FreeRevisionCount, x.ExtraRevisionFee },
                predicate: x => x.Status == QuotationStatus.accepted
                                && (x.ProjectWorkingId == projectWorkingId
                                    || (applyId != null && x.ApplyId == applyId))))
            .FirstOrDefault();

        return accepted is null
            ? new RevisionTerms(null, null, null)
            : new RevisionTerms(accepted.Id, accepted.FreeRevisionCount, accepted.ExtraRevisionFee);
    }

    /// <summary>
    /// Tổng số vòng sửa owner đã đòi trên TOÀN engagement — cộng <c>designs.revision_count</c> của
    /// mọi bản vẽ thuộc hợp tác.
    ///
    /// Phải đếm ở phạm vi này vì hạn mức miễn phí nằm trên BÁO GIÁ, mà báo giá phủ cả hợp tác chứ
    /// không phủ riêng một bản vẽ. Đếm theo từng bản vẽ thì mỗi lần provider mở bản vẽ mới là hạn
    /// mức tự nạp lại, và điều khoản "N lần sửa miễn phí" không còn nghĩa gì: không bên nào đoán
    /// được mình đã mua bao nhiêu vòng sửa.
    /// </summary>
    public static async Task<int> CountUsedAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid projectWorkingId)
    {
        var perDesign = await unitOfWork.GetRepository<Design>().GetListAsync(
            selector: d => d.RevisionCount,
            predicate: d => d.ProjectWorkingId == projectWorkingId);

        return perDesign.Sum();
    }
}
