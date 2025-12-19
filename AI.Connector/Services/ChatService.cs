using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AI.Connector.Services
{
    // Interface cho dịch vụ Chat
    public interface IChatService
    {
        // Hàm gửi tin nhắn và nhận câu trả lời
        // Hàm gửi tin nhắn và nhận câu trả lời với SessionID
        Task<string> ChatAsync(string userMessage, string sessionId);
    }

    public class ChatService : IChatService
    {
        private readonly IKernelService _kernelService;
        private readonly IChatSessionService _sessionService;

        public ChatService(IKernelService kernelService, IChatSessionService sessionService)
        {
            _kernelService = kernelService;
            _sessionService = sessionService;
        }

        public async Task<string> ChatAsync(string userMessage, string sessionId)
        {
            // 1. Lấy lịch sử chat theo Session ID (người dùng cũ hay mới)
            var chatHistory = _sessionService.GetOrCreateHistory(sessionId);

            // 2. Lấy Kernel
            var kernel = _kernelService.GetKernel();

            // 3. Thêm câu hỏi vào lịch sử
            chatHistory.AddUserMessage(userMessage);

            // 4. Lấy Chat Completion Service
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // 5. Cấu hình Execution Settings
            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.7,
                MaxTokens = 2000,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // 6. Gửi tin nhắn
            var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                settings,
                kernel
            );

            // 7. Lưu câu trả lời vào lịch sử
            var responseContent = result.Content ?? "Xin lỗi, tôi không thể trả lời lúc này.";
            chatHistory.AddAssistantMessage(responseContent);

            return responseContent;
        }
    }
}
