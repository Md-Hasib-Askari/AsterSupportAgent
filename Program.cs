var builder = WebApplication.CreateBuilder(args);

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
// TODO: add services here

// HttpClient-backed Services
// TODO: add HttpClient-backed services here

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
