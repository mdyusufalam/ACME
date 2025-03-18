using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FileUploadAPI.Core.Services
{
    public class FileCompressionService
    {
        private const int CompressionThresholdBytes = 1024 * 1024; // 1MB
        private const int BufferSize = 81920; // 80KB buffer

        public async Task<(Stream CompressedStream, long CompressedSize)> CompressAsync(
            Stream inputStream,
            CancellationToken cancellationToken = default)
        {
            if (inputStream.Length < CompressionThresholdBytes)
            {
                // Don't compress small files
                return (inputStream, inputStream.Length);
            }

            var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, true))
            {
                await CopyStreamWithProgressAsync(inputStream, gzipStream, cancellationToken);
            }

            outputStream.Position = 0;
            return (outputStream, outputStream.Length);
        }

        public async Task<Stream> DecompressAsync(
            Stream compressedStream,
            CancellationToken cancellationToken = default)
        {
            var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                await CopyStreamWithProgressAsync(gzipStream, outputStream, cancellationToken);
            }

            outputStream.Position = 0;
            return outputStream;
        }

        private async Task CopyStreamWithProgressAsync(
            Stream source,
            Stream destination,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[BufferSize];
            int bytesRead;
            long totalBytesRead = 0;
            var sourceLength = source.Length;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead;

                var progress = (int)((double)totalBytesRead / sourceLength * 100);
                // You could raise an event here to report progress
            }
        }

        public bool ShouldCompress(long fileSize, string contentType)
        {
            // Only compress files larger than the threshold
            if (fileSize < CompressionThresholdBytes)
            {
                return false;
            }

            // Add more content types that are good candidates for compression
            return contentType.ToLower() switch
            {
                "text/csv" => true,
                "text/plain" => true,
                "application/json" => true,
                "application/xml" => true,
                _ => false
            };
        }
    }
} 