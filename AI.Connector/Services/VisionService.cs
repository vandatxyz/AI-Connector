namespace AI.Connector.Services
{
    public interface IVisionService
    {
        /// <summary>
        /// Phân tích ảnh bằng LLM Vision model (Qwen2.5 VL)
        /// </summary>
        Task<LmStudioResponse> AnalyzeImageAsync(string imageBase64, string? prompt = null, string mimeType = "image/jpeg");
    }

    public class VisionService : IVisionService
    {
        private readonly ILmStudioClient _lmClient;
        private readonly ILogger<VisionService> _logger;
        private readonly string _defaultPrompt;

        public VisionService(ILmStudioClient lmClient, ILogger<VisionService> logger, IWebHostEnvironment env)
        {
            _lmClient = lmClient;
            _logger = logger;

            // Đọc prompt từ file txt
            var promptPath = Path.Combine(env.ContentRootPath, "Prompts", "invoice_extraction_prompt.txt");
            if (File.Exists(promptPath))
            {
                _defaultPrompt = File.ReadAllText(promptPath);
                _logger.LogInformation($"✅ Đã load prompt từ: {promptPath} ({_defaultPrompt.Length} chars)");
            }
            else
            {
                _defaultPrompt = "Extract all information from this invoice image and return as JSON.";
                _logger.LogWarning($"⚠️ Không tìm thấy prompt file: {promptPath}, dùng prompt mặc định");
            }
        }

        public async Task<LmStudioResponse> AnalyzeImageAsync(string imageBase64, string? prompt = null, string mimeType = "image/jpeg")
        {
            var config = _lmClient.GetConfig();
            var userPrompt = prompt ?? _defaultPrompt;

            _logger.LogInformation($"🖼️ Đang phân tích ảnh với Vision model: {config.VisionModel}");

            var messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = userPrompt },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:{mimeType};base64,{imageBase64}"
                            }
                        }
                    }
                }
            };

            return await _lmClient.ChatCompletionAsync(messages, config.VisionModel);
        }
    }
}
