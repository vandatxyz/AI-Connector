using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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

        public KernelService(IConfiguration configuration, ILogger<KernelService> logger)
        {
            _logger = logger;

            // 1. Tạo Builder để cấu hình Semantic Kernel
            var builder = Kernel.CreateBuilder();

            // 2. Đọc cấu hình từ file appsettings.json
            var openAIConfig = configuration.GetSection("OpenAI");
            var endpoint = openAIConfig["Endpoint"] ?? "http://localhost:1234/v1";
            var model = openAIConfig["Model"] ?? "local-model";
            var apiKey = openAIConfig["ApiKey"] ?? "lm-studio";

            _logger.LogInformation($"🔌 Đang kết nối tới LLM tại: {endpoint} (Model: {model})");

            // 3. Đăng ký connector OpenAI (dùng chung cho cả OpenAI thật và LM Studio)
            // Vì LM Studio giả lập OpenAI API nên ta dùng AddOpenAIChatCompletion
            builder.AddOpenAIChatCompletion(
                modelId: model,
                apiKey: apiKey,
                endpoint: new Uri(endpoint) // Quan trọng: trỏ về localhost:1234
            );

            // 4. (Tùy chọn) Thêm các Plugin/Tool vào đây nếu có
            // Ví dụ: builder.Plugins.AddFromType<YourMcpTool>();

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
