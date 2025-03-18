using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAPI.Core.Models;

namespace FileUploadAPI.Core.Interfaces
{
    public interface IFileUploadRepository
    {
        Task<FileUpload> AddAsync(FileUpload upload, CancellationToken cancellationToken = default);
        Task<FileUpload> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<FileUpload>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task<FileUpload> UpdateAsync(FileUpload upload, CancellationToken cancellationToken = default);
        Task<IEnumerable<FileUpload>> GetExpiredUploadsAsync(CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
} 