using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using FileUploadAPI.Core.Interfaces;
using FileUploadAPI.Core.Models;

namespace FileUploadAPI.Infrastructure.Services
{
    public class CsvFileUploadRepository : IFileUploadRepository
    {
        private readonly string _csvFilePath;
        private readonly SemaphoreSlim _semaphore;
        private List<FileUpload> _uploads;

        public CsvFileUploadRepository(string csvFilePath)
        {
            _csvFilePath = csvFilePath;
            _semaphore = new SemaphoreSlim(1, 1);
            _uploads = new List<FileUpload>();
            InitializeAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_csvFilePath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_csvFilePath));
                    await SaveChangesAsync();
                    return;
                }

                using var reader = new StreamReader(_csvFilePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
                _uploads = csv.GetRecords<FileUpload>().ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<FileUpload> AddAsync(FileUpload upload, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _uploads.Add(upload);
                await SaveChangesAsync(cancellationToken);
                return upload;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<FileUpload> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return _uploads.FirstOrDefault(u => u.Id == id);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEnumerable<FileUpload>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return _uploads.Where(u => u.ClientId == clientId).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var upload = _uploads.FirstOrDefault(u => u.Id == id);
                if (upload == null)
                {
                    return false;
                }

                _uploads.Remove(upload);
                await SaveChangesAsync(cancellationToken);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<FileUpload> UpdateAsync(FileUpload upload, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var existingUpload = _uploads.FirstOrDefault(u => u.Id == upload.Id);
                if (existingUpload == null)
                {
                    return null;
                }

                _uploads.Remove(existingUpload);
                _uploads.Add(upload);
                await SaveChangesAsync(cancellationToken);
                return upload;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEnumerable<FileUpload>> GetExpiredUploadsAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return _uploads.Where(u => u.ExpiresAt <= DateTime.UtcNow).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_csvFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(_csvFilePath, false);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
            await csv.WriteRecordsAsync(_uploads, cancellationToken);
        }
    }
} 