using StringAnalyzerService.Services;

var builder = WebApplication.CreateBuilder(args);

//  Configure Services (Dependency Injection)
builder.Services.AddSingleton<StringAnalyzerService.Services.StringAnalyzerService>();

// Add Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Optional: Keep JSON camelCase (like HNG spec)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enable CORS (important for hosted APIs)
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

//  Configure HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Optional: Force redirect from HTTP → HTTPS
app.UseHttpsRedirection();

// Use the CORS policy
app.UseCors("AllowAll");

// Map your controllers
app.MapControllers();

// Run the app
app.Run();
