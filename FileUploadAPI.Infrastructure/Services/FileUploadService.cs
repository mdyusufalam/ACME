using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAPI.Core.Interfaces;
using FileUploadAPI.Core.Models;

namespace FileUploadAPI.Infrastructure.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IFileStorageService _storageService;
        private readonly IFileUploadRepository _repository;

        public FileUploadService(IFileStorageService storageService, IFileUploadRepository repository)
        {
            _storageService = storageService;
            _repository = repository;
        }

        public async Task<FileUpload> CreateUploadAsync(string clientId, string fileName, long fileSize, string contentType, CancellationToken cancellationToken = default)
        {
            var upload = new FileUpload
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = clientId,
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                Status = FileUploadStatus.Pending,
                UploadedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            return await _repository.AddAsync(upload, cancellationToken);
        }

        public async Task<FileUpload> GetUploadAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<IEnumerable<FileUpload>> GetClientUploadsAsync(string clientId, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByClientIdAsync(clientId, cancellationToken);
        }

        public async Task<bool> DeleteUploadAsync(string id, string clientId, CancellationToken cancellationToken = default)
        {
            var upload = await _repository.GetByIdAsync(id, cancellationToken);
            if (upload == null || upload.ClientId != clientId)
            {
                return false;
            }

            if (upload.StoragePath != null)
            {
                await _storageService.DeleteFileAsync(clientId, upload.FileName, cancellationToken);
            }

            return await _repository.DeleteAsync(id, cancellationToken);
        }

        public async Task<FileUpload> CompleteUploadAsync(string id, string clientId, CancellationToken cancellationToken = default)
        {
            var upload = await _repository.GetByIdAsync(id, cancellationToken);
            if (upload == null || upload.ClientId != clientId)
            {
                return null;
            }

            if (await _storageService.FileExistsAsync(clientId, upload.FileName, cancellationToken))
            {
                upload.Status = FileUploadStatus.Completed;
                upload.StoragePath = $"{clientId}/{upload.FileName}";
                return await _repository.UpdateAsync(upload, cancellationToken);
            }

            return upload;
        }

        public async Task CleanupExpiredUploadsAsync(CancellationToken cancellationToken = default)
        {
            var expiredUploads = await _repository.GetExpiredUploadsAsync(cancellationToken);
            foreach (var upload in expiredUploads)
            {
                await DeleteUploadAsync(upload.Id, upload.ClientId, cancellationToken);
            }
        }
    }
} 