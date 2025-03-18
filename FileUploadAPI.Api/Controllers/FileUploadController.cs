using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FileUploadAPI.Core.Interfaces;
using FileUploadAPI.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

namespace FileUploadAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly IFileStorageService _fileStorageService;

        public FileUploadController(IFileUploadService fileUploadService, IFileStorageService fileStorageService)
        {
            _fileUploadService = fileUploadService;
            _fileStorageService = fileStorageService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<FileUpload>> CreateUpload(
            [FromHeader(Name = "X-Client-Id")] string clientId,
            [FromBody] CreateUploadRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest("Client ID is required");
            }

            var upload = await _fileUploadService.CreateUploadAsync(
                clientId,
                request.FileName,
                request.FileSize,
                request.ContentType,
                cancellationToken);

            return Ok(upload);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FileUpload>> GetUpload(
            string id,
            [FromHeader(Name = "X-Client-Id")] string clientId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest("Client ID is required");
            }

            var upload = await _fileUploadService.GetUploadAsync(id, cancellationToken);
            if (upload == null || upload.ClientId != clientId)
            {
                return NotFound();
            }

            return Ok(upload);
        }

        [HttpGet("client")]
        public async Task<ActionResult<IEnumerable<FileUpload>>> GetClientUploads(
            [FromHeader(Name = "X-Client-Id")] string clientId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest("Client ID is required");
            }

            var uploads = await _fileUploadService.GetClientUploadsAsync(clientId, cancellationToken);
            return Ok(uploads);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUpload(
            string id,
            [FromHeader(Name = "X-Client-Id")] string clientId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest("Client ID is required");
            }

            var result = await _fileUploadService.DeleteUploadAsync(id, clientId, cancellationToken);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("{id}/complete")]
        public async Task<ActionResult<FileUpload>> CompleteUpload(
            string id,
            [FromHeader(Name = "X-Client-Id")] string clientId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest("Client ID is required");
            }

            var upload = await _fileUploadService.CompleteUploadAsync(id, clientId, cancellationToken);
            if (upload == null)
            {
                return NotFound();
            }

            return Ok(upload);
        }

        [HttpGet("{id}/download")]
        public async Task<ActionResult> DownloadFile(
            string id,
            [FromHeader(Name = "X-Client-Id")] string clientId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest("Client ID is required");
            }

            var upload = await _fileUploadService.GetUploadAsync(id, cancellationToken);
            if (upload == null || upload.ClientId != clientId)
            {
                return NotFound();
            }

            var stream = await _fileStorageService.GetFileAsync(clientId, upload.FileName, cancellationToken);
            if (stream == null)
            {
                return NotFound();
            }

            return File(stream, upload.ContentType, upload.FileName);
        }
    }

    public class CreateUploadRequest
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
    }
} 