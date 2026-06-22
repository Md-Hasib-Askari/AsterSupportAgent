using System.Text.Json;
using System.Text.RegularExpressions;
using AsterSupportAgent.Services.Interfaces;
using AsterSupportAgent.DTOs;
using AsterSupportAgent.Models;

namespace AsterSupportAgent.Services;

public class AgentResult
{
    public string Reply { get; set; } = string.Empty;
    public List<TraceStep> Trace { get; set; } = [];
}

public interface IAgentService
{
    Task<AgentResult> RunAsync(string userMessage, List<ChatMessage> conversationHistory);
}

/// <summary>
/// The system prompt instructs the model to respond with
/// strict JSON describing ONE action at a time. The server parses that JSON,
/// executes the matching local service method, feeds the result back in as
/// a new message, and loops until the model emits a "respond" action.
/// </summary>
public class AgentService(
    ILLMClient llm,
    IKbSearchService kbSvc,
    IOrderService orderSvc,
    ICalendlyService calendlySvc
) : IAgentService
{
    private readonly ILLMClient _llm = llm;
    private readonly IKbSearchService _kbSearchService = kbSvc;
    private readonly IOrderService _orderService = orderSvc;
    private readonly ICalendlyService _calendlyService = calendlySvc;

    private const int MaxSteps = 4;

    private const string SystemPrompt = """
        You are a customer support agent for an online clothing store.

        You can take actions by responding with ONLY a single JSON object (no markdown, no prose outside the JSON) in one of these shapes:

        1. To search the help docs:
        {"action": "search_kb", "query": "<search terms>"}

        2. To look up an order:
        {"action": "get_order_status", "orderId": "<order id like ORD-1001>"}

        3. To create a real booking link for the customer to talk to a human:
        {"action": "create_booking_link", "reason": "<short reason>"}

        4. To answer the customer directly (use this once you have enough information, or if no tool is needed):
        {"action": "respond", "message": "<your reply to the customer>"}

        Rules:
        - Always respond with exactly one JSON object, nothing else.
        - Use search_kb for policy/process questions (shipping, returns, sizing, payments, etc).
        - Use get_order_status when the customer gives or references an order ID.
        - Use create_booking_link only when the customer asks to talk to a human/agent, or you cannot resolve their issue.
        - After receiving a tool result, decide whether you have enough info to "respond", or whether another action is needed.
        - Never invent order details, tracking numbers, or policies that weren't returned by a tool.
        - Keep final responses concise and friendly.
        """;

    private static AgentAction? ExtractAction(string text)
    {
        // Models sometimes wwrap JSON in ```json fences or add stray text.
        // Strip and parse defencively, returning null if we can't parse a valid action.
        var fenceMatch = Regex.Match(text, "```(?:json)?\\s*([\\s\\S]*?)```");
        var candidate = fenceMatch.Success ? fenceMatch.Groups[1].Value : text;

        var firstBrace = candidate.IndexOf('{');
        var lastBrace = candidate.LastIndexOf('}');
        if (firstBrace == -1 || lastBrace == -1 || lastBrace <= firstBrace)
            return null;

        var jsonSlice = candidate.Substring(firstBrace, lastBrace - firstBrace + 1);
        try
        {
            var action = JsonSerializer.Deserialize<AgentAction>(
                jsonSlice,
                JsonOptions.CaseInsensitive
            );
            return action;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<string> ExecuteActionAsync(AgentAction action)
    {
        switch (action.Action)
        {
            case AgentActionType.SEARCH_KB:
                var results = _kbSearchService.Search(action.Query ?? string.Empty);
                return results.Count > 0
                    ? string.Join("\n\n", results.Select(r => $"[{r.Title}] {r.Content}"))
                    : "No matching help articles found.";
            case AgentActionType.GET_ORDER_STATUS:
                var order = _orderService.GetOrderStatus(
                    action.OrderId ?? string.Empty,
                    out var error
                );
                if (order is null)
                {
                    return JsonSerializer.Serialize(new { error });
                }
                return JsonSerializer.Serialize(order);
            case AgentActionType.CREATE_BOOKING_LINK:
                var result = await _calendlyService.CreateBookingLinkAsync(action.Reason);
                return JsonSerializer.Serialize(result);
            default:
                return string.Empty;
        }
    }

    public async Task<AgentResult> RunAsync(
        string userMessage,
        List<ChatMessage> conversationHistory
    )
    {
        var messages = new List<ChatMessage>
        {
            new() { Role = ChatMessageRole.SYSTEM, Content = SystemPrompt },
        };
        messages.AddRange(conversationHistory);
        messages.Add(new() { Role = ChatMessageRole.USER, Content = userMessage });

        var trace = new List<TraceStep>();

        for (var step = 0; step < MaxSteps; step++)
        {
            var llmResponse = await _llm.CompleteAsync(messages);
            var action = ExtractAction(llmResponse);
            if (action is null || action.Action == AgentActionType.NONE)
            {
                trace.Add(
                    new TraceStep
                    {
                        Step = step + 1,
                        Type = TraceStepType.FALLBACK_RAW,
                        Raw = llmResponse,
                    }
                );
                return new AgentResult { Reply = llmResponse.Trim(), Trace = trace };
            }

            if (action.Action == AgentActionType.RESPOND)
            {
                var msg = action.Message ?? string.Empty;
                trace.Add(
                    new TraceStep
                    {
                        Step = step + 1,
                        Type = TraceStepType.RESPOND,
                        Raw = msg,
                    }
                );
                return new AgentResult { Reply = action.Message ?? string.Empty, Trace = trace };
            }

            var result = await ExecuteActionAsync(action);
            trace.Add(
                new TraceStep
                {
                    Step = step + 1,
                    Type = TraceStepType.TOOL_CALL,
                    Raw = result,
                }
            );

            messages.Add(
                new ChatMessage
                {
                    Role = ChatMessageRole.ASSISTANT,
                    Content = JsonSerializer.Serialize(action),
                }
            );
            messages.Add(
                new ChatMessage
                {
                    Role = ChatMessageRole.USER,
                    Content =
                        $"Tool result for {action.Action}: {result}\n\nNow respond with your next action JSON.",
                }
            );
        }

        return new AgentResult
        {
            Reply = "I'm having trouble resolving your issue. Please contact support directly.",
            Trace = trace,
        };
    }
}
