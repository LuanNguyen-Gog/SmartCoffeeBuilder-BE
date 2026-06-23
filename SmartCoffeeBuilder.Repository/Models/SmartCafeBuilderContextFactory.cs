using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Design-time factory cho dotnet-ef (vì appsettings.json bị gitignore).
/// Đọc connection string từ env ConnectionStrings__DefaultConnection, fallback localhost
/// (qua Cloud SQL Auth Proxy → trỏ tới Cloud SQL khi proxy đang chạy).
/// </summary>
public class SmartCafeBuilderContextFactory : IDesignTimeDbContextFactory<SmartCafeBuilderContext>
{
    public SmartCafeBuilderContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=SmartCafeBuilder;Username=postgres;Password=Sep#12345678";

        var options = new DbContextOptionsBuilder<SmartCafeBuilderContext>()
            .UseNpgsql(conn)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SmartCafeBuilderContext(options);
    }
}
