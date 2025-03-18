using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAPI.Core.Models;

namespace FileUploadAPI.Core.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(string clientId, Stream fileStream, string fileName, CancellationToken cancellationToken = default);
        Task<bool> DeleteFileAsync(string clientId, string fileName, CancellationToken cancellationToken = default);
        Task<Stream> GetFileAsync(string clientId, string fileName, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(string clientId, string fileName, CancellationToken cancellationToken = default);
        string GetPresignedUrl(string clientId, string fileName, int expiryMinutes = 60);
    }
} 