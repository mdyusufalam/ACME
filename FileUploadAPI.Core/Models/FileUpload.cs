using System;

namespace FileUploadAPI.Core.Models
{
    public class FileUpload
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string StoragePath { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public FileUploadStatus Status { get; set; }
        public int ProgressPercentage { get; set; }
        public long BytesUploaded { get; set; }
        public int RetryCount { get; set; }
        public string LastError { get; set; }
        public DateTime? LastRetryAttempt { get; set; }
        public bool IsCompressed { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
    }

    public enum FileUploadStatus
    {
        Pending,
        InProgress,
        Compressing,
        Validating,
        Retrying,
        Completed,
        Failed,
        Expired,
        Cancelled,
        Processing,
        Uploading
    }
} 