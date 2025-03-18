using System;
using System.IO;
using System.Threading.Tasks;
using FileUploadAPI.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

namespace FileUploadAPI.Api.Controllers
{
    public class TusUploadController
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileUploadService _fileUploadService;

        public TusUploadController(IFileStorageService fileStorageService, IFileUploadService fileUploadService)
        {
            _fileStorageService = fileStorageService;
            _fileUploadService = fileUploadService;
        }

        public DefaultTusConfiguration GetTusConfiguration()
        {
            return new DefaultTusConfiguration
            {
                Store = new TusDiskStore(@"./tusfiles"),
                UrlPath = "/api/tus",
                Events = new Events
                {
                    OnFileCompleteAsync = async eventContext =>
                    {
                        var file = await eventContext.GetFileAsync();
                        var metadata = await file.GetMetadataAsync();

                        var clientId = metadata.ContainsKey("clientId")
                            ? metadata["clientId"].GetString()
                            : throw new InvalidOperationException("Client ID is required");

                        var fileId = metadata.ContainsKey("fileId")
                            ? metadata["fileId"].GetString()
                            : throw new InvalidOperationException("File ID is required");

                        using var fileStream = await file.GetContentAsync();
                        var memoryStream = new MemoryStream();
                        await fileStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        await _fileStorageService.UploadFileAsync(
                            clientId,
                            memoryStream,
                            metadata["filename"].GetString(),
                            eventContext.CancellationToken);

                        await _fileUploadService.CompleteUploadAsync(
                            fileId,
                            clientId,
                            eventContext.CancellationToken);

                        // Cleanup temporary file
                        await file.DeleteAsync();
                    }
                },
                AllowedExtensions = new[] { ".csv" },
                MaxAllowedUploadSizeInBytes = 5L * 1024L * 1024L * 1024L // 5GB
            };
        }
    }
} 