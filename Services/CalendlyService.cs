using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AsterSupportAgent.Services;

public class CalendlyBookingResult
{
    public string? BookingUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
}

public interface ICalendlyService
{
    Task<CalendlyBookingResult> CreateBookingLinkAsync(string? reason);
}

/// <summary>
/// Calendly's public API does not allow direct third-party booking creation
/// (no "POST a booking" endpoint exists — only the calendar owner can create
/// events via OAuth; end-user booking always happens through Calendly's own
/// hosted page).
///
/// The real, supported integration pattern: call POST /scheduling_links to
/// generate a genuine, authenticated, single-use link tied to real
/// availability, then hand that link to the user to complete on Calendly's
/// UI.
/// </summary>
public class CalendlyService(HttpClient httpClient, IConfiguration config, ILogger<CalendlyService> logger) : ICalendlyService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string? _apiKey = config["Calendly:ApiKey"];
    private readonly string? _eventTypeUri = config["Calendly:EventTypeUri"];
    private readonly ILogger<CalendlyService> _logger = logger;

    public async Task<CalendlyBookingResult> CreateBookingLinkAsync(string? reason)
    {
        _logger.LogInformation("Calendly — CreateBookingLink called, reason: {Reason}", reason ?? "(none)");

        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_eventTypeUri))
        {
            _logger.LogError("Calendly — Missing config: ApiKey={HasApiKey}, EventTypeUri={HasEventTypeUri}",
                !string.IsNullOrWhiteSpace(_apiKey), !string.IsNullOrWhiteSpace(_eventTypeUri));
            return new CalendlyBookingResult
            {
                ErrorMessage = "Calendly API key or event type URI is not configured.",
            };
        }

        try
        {
            var payload = JsonSerializer.Serialize(
                new
                {
                    max_event_count = 1,
                    owner = _eventTypeUri,
                    owner_type = "EventType",
                }
            );

            _logger.LogInformation("Calendly — POST scheduling_links, eventType: {EventTypeUri}, payload: {Payload}",
                _eventTypeUri, payload);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.calendly.com/scheduling_links"
            )
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Calendly — API response: status={StatusCode}, body={Body}",
                (int)response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Calendly — API request failed, status: {StatusCode}", response.StatusCode);
                return new CalendlyBookingResult
                {
                    ErrorMessage =
                        $"Calendly API request failed with status code {response.StatusCode}.",
                    Reason = body,
                };
            }

            using var doc = JsonDocument.Parse(body);
            var bookingUrl = doc
                .RootElement.GetProperty("resource")
                .GetProperty("booking_url")
                .GetString();

            _logger.LogInformation("Calendly — Booking link created: {BookingUrl}", bookingUrl);

            return new CalendlyBookingResult
            {
                BookingUrl = bookingUrl,
                Reason = reason,
                Note = "This link is valid for a single booking and may expire after use.",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Calendly — Exception while creating booking link");
            return new CalendlyBookingResult
            {
                ErrorMessage = "An error occurred while creating the booking link.",
                Reason = ex.Message,
            };
        }
    }
}
