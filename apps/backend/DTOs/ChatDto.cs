namespace AsterSupportAgent.DTOs;

public record ChatRequest(string Message, string? SessionId = null);

public record ChatResponse(string Reply, List<TraceStep> Trace, string SessionId);

// 1. Agentic LLM chatbot with a tool-calling loop — the AI decides when to search KB, look up orders, or create Calendly booking links, then responds with full step-by-step trace.

// 2. Pluggable backend via strategy pattern, hot-swapping between Ollama and OpenRouter at runtime from config with no code changes.

// 3. .NET 10 minimal API + vanilla frontend — ASP.NET Core MVC controllers, DI, IHttpClientFactory, Calendly API integration, session management, zero external NuGet deps.
