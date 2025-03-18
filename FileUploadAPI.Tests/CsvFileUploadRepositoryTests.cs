using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileUploadAPI.Core.Models;
using FileUploadAPI.Infrastructure.Services;
using Xunit;

namespace FileUploadAPI.Tests
{
    public class CsvFileUploadRepositoryTests : IDisposable
    {
        private readonly string _testCsvPath;
        private readonly CsvFileUploadRepository _repository;

        public CsvFileUploadRepositoryTests()
        {
            _testCsvPath = Path.Combine(Path.GetTempPath(), $"test_uploads_{Guid.NewGuid()}.csv");
            _repository = new CsvFileUploadRepository(_testCsvPath);
        }

        public void Dispose()
        {
            if (File.Exists(_testCsvPath))
            {
                File.Delete(_testCsvPath);
            }
        }

        [Fact]
        public async Task AddAsync_ShouldAddUploadAndPersistToFile()
        {
            // Arrange
            var upload = new FileUpload
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = "test-client",
                FileName = "test.csv",
                FileSize = 1024,
                ContentType = "text/csv",
                Status = FileUploadStatus.Pending,
                UploadedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var result = await _repository.AddAsync(upload);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(upload.Id, result.Id);
            Assert.True(File.Exists(_testCsvPath));

            // Verify the file contains the upload
            var savedUpload = await _repository.GetByIdAsync(upload.Id);
            Assert.NotNull(savedUpload);
            Assert.Equal(upload.Id, savedUpload.Id);
            Assert.Equal(upload.ClientId, savedUpload.ClientId);
            Assert.Equal(upload.FileName, savedUpload.FileName);
        }

        [Fact]
        public async Task GetByClientIdAsync_ShouldReturnClientUploads()
        {
            // Arrange
            var clientId = "test-client";
            var upload1 = new FileUpload
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = clientId,
                FileName = "test1.csv"
            };
            var upload2 = new FileUpload
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = clientId,
                FileName = "test2.csv"
            };

            await _repository.AddAsync(upload1);
            await _repository.AddAsync(upload2);

            // Act
            var result = await _repository.GetByClientIdAsync(clientId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, u => Assert.Equal(clientId, u.ClientId));
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveUploadFromFile()
        {
            // Arrange
            var upload = new FileUpload
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = "test-client",
                FileName = "test.csv"
            };
            await _repository.AddAsync(upload);

            // Act
            var result = await _repository.DeleteAsync(upload.Id);

            // Assert
            Assert.True(result);
            var deletedUpload = await _repository.GetByIdAsync(upload.Id);
            Assert.Null(deletedUpload);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateExistingUpload()
        {
            // Arrange
            var upload = new FileUpload
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = "test-client",
                FileName = "test.csv",
                Status = FileUploadStatus.Pending
            };
            await _repository.AddAsync(upload);

            // Update the upload
            upload.Status = FileUploadStatus.Completed;
            upload.StoragePath = "test-path";

            // Act
            var result = await _repository.UpdateAsync(upload);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(FileUploadStatus.Completed, result.Status);
            Assert.Equal("test-path", result.StoragePath);

            // Verify the file contains the updated upload
            var updatedUpload = await _repository.GetByIdAsync(upload.Id);
            Assert.NotNull(updatedUpload);
            Assert.Equal(FileUploadStatus.Completed, updatedUpload.Status);
            Assert.Equal("test-path", updatedUpload.StoragePath);
        }

        [Fact]
        public async Task GetExpiredUploadsAsync_ShouldReturnExpiredUploads()
        {
            // Arrange
            var expiredUpload = new FileUpload
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = "test-client",
                FileName = "expired.csv",
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            };
            var validUpload = new FileUpload
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = "test-client",
                FileName = "valid.csv",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            await _repository.AddAsync(expiredUpload);
            await _repository.AddAsync(validUpload);

            // Act
            var result = await _repository.GetExpiredUploadsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expiredUpload.Id, result.First().Id);
        }
    }
} 