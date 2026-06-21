using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AsterSupportAgent.Models;
using AsterSupportAgent.Services.Interfaces;

namespace AsterSupportAgent.Services;

public class OpenRouterClient(HttpClient httpClient, IConfiguration config) : ILLMClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string? _apiKey = config["OpenRouter:ApiKey"];
    private readonly string? _model = config["OpenRouter:Model"] ?? "gpt-4o-mini";
    private readonly string? _baseUrl =
        config["OpenRouter:BaseUrl"] ?? "https://api.openrouter.ai/v1";

    public async Task<string> CompleteAsync(List<ChatMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("OpenRouter API key is not configured.");
        }

        var payload = JsonSerializer.Serialize(
            new
            {
                model = _model,
                messages = messages
                    .Select(m => new { role = m.Role, content = m.Content })
                    .ToList(),
                temperature = 0.3,
            }
        );

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenRouter API request failed with status code {response.StatusCode}: {body}"
            );
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc
            .RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        return content ?? string.Empty;
    }
}
