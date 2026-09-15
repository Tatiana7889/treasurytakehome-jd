using System.Text.Json.Serialization;
using Ttb.LabelReview.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services -----------------------------------------------------------

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TTB Label Review Prototype API",
        Version = "v1",
        Description = "OCR-based label-to-application verification for TTB compliance review."
    });
});

builder.Services.AddScoped<IImagePreprocessingService, ImagePreprocessingService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IFieldExtractionService, FieldExtractionService>();
builder.Services.AddScoped<IComparisonService, ComparisonService>();
builder.Services.AddScoped<ILabelAnalysisService, LabelAnalysisService>();


var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// --- Middleware pipeline --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TTB Label Review Prototype API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestampUtc = DateTimeOffset.UtcNow }))
   .WithName("HealthCheck");

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
