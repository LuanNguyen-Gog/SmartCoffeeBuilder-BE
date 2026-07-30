namespace SmartCoffeeBuilder.Service.DTOs.Responses.Admin;

/// <summary>Thống kê tài khoản cho admin (không tính tài khoản đã xoá mềm).</summary>
public class AccountStatisticsResponse
{
    public int Total { get; set; }

    // Theo vai trò
    public int Owners { get; set; }
    public int Providers { get; set; }
    public int Admins { get; set; }

    // Theo trạng thái
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int Banned { get; set; }
    public int Pending { get; set; }

    public int EmailVerified { get; set; }
    /// <summary>Số tài khoản tạo mới từ đầu tháng hiện tại (UTC).</summary>
    public int NewThisMonth { get; set; }
}
