using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Biến một khoản phát sinh HAI BÊN ĐÃ DUYỆT thành một đợt thanh toán thật.
///
/// Vì sao cần: <c>change_orders</c> ghi nhận sự ĐỒNG Ý về một khoản tiền, còn
/// <c>payment_batches</c> mới là đường owner thực sự trả và provider xác nhận đã nhận. Thiếu cầu
/// nối này thì một khoản duyệt xong chỉ làm tổng công nợ (<c>ChangeOrderSummary.TotalCommitted</c>)
/// tăng lên, mà không đợt nào đòi được — hai màn hình tiền trong cùng một dự án nói hai chuyện
/// khác nhau và không bên nào sai.
///
/// Gọi từ hai chỗ, đúng hai chỗ khoản phát sinh chuyển sang 'accepted':
/// <c>ChangeOrderService.RespondAsync</c> (bên kia bấm đồng ý) và
/// <c>DesignService.RequestRevisionAsync</c> (báo giá đã công bố đơn giá sửa ⇒ chốt luôn).
/// </summary>
public static class ChangeOrderBilling
{
    /// <summary>
    /// Sinh đợt thanh toán cho <paramref name="order"/> nếu đủ điều kiện. CHỈ ghi vào change
    /// tracker — caller commit chung transaction với hành động đã duyệt khoản, để không có đường
    /// chạy nào duyệt được khoản mà đợt tiền commit hụt.
    /// </summary>
    /// <returns>Đợt vừa dựng, hoặc null khi không sinh (xem các nhánh bên dưới).</returns>
    public static async Task<PaymentBatch?> TryCreateBatchAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, ChangeOrder order)
    {
        if (order.Status != ChangeOrderStatus.accepted) return null;

        // 0 đồng là ghi nhận một thay đổi phạm vi mà hai bên thống nhất không tính tiền — không có
        // gì để thu, và SubmitProofAsync cũng chặn minh chứng <= 0.
        if (order.Amount <= 0m) return null;

        // Đợt thanh toán treo vào HỢP ĐỒNG. Chưa ký thì chưa có chỗ ghi nợ: khoản vẫn 'accepted',
        // vẫn nằm trong tổng công nợ, chỉ chưa ra được đợt thu — và
        // ChangeOrderSummary.UnbilledAmount nói đúng phần chênh đó ra cho cả hai bên thấy.
        var contractId = (await unitOfWork.GetRepository<Contract>().GetListAsync(
                selector: c => c.Id,
                predicate: c => c.ProjectWorkingId == order.ProjectWorkingId
                                && c.Status == ContractStatus.confirmed))
            .FirstOrDefault();

        if (contractId == Guid.Empty) return null;

        var batches = unitOfWork.GetRepository<PaymentBatch>();

        // Chạy lại luồng không được đẻ đợt trùng — cùng nguyên tắc với
        // ContractService.GeneratePaymentBatchesAsync. Khoản chưa commit thì Id còn rỗng, chưa
        // thể có đợt nào trỏ vào nó, nên bỏ qua bước soi.
        if (order.Id != Guid.Empty
            && await batches.CountAsync(b => b.ChangeOrderId == order.Id) > 0) return null;

        var existingSort = await batches.GetListAsync(
            selector: b => b.SortOrder, predicate: b => b.ContractId == contractId);

        var now = DateTime.UtcNow;
        var batch = new PaymentBatch
        {
            ContractId = contractId,

            // Phát sinh gắn hạng mục nào thì đợt tiền gắn theo, để cờ is_paid của hạng mục đó chạy
            // đúng khi provider xác nhận đã nhận tiền.
            ConstructionItemId = order.ConstructionItemId,

            // Gán NAVIGATION chứ không gán FK: khoá chính do Postgres sinh (gen_random_uuid()) nên
            // một khoản vừa InsertAsync còn Id rỗng. Đi qua navigation thì EF tự xếp thứ tự insert
            // và điền khoá ngoại sau, dùng được cho cả khoản mới lẫn khoản đã nằm trong DB.
            ChangeOrder = order,

            // Phát sinh luôn nằm SAU mọi đợt của báo giá gốc: nó là cam kết sinh sau.
            SortOrder = existingSort.Count == 0 ? 1 : existingSort.Max() + 1,
            Name = order.Title,
            Amount = order.Amount,
            Note = $"Approved change order. Reason: {order.Reason}",
            Status = PaymentBatchStatus.pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await batches.InsertAsync(batch);
        return batch;
    }
}
