using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using FileUploadAPI.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace FileUploadAPI.Infrastructure.Services
{
    public class DigitalOceanStorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private const int BufferSize = 81920; // 80KB chunks for upload

        public DigitalOceanStorageService(
            IAmazonS3 s3Client,
            IOptions<DigitalOceanStorageOptions> options)
        {
            _s3Client = s3Client;
            _bucketName = options.Value.BucketName;
        }

        public async Task<string> UploadAsync(
            string clientId,
            string fileId,
            Stream fileStream,
            string contentType,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            var key = $"{clientId}/{fileId}";
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentType = contentType,
                InputStream = fileStream
            };

            if (progress != null && fileStream.Length > 0)
            {
                request.StreamTransferProgress += (sender, args) =>
                {
                    var percentComplete = (int)((double)args.TransferredBytes / fileStream.Length * 100);
                    progress.Report(percentComplete);
                };
            }

            await _s3Client.PutObjectAsync(request, cancellationToken);
            return key;
        }

        public async Task<Stream> DownloadAsync(
            string storageLocation,
            CancellationToken cancellationToken = default)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = storageLocation
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, BufferSize, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task DeleteAsync(
            string storageLocation,
            CancellationToken cancellationToken = default)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = storageLocation
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            string storageLocation,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = storageLocation
                };

                await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }

    public class DigitalOceanStorageOptions
    {
        public string BucketName { get; set; }
    }
} 