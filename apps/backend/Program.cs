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

// Rate limiting: 30 requests per minute per IP address
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("fixed-by-ip", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }
        ));
});

// CORS — allow frontend origin
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://aster-agent.vercel.app", "http://localhost:3000")
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseRateLimiter();

app.MapControllers();

app.Run();
