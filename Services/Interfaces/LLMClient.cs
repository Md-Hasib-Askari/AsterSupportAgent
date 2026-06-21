using AsterSupportAgent.Models;

namespace AsterSupportAgent.Services.Interfaces;

public interface ILLMClient
{
    Task<string> CompleteAsync(List<ChatMessage> messages);
}
