using AsterSupportAgent.Services;
using AsterSupportAgent.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddConsole();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// MVC Controllers
builder.Services.AddControllers();

// CORS - open for local development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
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

app.MapControllers();

app.Run();
