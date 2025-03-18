using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAPI.Core.Interfaces;
using FileUploadAPI.Core.Models;
using FileUploadAPI.Infrastructure.Services;
using Moq;
using Xunit;

namespace FileUploadAPI.Tests
{
    public class FileUploadServiceTests
    {
        private readonly Mock<IFileStorageService> _mockStorageService;
        private readonly Mock<IFileUploadRepository> _mockRepository;
        private readonly FileUploadService _fileUploadService;

        public FileUploadServiceTests()
        {
            _mockStorageService = new Mock<IFileStorageService>();
            _mockRepository = new Mock<IFileUploadRepository>();
            _fileUploadService = new FileUploadService(_mockStorageService.Object, _mockRepository.Object);
        }

        [Fact]
        public async Task CreateUploadAsync_ShouldCreateNewUpload()
        {
            // Arrange
            var clientId = "test-client";
            var fileName = "test.csv";
            var fileSize = 1024L;
            var contentType = "text/csv";

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync((FileUpload upload, CancellationToken _) => upload);

            // Act
            var result = await _fileUploadService.CreateUploadAsync(clientId, fileName, fileSize, contentType);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(clientId, result.ClientId);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal(fileSize, result.FileSize);
            Assert.Equal(contentType, result.ContentType);
            Assert.Equal(FileUploadStatus.Pending, result.Status);

            _mockRepository.Verify(x => x.AddAsync(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetUploadAsync_ShouldReturnUpload_WhenExists()
        {
            // Arrange
            var upload = new FileUpload
            {
                Id = "test-id",
                ClientId = "test-client",
                FileName = "test.csv"
            };

            _mockRepository.Setup(x => x.GetByIdAsync(upload.Id, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(upload);

            // Act
            var result = await _fileUploadService.GetUploadAsync(upload.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(upload.Id, result.Id);
            _mockRepository.Verify(x => x.GetByIdAsync(upload.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetClientUploadsAsync_ShouldReturnClientUploads()
        {
            // Arrange
            var clientId = "test-client";
            var uploads = new[]
            {
                new FileUpload { Id = "1", ClientId = clientId },
                new FileUpload { Id = "2", ClientId = clientId }
            };

            _mockRepository.Setup(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(uploads);

            // Act
            var result = await _fileUploadService.GetClientUploadsAsync(clientId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, upload => Assert.Equal(clientId, upload.ClientId));
            _mockRepository.Verify(x => x.GetByClientIdAsync(clientId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteUploadAsync_ShouldDeleteUpload_WhenExists()
        {
            // Arrange
            var clientId = "test-client";
            var upload = new FileUpload
            {
                Id = "test-id",
                ClientId = clientId,
                FileName = "test.csv",
                StoragePath = $"{clientId}/test.csv"
            };

            _mockRepository.Setup(x => x.GetByIdAsync(upload.Id, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(upload);
            _mockRepository.Setup(x => x.DeleteAsync(upload.Id, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(true);
            _mockStorageService.Setup(x => x.DeleteFileAsync(clientId, upload.FileName, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(true);

            // Act
            var result = await _fileUploadService.DeleteUploadAsync(upload.Id, clientId);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(x => x.DeleteAsync(upload.Id, It.IsAny<CancellationToken>()), Times.Once);
            _mockStorageService.Verify(x => x.DeleteFileAsync(clientId, upload.FileName, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CompleteUploadAsync_ShouldCompleteUpload_WhenFileExists()
        {
            // Arrange
            var clientId = "test-client";
            var upload = new FileUpload
            {
                Id = "test-id",
                ClientId = clientId,
                FileName = "test.csv"
            };

            _mockRepository.Setup(x => x.GetByIdAsync(upload.Id, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(upload);
            _mockStorageService.Setup(x => x.FileExistsAsync(clientId, upload.FileName, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(true);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync((FileUpload u, CancellationToken _) => u);

            // Act
            var result = await _fileUploadService.CompleteUploadAsync(upload.Id, clientId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(FileUploadStatus.Completed, result.Status);
            Assert.Equal($"{clientId}/{upload.FileName}", result.StoragePath);
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
} 