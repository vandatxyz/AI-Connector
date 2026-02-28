using AI.Connector.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Serilog (Ghi log)
builder.Host.UseSerilog((context, configuration) => 
    configuration.ReadFrom.Configuration(context.Configuration));

// 2. Đăng ký các dịch vụ (Dependency Injection)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký McpClientService (Singleton)
builder.Services.AddSingleton<IMcpClientService, McpClientService>();

// Đăng ký KernelService (Singleton vì Kernel có thể dùng chung)
builder.Services.AddSingleton<IKernelService, KernelService>();

// Đăng ký ChatSessionService để quản lý phiên chat (Singleton)
builder.Services.AddSingleton<IChatSessionService, ChatSessionService>();

// Đăng ký ChatService (Scoped: Mỗi request tạo 1 instance mới)
builder.Services.AddScoped<IChatService, ChatService>();

// Đăng ký LmStudioClient - base HTTP client cho LM Studio (timeout 5 phút cho Vision)
builder.Services.AddHttpClient<ILmStudioClient, LmStudioClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Đăng ký VisionService - phân tích ẢNH
builder.Services.AddScoped<IVisionService, VisionService>();

// Đăng ký DocumentService - phân tích TÀI LIỆU (PDF/Word)
builder.Services.AddScoped<IDocumentService, DocumentService>();

var app = builder.Build();

// 3. Cấu hình Pipeline (Middleware)
// Bật Swagger ở mọi môi trường để test
app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();

app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🚀 Connector API đang chạy...");
app.Run();
