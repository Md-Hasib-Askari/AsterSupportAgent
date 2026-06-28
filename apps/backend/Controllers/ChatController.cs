using AsterSupportAgent.DTOs;
using AsterSupportAgent.Models;
using AsterSupportAgent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AsterSupportAgent.Controllers;

[ApiController]
[Route("api/chat")]
[EnableRateLimiting("sliding-by-ip")]
public class ChatController(
    IAgentService agentService,
    ISessionStore sessionStore,
    ILogger<ChatController> logger
) : ControllerBase
{
    private readonly IAgentService _agentService = agentService;
    private readonly ISessionStore _sessionStore = sessionStore;
    private readonly ILogger<ChatController> _logger = logger;

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty");
        }

        var sessionId = string.IsNullOrEmpty(request.SessionId)
            ? Guid.NewGuid().ToString()
            : request.SessionId;

        try
        {
            var history = _sessionStore.Get(sessionId);
            var result = await _agentService.RunAsync(
                userMessage: request.Message,
                conversationHistory: history
            );

            var updatedHistory = new List<ChatMessage>(history)
            {
                new() { Role = ChatMessageRole.USER, Content = request.Message },
                new() { Role = ChatMessageRole.ASSISTANT, Content = result.Reply },
            };
            _sessionStore.Set(sessionId, updatedHistory);

            return Ok(
                new ChatResponse(Reply: result.Reply, Trace: result.Trace, SessionId: sessionId)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
