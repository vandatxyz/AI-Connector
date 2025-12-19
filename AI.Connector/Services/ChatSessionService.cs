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
                history.AddSystemMessage("Bạn là một trợ lý AI hữu ích. Hãy trả lời ngắn gọn và đi thẳng vào vấn đề.");
                
                return history;
            });
        }

        public void ClearHistory(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }
}
