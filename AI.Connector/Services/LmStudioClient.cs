using System.Text;
using System.Text.Json;

namespace AI.Connector.Services
{
    /// <summary>
    /// Client chung cho LM Studio API - xử lý kết nối và gọi API
    /// </summary>
    public interface ILmStudioClient
    {
        /// <summary>
        /// Gọi LM Studio Chat Completions API với message dạng text
        /// </summary>
        Task<LmStudioResponse> ChatCompletionAsync(object[] messages, string? model = null, int maxTokens = 4096, double temperature = 0.3);

        /// <summary>
        /// Lấy thông tin cấu hình endpoint và model
        /// </summary>
        (string Endpoint, string Model, string VisionModel, string ApiKey) GetConfig();
    }

    public class LmStudioClient : ILmStudioClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LmStudioClient> _logger;

        public LmStudioClient(HttpClient httpClient, IConfiguration configuration, ILogger<LmStudioClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public (string Endpoint, string Model, string VisionModel, string ApiKey) GetConfig()
        {
            var openAIConfig = _configuration.GetSection("OpenAI");
            return (
                Endpoint: openAIConfig["Endpoint"] ?? "http://localhost:1234/v1",
                Model: openAIConfig["Model"] ?? "qwen/qwen2.5-vl-7b",
                VisionModel: openAIConfig["VisionModel"] ?? openAIConfig["Model"] ?? "qwen/qwen2.5-vl-7b",
                ApiKey: openAIConfig["ApiKey"] ?? "lm-studio"
            );
        }

        public async Task<LmStudioResponse> ChatCompletionAsync(object[] messages, string? model = null, int maxTokens = 4096, double temperature = 0.3)
        {
            try
            {
                var config = GetConfig();
                var useModel = model ?? config.Model;
                var apiUrl = $"{config.Endpoint.TrimEnd('/')}/chat/completions";

                var requestBody = new
                {
                    model = useModel,
                    messages,
                    max_tokens = maxTokens,
                    temperature
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

                _logger.LogInformation($"🔗 Gọi LM Studio API: {apiUrl} (model: {useModel})");

                var response = await _httpClient.PostAsync(apiUrl, httpContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"❌ LM Studio lỗi: {response.StatusCode} - {responseBody}");
                    return new LmStudioResponse
                    {
                        Success = false,
                        Error = $"LM Studio error: {response.StatusCode} - {responseBody}"
                    };
                }

                // Parse response
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                var content = root
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                _logger.LogInformation($"✅ LM Studio trả về thành công. Content length: {content.Length}");

                return new LmStudioResponse
                {
                    Success = true,
                    Content = content,
                    Model = useModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi gọi LM Studio");
                return new LmStudioResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }

    public class LmStudioResponse
    {
        public bool Success { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Model { get; set; }
        public string? Error { get; set; }
    }
}
