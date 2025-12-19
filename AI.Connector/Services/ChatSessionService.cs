using System.Collections.Concurrent;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Connector.Services
{
    public interface IChatSessionService
    {
        ChatHistory GetOrCreateHistory(string sessionId);
        void ClearHistory(string sessionId);
    }

    // Service này sẽ là Singleton để giữ data trong RAM
    public class ChatSessionService : IChatSessionService
    {
        // Dùng ConcurrentDictionary để đảm bảo an toàn đa luồng (Thread-safe)
        private readonly ConcurrentDictionary<string, ChatHistory> _sessions = new();

        public ChatHistory GetOrCreateHistory(string sessionId)
        {
            return _sessions.GetOrAdd(sessionId, _ =>
            {
                var history = new ChatHistory();
                
                // System Prompt mặc định cho mọi phiên chat mới
                history.AddSystemMessage(@"Bạn là một trợ lý AI có khả năng điều khiển hệ thống thông qua các công cụ (Tools).
                Các công cụ hiện có:
                1. GetServerTime: Lấy giờ hiện tại của server.
                2. Echo: Phản hồi lại tin nhắn của người dùng.
                
                NẾU người dùng hỏi giờ, HÃY gọi tool GetServerTime.
                NẾU người dùng bảo 'echo' hoặc 'lặp lại', HÃY gọi tool Echo.
                Đừng tự bịa ra câu trả lời nếu bạn có thể dùng tool.");
                
                return history;
            });
        }

        public void ClearHistory(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }
}
