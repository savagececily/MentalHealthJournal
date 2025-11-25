using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MentalHealthJournal.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly ILogger<BlobStorageService> _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _audioContainerName;

        public BlobStorageService(ILogger<BlobStorageService> logger, IConfiguration configuration, BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
            _audioContainerName = configuration["AzureBlobStorage:ContainerName"];
        }


        public async Task<string> UploadAudioAsync(IFormFile audioFile, string userId, CancellationToken cancellationToken = default)
        {
            string blobName = $"{userId}/{Guid.NewGuid()}{Path.GetExtension(audioFile.FileName)}";
            _logger.LogInformation("Uploading audio file to blob storage: {BlobName}", blobName);
            
            BlobContainerClient _containerClient = _blobServiceClient.GetBlobContainerClient(_audioContainerName);
            await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            BlobClient blobClient = _containerClient.GetBlobClient(blobName);

            using var stream = audioFile.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);

            return blobClient.Uri.ToString();
        }

    }
}
