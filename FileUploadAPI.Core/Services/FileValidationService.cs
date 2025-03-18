using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FileUploadAPI.Core.Services
{
    public class FileValidationService
    {
        private readonly long _maxFileSizeBytes;
        private readonly HashSet<string> _allowedExtensions;
        private readonly HashSet<string> _allowedMimeTypes;

        public FileValidationService(long maxFileSizeBytes = 5L * 1024L * 1024L * 1024L) // 5GB default
        {
            _maxFileSizeBytes = maxFileSizeBytes;
            _allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv" };
            _allowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "text/csv",
                "application/csv",
                "application/vnd.ms-excel"
            };
        }

        public async Task<(bool IsValid, string ErrorMessage)> ValidateFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "File is empty");
            }

            if (file.Length > _maxFileSizeBytes)
            {
                return (false, $"File size exceeds maximum allowed size of {_maxFileSizeBytes / (1024 * 1024)} MB");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!_allowedExtensions.Contains(extension))
            {
                return (false, $"File extension {extension} is not allowed");
            }

            if (!_allowedMimeTypes.Contains(file.ContentType))
            {
                return (false, $"File type {file.ContentType} is not allowed");
            }

            // Validate CSV format
            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                var firstLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(firstLine))
                {
                    return (false, "CSV file is empty or invalid");
                }

                // Check if it's a valid CSV by counting commas
                var commaCount = firstLine.Count(c => c == ',');
                if (commaCount == 0)
                {
                    return (false, "File does not appear to be a valid CSV");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error validating CSV format: {ex.Message}");
            }

            return (true, null);
        }

        public void AddAllowedExtension(string extension)
        {
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }
            _allowedExtensions.Add(extension.ToLowerInvariant());
        }

        public void AddAllowedMimeType(string mimeType)
        {
            _allowedMimeTypes.Add(mimeType.ToLowerInvariant());
        }
    }
} 