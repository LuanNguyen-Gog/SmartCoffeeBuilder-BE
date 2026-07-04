using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartCoffeeBuilder.API.Middlewares;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Implementations;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.SeedData;
using SmartCoffeeBuilder.Service.Implementations;
using SmartCoffeeBuilder.Service.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<SmartCafeBuilderContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention());

// DI
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IShopOwnerService, ShopOwnerService>();
builder.Services.AddScoped<IServiceProviderService, ServiceProviderService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IDesignBriefService, DesignBriefService>();
builder.Services.AddScoped<IAiRecommendationService, AiRecommendationService>();
builder.Services.AddScoped<IProjectPostService, ProjectPostService>();
builder.Services.AddScoped<IProjectApplicationService, ProjectApplicationService>();
builder.Services.AddScoped<IProjectProviderService, ProjectProviderService>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();

// Hangfire — job nền chạy trên cùng Postgres (schema "hangfire" tự tạo).
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Missing configuration: Jwt:Key");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Auth middleware không throw exception nên GlobalExceptionHandler không bắt được 401.
        // Log lý do thất bại + trả về body JSON thay vì 401 rỗng.
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");
                logger.LogWarning(
                    context.Exception,
                    "JWT authentication failed on {Method} {Path}: {Message}",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path,
                    context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                // Ngăn ASP.NET ghi response 401 rỗng mặc định.
                context.HandleResponse();

                // Lý do thật nằm ở AuthenticateFailure (token hết hạn, sai chữ ký,
                // sai issuer/audience...). ErrorDescription thường rỗng nên đọc thêm.
                var reason = context.AuthenticateFailure?.Message
                    ?? (context.HttpContext.Request.Headers.ContainsKey("Authorization")
                        ? "Authorization header present but token could not be validated."
                        : "No Authorization header was sent.");

                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");
                logger.LogWarning(
                    "JWT challenge on {Method} {Path}: error={Error}, reason={Reason}, failure={Failure}",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path,
                    context.Error,
                    reason,
                    context.AuthenticateFailure?.GetType().Name);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = reason,
                    Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}"
                });
            }
        };
    });

builder.Services.AddAuthorization();

// CORS — tạm thời allow all vì chưa chốt origin của FE web/mobile.
// Khi FE ổn định, thay bằng WithOrigins(...) cụ thể.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Global exception handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();

// Swagger UI (dev + production)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartCoffeeBuilder API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token. Ví dụ: eyJhbGci..."
    });
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), [] }
    });
});

var app = builder.Build();

// Seed dữ liệu mẫu (idempotent — bỏ qua nếu DB đã có dữ liệu).
// Không để lỗi seeding chặn việc app lắng nghe port 8080 (Cloud Run startup probe).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartCafeBuilderContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migrate/seed failed at startup; continuing so the server can start.");
    }
}

// Job refresh OTP mỗi phút: xoay CurrentCode/PreviousCode theo chu kỳ TOTP
// và dọn các OTP đã dùng / hết hạn.
using (var scope = app.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<IOtpService>(
        "refresh-otps",
        service => service.RefreshOtpsAsync(),
        Cron.Minutely());
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartCoffeeBuilder API v1");
    options.RoutePrefix = "swagger";
});

app.UseExceptionHandler();

// Cloud Run xử lý HTTPS ở load balancer — không cần redirect trong container
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();

    // Dashboard theo dõi job (http://localhost:xxxx/hangfire) — chỉ mở ở môi trường dev.
    app.UseHangfireDashboard("/hangfire");
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
