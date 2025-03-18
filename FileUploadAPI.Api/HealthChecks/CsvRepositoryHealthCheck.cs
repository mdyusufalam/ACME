using System;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAPI.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FileUploadAPI.Api.HealthChecks
{
    public class CsvRepositoryHealthCheck : IHealthCheck
    {
        private readonly IFileUploadRepository _repository;

        public CsvRepositoryHealthCheck(IFileUploadRepository repository)
        {
            _repository = repository;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to get expired uploads as a test
                await _repository.GetExpiredUploadsAsync(cancellationToken);
                return HealthCheckResult.Healthy("CSV repository is accessible");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("CSV repository is not accessible", ex);
            }
        }
    }
} 