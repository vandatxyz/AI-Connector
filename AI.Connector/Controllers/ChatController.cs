using AI.Connector.Services;
using Microsoft.AspNetCore.Mvc;

namespace AI.Connector.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IVisionService _visionService;
        private readonly IDocumentService _documentService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatService chatService,
            IVisionService visionService,
            IDocumentService documentService,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _visionService = visionService;
            _documentService = documentService;
            _logger = logger;
        }

        // POST: /api/chat/send - Chat text thông thường
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Tin nhắn không được để trống");
            }

            try
            {
                string sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? "default-session" : request.SessionId;

                _logger.LogInformation($"📩 Nhận tin nhắn: {request.Message} (Session: {sessionId})");

                var response = await _chatService.ChatAsync(request.Message, sessionId);

                return Ok(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý chat");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // POST: /api/chat/analyze-image - Phân tích ẢNH bằng Vision model
        [HttpPost("analyze-image")]
        public async Task<IActionResult> AnalyzeImage([FromBody] ImageAnalysisRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ImageBase64))
            {
                return BadRequest(new { success = false, error = "ImageBase64 không được để trống" });
            }

            try
            {
                _logger.LogInformation($"🖼️ Nhận yêu cầu phân tích ảnh. Size: {request.ImageBase64.Length} chars");

                var result = await _visionService.AnalyzeImageAsync(
                    request.ImageBase64,
                    request.Prompt,
                    request.MimeType ?? "image/jpeg"
                );

                if (!result.Success)
                {
                    return StatusCode(500, new { success = false, error = result.Error });
                }

                return Ok(new
                {
                    success = true,
                    data = result.Content,
                    model = result.Model
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân tích ảnh");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // POST: /api/chat/analyze-document - Phân tích TÀI LIỆU (PDF/Word text đã trích xuất)
        [HttpPost("analyze-document")]
        public async Task<IActionResult> AnalyzeDocument([FromBody] DocumentAnalysisRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { success = false, error = "Content không được để trống" });
            }

            try
            {
                _logger.LogInformation($"📄 Nhận yêu cầu phân tích tài liệu. Content length: {request.Content.Length} chars");

                var result = await _documentService.AnalyzeDocumentAsync(request.Content, request.Prompt);

                if (!result.Success)
                {
                    return StatusCode(500, new { success = false, error = result.Error });
                }

                return Ok(new
                {
                    success = true,
                    data = result.Content,
                    model = result.Model
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân tích tài liệu");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    // DTOs
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }

    public class ImageAnalysisRequest
    {
        public string ImageBase64 { get; set; } = string.Empty;
        public string? Prompt { get; set; }
        public string? MimeType { get; set; } = "image/jpeg";
        public string? SessionId { get; set; }
    }

    public class DocumentAnalysisRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? Prompt { get; set; }
        public string? SessionId { get; set; }
    }
}
