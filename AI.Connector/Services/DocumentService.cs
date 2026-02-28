namespace AI.Connector.Services
{
    public interface IDocumentService
    {
        /// <summary>
        /// Phân tích nội dung text tài liệu (đã trích xuất từ PDF/Word) bằng LLM
        /// </summary>
        Task<LmStudioResponse> AnalyzeDocumentAsync(string textContent, string? prompt = null);
    }

    public class DocumentService : IDocumentService
    {
        private readonly ILmStudioClient _lmClient;
        private readonly ILogger<DocumentService> _logger;
        private readonly string _defaultPrompt;

        public DocumentService(ILmStudioClient lmClient, ILogger<DocumentService> logger, IWebHostEnvironment env)
        {
            _lmClient = lmClient;
            _logger = logger;

            // Đọc prompt từ file txt (dùng chung với VisionService)
            var promptPath = Path.Combine(env.ContentRootPath, "Prompts", "invoice_extraction_prompt.txt");
            if (File.Exists(promptPath))
            {
                _defaultPrompt = File.ReadAllText(promptPath);
                _logger.LogInformation($"✅ DocumentService đã load prompt từ: {promptPath}");
            }
            else
            {
                _defaultPrompt = "Extract all information from this document and return as JSON.";
                _logger.LogWarning($"⚠️ Không tìm thấy prompt file: {promptPath}");
            }
        }

        public async Task<LmStudioResponse> AnalyzeDocumentAsync(string textContent, string? prompt = null)
        {
            var userPrompt = prompt ?? _defaultPrompt;
            var fullMessage = $"{userPrompt}\n\n--- NỘI DUNG TÀI LIỆU ---\n{textContent}";

            _logger.LogInformation($"📄 Đang phân tích tài liệu. Text length: {textContent.Length} chars");

            var messages = new object[]
            {
                new
                {
                    role = "user",
                    content = fullMessage
                }
            };

            return await _lmClient.ChatCompletionAsync(messages);
        }
    }
}
