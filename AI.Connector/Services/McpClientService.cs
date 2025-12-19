using ModelContextProtocol.Client;
using ModelContextProtocol;
using System.Diagnostics;

namespace AI.Connector.Services
{
    public interface IMcpClientService
    {
        Task<List<McpTool>> GetToolsAsync();
        Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments);
    }

    public class McpClientService : IMcpClientService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<McpClientService> _logger;
        private IMcpClient? _client; // Store client to reuse

        public McpClientService(IConfiguration configuration, ILogger<McpClientService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<IMcpClient> GetClientAsync()
        {
             if (_client != null) return _client;

             var exePath = _configuration["McpServer:ExecutablePath"];
             if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
             {
                 // Fallback
                 exePath = @"D:\OutSource\MCP-Server\MCP-Server\bin\Debug\net8.0\MCP-Server.exe";
             }

             var transport = new StdioClientTransport(new StdioClientTransportOptions
             {
                 Command = exePath,
                 Arguments = Array.Empty<string>()
             });

             _client = await McpClientFactory.CreateAsync(transport);
             return _client;
        }

        public async Task<List<McpTool>> GetToolsAsync()
        {
            try
            {
                var client = await GetClientAsync();
                var tools = await client.ListToolsAsync();
                
                _logger.LogInformation($"✅ Tìm thấy {tools.Count} tools từ MCP Server");

                return tools.Select(t => new McpTool 
                { 
                    Name = t.Name, 
                    Description = t.Description,
                    // InputSchema = t.InputSchema // Property not available in McpClientTool
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kết nối MCP Server");
                return new List<McpTool>();
            }
        }

        public async Task<string> CallToolAsync(string toolName, Dictionary<string, object> arguments)
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client.CallToolAsync(toolName, arguments);
                
                if (result.Content != null && result.Content.Any())
                {
                     // Simple text extraction from first content item
                     // In real world, process result.Content (list of Mixed content)
                     // For now, assume it returns text (TextContent or similar)
                     var first = result.Content.First();
                     // Serialize entire content to string for Kernel
                     return System.Text.Json.JsonSerializer.Serialize(first);
                }
                
                return "No content";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gọi tool {toolName}");
                return $"Error: {ex.Message}";
            }
        }
    }

    // DTO cho Tool map từ thư viện
    public class McpTool
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object? InputSchema { get; set; }

        // Helper để convert sang KernelFunction (sẽ implement sau hoặc dùng dynamic)
        public Microsoft.SemanticKernel.KernelFunction AsKernelFunction()
        {
             // Đơn giản hóa: Trả về một dummy function, 
             // thực tế cần logic dynamic để map schema JSON sang StartFunction
             // Ở đây ta tạm thời return null hoặc throw NotImplemented để user biết cần xử lý đoạn này kỹ hơn
             // Nhưng để build pass, ta sẽ update KernelService để xử lý mapping này thủ công hoặc bỏ qua
             throw new NotImplementedException("Cần implement logic dynamic tạo KernelFunction từ JsonSchema");
        }
    }
}
