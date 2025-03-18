using Amazon.S3;
using FileUploadAPI.Core.Interfaces;
using FileUploadAPI.Core.Services;
using FileUploadAPI.Infrastructure.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<DigitalOceanStorageOptions>(
    builder.Configuration.GetSection("DigitalOceanStorage"));

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = new AmazonS3Config
    {
        ServiceURL = builder.Configuration["DigitalOceanStorage:Endpoint"],
        ForcePathStyle = true
    };

    return new AmazonS3Client(
        builder.Configuration["DigitalOceanStorage:AccessKey"],
        builder.Configuration["DigitalOceanStorage:SecretKey"],
        config);
});

builder.Services.AddSingleton<IStorageService, DigitalOceanStorageService>();
builder.Services.AddSingleton<FileCompressionService>();
builder.Services.AddSingleton<UploadProgressTracker>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run(); 