using AsterSupportAgent.Services;
using AsterSupportAgent.Services.Interfaces;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddConsole();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// MVC Controllers
builder.Services.AddControllers();

// Rate limiting: 15 requests per day per IP address
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.Append("Retry-After", "86400");
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("sliding-by-ip", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromDays(1),
                SegmentsPerWindow = 24,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }
        ));
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://aster-agent.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// App Services
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddSingleton<IAgentService, AgentService>();
builder.Services.AddSingleton<IKbSearchService, KbSearchService>();
builder.Services.AddSingleton<IOrderService, OrderService>();

// HttpClient-backed Services — register both clients
builder.Services.AddHttpClient<OllamaClient>();
builder.Services.AddHttpClient<OpenRouterClient>();
builder.Services.AddSingleton<ILLMClient, LLMClientStrategy>();
builder.Services.AddHttpClient<ICalendlyService, CalendlyService>(client =>
{
    client.BaseAddress = new Uri("https://api.calendly.com/");
    client.DefaultRequestHeaders.Add(
        "Authorization",
        $"Bearer {builder.Configuration["Calendly:ApiKey"]}"
    );
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRateLimiter();

app.MapControllers();

app.Run();
