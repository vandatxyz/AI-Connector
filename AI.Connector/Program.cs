using AI.Connector.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Serilog (Ghi log)
// Đọc cấu hình từ appsettings.json
builder.Host.UseSerilog((context, configuration) => 
    configuration.ReadFrom.Configuration(context.Configuration));

// 2. Đăng ký các dịch vụ (Dependency Injection)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Tạo tài liệu API Swagger

// Đăng ký McpClientService (Singleton)
builder.Services.AddSingleton<IMcpClientService, McpClientService>();

// Đăng ký KernelService (Singleton vì Kernel có thể dùng chung)
builder.Services.AddSingleton<IKernelService, KernelService>();

// Đăng ký ChatSessionService để quản lý phiên chat (Singleton)
builder.Services.AddSingleton<IChatSessionService, ChatSessionService>();

// Đăng ký ChatService (Scoped: Mỗi request tạo 1 instance mới - Hoặc Singleton nếu muốn giữ history chung)
// Ở đây dùng Scoped cho demo, mỗi lần gọi API sẽ là 1 phiên chat mới nếu không quản lý Session
builder.Services.AddScoped<IChatService, ChatService>();

var app = builder.Build();

// 3. Cấu hình Pipeline (Middleware)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Giao diện test API tại /swagger
}

// Log request HTTP tự động
app.UseSerilogRequestLogging();

app.UseAuthorization();
app.MapControllers(); // Map các Controller vào đường dẫn API

Console.WriteLine("🚀 Connector API đang chạy...");
app.Run();
