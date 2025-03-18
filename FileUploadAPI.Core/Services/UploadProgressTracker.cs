using System;
using System.Collections.Concurrent;
using FileUploadAPI.Core.Models;

namespace FileUploadAPI.Core.Services
{
    public class UploadProgressTracker
    {
        private readonly ConcurrentDictionary<string, FileUpload> _activeUploads = new();

        public void TrackUpload(FileUpload fileUpload)
        {
            _activeUploads.TryAdd(fileUpload.Id, fileUpload);
        }

        public void UpdateProgress(string fileUploadId, long bytesUploaded, int progressPercentage)
        {
            if (_activeUploads.TryGetValue(fileUploadId, out var fileUpload))
            {
                fileUpload.BytesUploaded = bytesUploaded;
                fileUpload.ProgressPercentage = progressPercentage;
            }
        }

        public void UpdateStatus(string fileUploadId, FileUploadStatus status)
        {
            if (_activeUploads.TryGetValue(fileUploadId, out var fileUpload))
            {
                fileUpload.Status = status;
            }
        }

        public void UpdateError(string fileUploadId, string error)
        {
            if (_activeUploads.TryGetValue(fileUploadId, out var fileUpload))
            {
                fileUpload.LastError = error;
                fileUpload.RetryCount++;
                fileUpload.LastRetryAttempt = DateTime.UtcNow;
            }
        }

        public void UpdateCompression(string fileUploadId, long originalSize, long compressedSize)
        {
            if (_activeUploads.TryGetValue(fileUploadId, out var fileUpload))
            {
                fileUpload.IsCompressed = true;
                fileUpload.OriginalSize = originalSize;
                fileUpload.CompressedSize = compressedSize;
            }
        }

        public FileUpload GetUploadStatus(string fileUploadId)
        {
            return _activeUploads.TryGetValue(fileUploadId, out var fileUpload) ? fileUpload : null;
        }

        public void RemoveUpload(string fileUploadId)
        {
            _activeUploads.TryRemove(fileUploadId, out _);
        }

        public bool IsUploadActive(string fileUploadId)
        {
            return _activeUploads.ContainsKey(fileUploadId);
        }
    }
} 