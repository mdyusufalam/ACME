using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileUploadAPI.Core.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadAsync(
            string clientId,
            string fileId,
            Stream fileStream,
            string contentType,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default);

        Task<Stream> DownloadAsync(
            string storageLocation,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string storageLocation,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            string storageLocation,
            CancellationToken cancellationToken = default);
    }
} 