using System;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAPI.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FileUploadAPI.Api.HealthChecks
{
    public class FileStorageHealthCheck : IHealthCheck
    {
        private readonly IFileStorageService _fileStorageService;

        public FileStorageHealthCheck(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to check if a test file exists
                var exists = await _fileStorageService.FileExistsAsync("health-check", "test.txt", cancellationToken);
                return HealthCheckResult.Healthy("File storage is accessible");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("File storage is not accessible", ex);
            }
        }
    }
} 