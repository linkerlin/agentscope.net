using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public interface ISessionStore
{
    Task<List<ChatSession>> GetAllSessionsAsync();
    Task<ChatSession?> GetSessionAsync(Guid sessionId);
    Task<ChatSession> CreateSessionAsync(Guid? agentConfigId = null);
    Task DeleteSessionAsync(Guid sessionId);
    Task<List<ChatMessage>> GetMessagesAsync(Guid sessionId);
    Task<ChatMessage> SaveMessageAsync(Guid sessionId, string role, string? content);
    Task UpdateSessionTitleAsync(Guid sessionId, string title);
}
