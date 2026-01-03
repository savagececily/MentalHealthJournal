using Azure.Storage.Blobs;
using MentalHealthJournal.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MentalHealthJournal.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly ILogger<BlobStorageService> _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _audioContainerName;

        public BlobStorageService(ILogger<BlobStorageService> logger, IOptions<AppSettings> configuration, BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
            _audioContainerName = configuration.Value.AzureBlobStorage.ContainerName ?? throw new ArgumentNullException("AzureBlobStorage:ContainerName");
        }


        public async Task<string> UploadAudioAsync(IFormFile audioFile, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (audioFile == null || audioFile.Length == 0)
                {
                    throw new ArgumentException("Audio file is null or empty");
                }

                string blobName = $"{userId}/{Guid.NewGuid()}{Path.GetExtension(audioFile.FileName)}";
                _logger.LogInformation("Uploading audio file to blob storage: {BlobName}", blobName);
                
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_audioContainerName);
                await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                using var stream = audioFile.OpenReadStream();
                var uploadOptions = new Azure.Storage.Blobs.Models.BlobUploadOptions
                {
                    HttpHeaders = new Azure.Storage.Blobs.Models.BlobHttpHeaders
                    {
                        ContentType = audioFile.ContentType
                    }
                };

                await blobClient.UploadAsync(stream, uploadOptions, cancellationToken: cancellationToken);

                _logger.LogInformation("Successfully uploaded audio file to blob storage: {BlobUrl}", blobClient.Uri);
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading audio file to blob storage for user: {UserId}", userId);
                throw;
            }
        }

    }
}
