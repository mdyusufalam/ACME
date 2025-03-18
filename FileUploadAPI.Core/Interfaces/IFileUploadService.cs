using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAPI.Core.Models;

namespace FileUploadAPI.Core.Interfaces
{
    public interface IFileUploadService
    {
        Task<FileUpload> CreateUploadAsync(string clientId, string fileName, long fileSize, string contentType, CancellationToken cancellationToken = default);
        Task<FileUpload> GetUploadAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<FileUpload>> GetClientUploadsAsync(string clientId, CancellationToken cancellationToken = default);
        Task<bool> DeleteUploadAsync(string id, string clientId, CancellationToken cancellationToken = default);
        Task<FileUpload> CompleteUploadAsync(string id, string clientId, CancellationToken cancellationToken = default);
        Task CleanupExpiredUploadsAsync(CancellationToken cancellationToken = default);
    }
} 