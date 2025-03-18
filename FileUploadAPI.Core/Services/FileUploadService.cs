using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAPI.Core.Models;
using FileUploadAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileUploadAPI.Core.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IStorageService _storageService;
        private readonly IFileUploadRepository _repository;
        private readonly FileCompressionService _compressionService;
        private readonly UploadProgressTracker _progressTracker;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            IStorageService storageService,
            IFileUploadRepository repository,
            FileCompressionService compressionService,
            UploadProgressTracker progressTracker,
            ILogger<FileUploadService> logger)
        {
            _storageService = storageService;
            _repository = repository;
            _compressionService = compressionService;
            _progressTracker = progressTracker;
            _logger = logger;
        }

        public async Task<FileUpload> InitiateUploadAsync(
            string clientId,
            string fileName,
            long fileSize,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var fileUpload = new FileUpload
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = clientId,
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                Status = FileUploadStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            await _repository.CreateAsync(fileUpload, cancellationToken);
            _progressTracker.TrackUpload(fileUpload);

            return fileUpload;
        }

        public async Task<FileUpload> ProcessUploadAsync(
            string fileUploadId,
            Stream fileStream,
            CancellationToken cancellationToken = default)
        {
            var fileUpload = await _repository.GetByIdAsync(fileUploadId, cancellationToken);
            if (fileUpload == null)
            {
                throw new ArgumentException("File upload not found", nameof(fileUploadId));
            }

            try
            {
                _progressTracker.UpdateStatus(fileUploadId, FileUploadStatus.Processing);

                Stream uploadStream = fileStream;
                if (_compressionService.ShouldCompress(fileUpload.FileSize, fileUpload.ContentType))
                {
                    _progressTracker.UpdateStatus(fileUploadId, FileUploadStatus.Compressing);
                    var (compressedStream, compressedSize) = await _compressionService.CompressAsync(fileStream, cancellationToken);
                    uploadStream = compressedStream;
                    _progressTracker.UpdateCompression(fileUploadId, fileUpload.FileSize, compressedSize);
                }

                _progressTracker.UpdateStatus(fileUploadId, FileUploadStatus.Uploading);
                var progress = new Progress<int>(percentage =>
                {
                    _progressTracker.UpdateProgress(fileUploadId, uploadStream.Position, percentage);
                });

                var storageLocation = await _storageService.UploadAsync(
                    fileUpload.ClientId,
                    fileUpload.Id,
                    uploadStream,
                    fileUpload.ContentType,
                    progress,
                    cancellationToken);

                fileUpload.StorageLocation = storageLocation;
                fileUpload.Status = FileUploadStatus.Completed;
                fileUpload.CompletedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(fileUpload, cancellationToken);
                _progressTracker.RemoveUpload(fileUploadId);

                return fileUpload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing upload for file {FileUploadId}", fileUploadId);
                _progressTracker.UpdateError(fileUploadId, ex.Message);
                _progressTracker.UpdateStatus(fileUploadId, FileUploadStatus.Failed);

                fileUpload.Status = FileUploadStatus.Failed;
                fileUpload.LastError = ex.Message;
                await _repository.UpdateAsync(fileUpload, cancellationToken);

                throw;
            }
        }

        public async Task<FileUpload> GetUploadStatusAsync(
            string fileUploadId,
            CancellationToken cancellationToken = default)
        {
            var activeUpload = _progressTracker.GetUploadStatus(fileUploadId);
            if (activeUpload != null)
            {
                return activeUpload;
            }

            return await _repository.GetByIdAsync(fileUploadId, cancellationToken);
        }

        public async Task CancelUploadAsync(
            string fileUploadId,
            CancellationToken cancellationToken = default)
        {
            var fileUpload = await _repository.GetByIdAsync(fileUploadId, cancellationToken);
            if (fileUpload == null)
            {
                throw new ArgumentException("File upload not found", nameof(fileUploadId));
            }

            if (_progressTracker.IsUploadActive(fileUploadId))
            {
                _progressTracker.UpdateStatus(fileUploadId, FileUploadStatus.Cancelled);
                _progressTracker.RemoveUpload(fileUploadId);
            }

            fileUpload.Status = FileUploadStatus.Cancelled;
            await _repository.UpdateAsync(fileUpload, cancellationToken);

            // Clean up any partially uploaded data
            if (!string.IsNullOrEmpty(fileUpload.StorageLocation))
            {
                await _storageService.DeleteAsync(fileUpload.StorageLocation, cancellationToken);
            }
        }
    }
} 