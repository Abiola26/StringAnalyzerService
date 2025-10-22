using StringAnalyzerService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Services
builder.Services.AddSingleton<StringAnalyzerService.Services.StringAnalyzerService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enable CORS (for testing & deployment)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ✅ Always enable Swagger (even in production)
app.UseSwagger();
app.UseSwaggerUI();

// ✅ Add a friendly root endpoint (so / doesn’t show 404)
app.MapGet("/", () => Results.Ok(new
{
    message = "Welcome to the String Analyzer Service API 🚀",
    documentation = "/swagger",
    example_endpoints = new[]
    {
        "POST /strings",
        "GET /strings/{string_value}",
        "GET /strings (with filters)",
        "DELETE /strings/{string_value}",
        "GET /strings/filter-by-natural-language"
    }
}));

// Redirect HTTP → HTTPS (optional)
app.UseHttpsRedirection();

// CORS policy
app.UseCors("AllowAll");

// Map controllers
app.MapControllers();

// ✅ Use dynamic port for Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

// Run the app
app.Run();
