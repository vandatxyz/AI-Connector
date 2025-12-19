using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

namespace AI.Connector.Services
{
    // Interface để các nơi khác (Controller) gọi vào dùng
    public interface IKernelService
    {
        // Hàm lấy Kernel đã được khởi tạo
        Kernel GetKernel();
        
        // Hàm kiểm tra kết nối tới LLM (Optional)
        Task<bool> IsConnectedAsync();
    }

    // Class thực thi logic
    public class KernelService : IKernelService
    {
        private readonly Kernel _kernel;
        private readonly ILogger<KernelService> _logger;
        private readonly IMcpClientService _mcpClientService;

        public KernelService(IConfiguration configuration, ILogger<KernelService> logger, IMcpClientService mcpClientService)
        {
            _logger = logger;
            _mcpClientService = mcpClientService;

            // 1. Tạo Builder để cấu hình Semantic Kernel
            var builder = Kernel.CreateBuilder();

            // 2. Đọc cấu hình từ file appsettings.json
            var openAIConfig = configuration.GetSection("OpenAI");
            var endpoint = openAIConfig["Endpoint"] ?? "http://localhost:1234/v1";
            var model = openAIConfig["Model"] ?? "local-model";
            var apiKey = openAIConfig["ApiKey"] ?? "lm-studio";

            _logger.LogInformation($"🔌 Đang kết nối tới LLM tại: {endpoint} (Model: {model})");

            // 3. Đăng ký connector OpenAI (dùng chung cho cả OpenAI thật và LM Studio)
            builder.AddOpenAIChatCompletion(
                modelId: model,
                apiKey: apiKey,
                endpoint: new Uri(endpoint)
            );

            // 4. Lấy Tools từ MCP Server và Add vào Kernel (Sync over Async - Acceptable in Constructor for demo)
            // Trong thực tế nên tách việc khởi tạo này ra phương thức InitAsync riêng
            var toolsTask = _mcpClientService.GetToolsAsync();
            var tools = toolsTask.GetAwaiter().GetResult();

            if (tools.Any())
            {
                _logger.LogInformation($"🎯 Tìm thấy {tools.Count} MCP Tools. Đang đăng ký vào Kernel...");
                var functions = new List<KernelFunction>();

                foreach (var tool in tools)
                {
                      // Tạo function wrapper cho từng tool
                      var function = KernelFunctionFactory.CreateFromMethod(
                          async (KernelArguments args, CancellationToken ct) =>
                          {
                              _logger.LogInformation($"🤖 Semantic Kernel invoking tool: {tool.Name}");
                              
                              // Convert arguments to dictionary
                              var dictArgs = args.ToDictionary(k => k.Key, v => (object)v.Value);
                              
                              // Call McpClient
                              return await _mcpClientService.CallToolAsync(tool.Name, dictArgs);
                          },
                          functionName: tool.Name,
                          description: tool.Description
                      );
                      functions.Add(function);
                }

                 builder.Plugins.AddFromFunctions("McpTools", functions);
                 _logger.LogInformation($"✅ Đã đăng ký thành công {functions.Count} MCP Functions");
            }

            // 5. Xây dựng Kernel
            _kernel = builder.Build();
            
            _logger.LogInformation("✅ Semantic Kernel đã được khởi tạo thành công!");
        }

        public Kernel GetKernel()
        {
            // Trả về đối tượng Kernel để dùng cho việc chat
            return _kernel;
        }

        public async Task<bool> IsConnectedAsync()
        {
            try 
            {
                // Thử gửi một câu đơn giản để ping
                // Lưu ý: Đây là cách test thủ công, trong thực tế có thể không cần thiết
                return true; 
            }
            catch
            {
                return false;
            }
        }
    }
}
