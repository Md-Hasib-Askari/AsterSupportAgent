using System.Collections.Concurrent;
using AsterSupportAgent.Models;

namespace AsterSupportAgent.Services;

public interface ISessionStore
{
    List<ChatMessage> Get(string sessionId);
    void Set(string sessionId, List<ChatMessage> history);
}

///  <summary>
///  In-memory session store (portfolio scope - swap for Redis/distributed
///  cache in production; resets on app restart and won't scale past one
///  instance.)
///  </summary
public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessions = new();
    private const int MaxHistoryLength = 20;

    public List<ChatMessage> Get(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var history) ? history : [];

    public void Set(string sessionId, List<ChatMessage> history)
    {
        var trimmed =
            history.Count > MaxHistoryLength
                ? [.. history.Skip(history.Count - MaxHistoryLength)]
                : history;
        _sessions[sessionId] = trimmed;
    }
}
