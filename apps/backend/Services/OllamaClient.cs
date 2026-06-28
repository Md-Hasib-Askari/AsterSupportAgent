using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AsterSupportAgent.Models;
using AsterSupportAgent.Services.Interfaces;

namespace AsterSupportAgent.Services;

public class OllamaClient(
    HttpClient httpClient,
    IConfiguration config,
    ILogger<OllamaClient> logger
) : ILLMClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string? _apiKey = config["Ollama:ApiKey"];
    private readonly string? _model = config["Ollama:Model"] ?? "gemma4:31b-cloud";
    private readonly string? _baseUrl = config["Ollama:BaseUrl"] ?? "https://ollama.com/api";
    private readonly ILogger<OllamaClient> _logger = logger;

    public async Task<string> CompleteAsync(List<ChatMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Ollama API key is not configured");
            throw new InvalidOperationException("Ollama API key is not configured.");
        }

        _logger.LogInformation(
            "Sending request to Ollama — model: {Model}, messages: {Count}, endpoint: {Endpoint}",
            _model,
            messages.Count,
            $"{_baseUrl}/chat"
        );
        var payload = JsonSerializer.Serialize(
            new
            {
                model = _model,
                messages = messages
                    .Select(m => new { role = m.Role, content = m.Content })
                    .ToList(),
                temperature = 0.3,
                stream = false,
            }
        );

        _logger.LogDebug("Request payload: {Payload}", payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await _httpClient.SendAsync(request);
        sw.Stop();

        _logger.LogInformation(
            "Ollama responded with status {StatusCode} in {ElapsedMs}ms",
            (int)response.StatusCode,
            sw.ElapsedMilliseconds
        );
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Ollama API request failed — status: {StatusCode}, body: {Body}",
                (int)response.StatusCode,
                body
            );
            throw new InvalidOperationException(
                $"Ollama API request failed with status code {response.StatusCode}: {body}"
            );
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.TryGetProperty("choices", out var choices)
            ? choices[0].GetProperty("message").GetProperty("content").GetString()
            : doc.RootElement.GetProperty("message").GetProperty("content").GetString();

        _logger.LogInformation("Ollama response: {Content}", content);
        return content ?? string.Empty;
    }
}
