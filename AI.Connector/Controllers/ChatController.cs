using AI.Connector.Services;
using Microsoft.AspNetCore.Mvc;

namespace AI.Connector.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Đường dẫn sẽ là: api/chat
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        // Dependency Injection: Inject ChatService vào Controller
        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        // Định nghĩa API POST: /api/chat/send
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Tin nhắn không được để trống");
            }

            try
            {
                // Tạo SessionId nếu người dùng không gửi lên (mặc định là "default-session")
                string sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? "default-session" : request.SessionId;

                _logger.LogInformation($"📩 Nhận tin nhắn: {request.Message} (Session: {sessionId})");

                // Gọi Service để xử lý với SessionId
                var response = await _chatService.ChatAsync(request.Message, sessionId);

                _logger.LogInformation($"📤 AI trả lời: {response}");

                // Trả về kết quả JSON
                return Ok(new 
                { 
                    success = true,
                    data = response 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý chat");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    // Class DTO (Data Transfer Object) để nhận dữ liệu từ Body
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; } // Thêm trường SessionId (Optional)
    }
}
