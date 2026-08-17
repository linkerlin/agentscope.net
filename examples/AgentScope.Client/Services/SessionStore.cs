using Microsoft.EntityFrameworkCore;
using AgentScope.Client.Models;

namespace AgentScope.Client.Services;

public class SessionStore : ISessionStore
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public SessionStore(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<ChatSession>> GetAllSessionsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.ChatSessions
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    }

    public async Task<ChatSession?> GetSessionAsync(Guid sessionId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.ChatSessions.FindAsync(sessionId);
    }

    public async Task<ChatSession> CreateSessionAsync(Guid? agentConfigId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            Title = "新会话",
            AgentConfigId = agentConfigId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var messages = await db.ChatMessages.Where(m => m.SessionId == sessionId).ToListAsync();
        db.ChatMessages.RemoveRange(messages);
        var session = await db.ChatSessions.FindAsync(sessionId);
        if (session != null) db.ChatSessions.Remove(session);
        await db.SaveChangesAsync();
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(Guid sessionId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<ChatMessage> SaveMessageAsync(Guid sessionId, string role, string? content)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var msg = new ChatMessage
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow
        };
        db.ChatMessages.Add(msg);

        var session = await db.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        return msg;
    }

    public async Task UpdateSessionTitleAsync(Guid sessionId, string title)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var session = await db.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.Title = title;
            session.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}
