using FileUploadAPI.Api.Controllers;
using FileUploadAPI.Core.Interfaces;
using FileUploadAPI.Infrastructure.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Events;
using tusdotnet;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.ApplicationInsights(
        new TelemetryConfiguration { InstrumentationKey = builder.Configuration["ApplicationInsights:InstrumentationKey"] },
        TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure services
builder.Services.AddSingleton<IFileStorageService>(provider =>
{
    var configuration = builder.Configuration;
    var logger = provider.GetRequiredService<ILogger<DigitalOceanStorageService>>();
    return new DigitalOceanStorageService(
        configuration["DigitalOcean:AccessKey"],
        configuration["DigitalOcean:SecretKey"],
        configuration["DigitalOcean:Region"],
        configuration["DigitalOcean:BucketName"],
        configuration["DigitalOcean:ServiceUrl"],
        logger
    );
});

// Configure CSV repository
builder.Services.AddSingleton<IFileUploadRepository>(provider =>
{
    var configuration = builder.Configuration;
    var logger = provider.GetRequiredService<ILogger<CsvFileUploadRepository>>();
    var csvFilePath = configuration["Storage:CsvFilePath"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "uploads.csv");
    return new CsvFileUploadRepository(csvFilePath, logger);
});

builder.Services.AddSingleton<IFileUploadService, FileUploadService>();
builder.Services.AddSingleton<TusUploadController>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<FileStorageHealthCheck>("FileStorage")
    .AddCheck<CsvRepositoryHealthCheck>("CsvRepository");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add request logging
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseCors();

// Configure TUS middleware
app.UseTus(httpContext => httpContext.RequestServices.GetRequiredService<TusUploadController>().GetTusConfiguration());

app.UseAuthorization();
app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health");

// Add background service for cleanup
var cleanupTimer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);
cleanupTimer.Elapsed += async (sender, e) =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var fileUploadService = scope.ServiceProvider.GetRequiredService<IFileUploadService>();
        await fileUploadService.CleanupExpiredUploadsAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during cleanup of expired uploads");
    }
};
cleanupTimer.Start();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
