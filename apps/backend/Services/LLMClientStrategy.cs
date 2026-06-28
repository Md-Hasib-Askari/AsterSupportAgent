using AsterSupportAgent.Models;
using AsterSupportAgent.Services.Interfaces;

namespace AsterSupportAgent.Services;

public class LLMClientStrategy(
    OllamaClient ollama,
    OpenRouterClient openRouter,
    IConfiguration config
) : ILLMClient
{
    private readonly OllamaClient _ollama = ollama;
    private readonly OpenRouterClient _openRouter = openRouter;
    private readonly string _provider = config["LLM:Provider"] ?? "Ollama";

    private ILLMClient Resolve() =>
        _provider switch
        {
            "OpenRouter" => _openRouter,
            "Ollama" => _ollama,
            _ => _ollama,
        };

    public Task<string> CompleteAsync(List<ChatMessage> messages) =>
        Resolve().CompleteAsync(messages);
}
