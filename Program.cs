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

// HttpClient-backed Services
builder.Services.AddHttpClient<ILLMClient, OllamaClient>(client =>
{
    client.BaseAddress = new Uri("https://ollama.com/api/chat");
    client.DefaultRequestHeaders.Add(
        "Authorization",
        $"Bearer {builder.Configuration["Ollama:ApiKey"]}"
    );
});
builder.Services.AddHttpClient<ICalendlyService, CalendlyService>(client =>
{
    client.BaseAddress = new Uri("https://api.calendly.com/");
    client.DefaultRequestHeaders.Add(
        "Authorization",
        $"Bearer {builder.Configuration["CALENDLY_API_KEY"]}"
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/api/health");
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseDefaultFiles(); // serve wwwroot/index.html at "/"
app.UseStaticFiles();

app.MapControllers();

app.Run();
